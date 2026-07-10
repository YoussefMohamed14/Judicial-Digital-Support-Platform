using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Cases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace JDSP.Controllers {
    [Authorize(Roles = Roles.Client)]
    public class PaymentsController : Controller {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public PaymentsController(ApplicationDbContext db, UserManager<ApplicationUser> users) {
            _db = db;
            _users = users;
        }

        [HttpGet]
        public async Task<IActionResult> Pay(int id) {
            var userId = _users.GetUserId(User);
            if (userId == null) return Challenge();

            var model = await BuildPaymentViewModelAsync(id, userId);
            if (model == null) return NotFound();
            if (model.Status != "Requested") {
                TempData["Error"] = Text("This payment request is no longer pending.", "طلب الدفع هذا لم يعد معلقاً.");
                return RedirectToAction("Index", "Messages");
            }

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(ClientPaymentViewModel model) {
            var userId = _users.GetUserId(User);
            if (userId == null) return Challenge();

            var payment = await _db.Payments
                .Include(x => x.Case)
                .Include(x => x.RequestedByLawyer)
                .FirstOrDefaultAsync(x => x.Id == model.PaymentId && x.PaidById == userId);

            if (payment?.Case == null) return NotFound();

            if (payment.Status != "Requested") {
                TempData["Error"] = Text("This payment request is no longer pending.", "طلب الدفع هذا لم يعد معلقاً.");
                return RedirectToAction("Index", "Messages", new { contactId = payment.RequestedByLawyerId });
            }

            if (!ModelState.IsValid) {
                var reload = await BuildPaymentViewModelAsync(model.PaymentId, userId);
                if (reload == null) return NotFound();
                reload.CardHolderName = model.CardHolderName;
                reload.CardNumber = model.CardNumber;
                reload.Expiry = model.Expiry;
                reload.Cvv = model.Cvv;
                return View(reload);
            }

            payment.Status = "Paid";
            payment.PaymentMethod = "Credit Card";
            payment.TransactionRef = $"PAID-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
            payment.PaidAt = DateTime.UtcNow;
            payment.LawyerPayoutStatus = "Available";

            if (!string.IsNullOrWhiteSpace(payment.RequestedByLawyerId)) {
                var ar = payment.RequestedByLawyer?.PreferredLanguage == "ar";
                // The original payment request chat card is linked to this payment row,
                // so changing the payment status updates the same DM card instead of adding a duplicate message.

                _db.SystemNotifications.Add(new SystemNotification {
                    RecipientId = payment.RequestedByLawyerId,
                    Title = ar ? "دفعة جديدة في الرصيد" : "New payment in balance",
                    Body = ar
                        ? $"تمت إضافة {payment.Amount:0.00} إلى رصيدك عن القضية: {payment.Case.CaseName}."
                        : $"{payment.Amount:0.00} was added to your balance for case: {payment.Case.CaseName}.",
                    Category = "Balance",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = Text("Payment completed successfully. The request card is now marked as paid.", "تم الدفع بنجاح. أصبحت بطاقة الدفع مدفوعة الآن.");
            return RedirectToAction("Index", "Messages", new { contactId = payment.RequestedByLawyerId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(int paymentId, string reason) {
            var userId = _users.GetUserId(User);
            if (userId == null) return Challenge();

            reason = (reason ?? string.Empty).Trim();
            if (reason.Length == 0) {
                TempData["Error"] = Text("Write a reason before declining the payment request.", "اكتب سبب الرفض قبل رفض طلب الدفع.");
                return RedirectToAction("Index", "Messages");
            }

            var payment = await _db.Payments
                .Include(x => x.Case)
                .Include(x => x.RequestedByLawyer)
                .FirstOrDefaultAsync(x => x.Id == paymentId && x.PaidById == userId);

            if (payment?.Case == null) return NotFound();
            if (payment.Status != "Requested") {
                TempData["Error"] = Text("This payment request is no longer pending.", "طلب الدفع هذا لم يعد معلقاً.");
                return RedirectToAction("Index", "Messages", new { contactId = payment.RequestedByLawyerId });
            }

            payment.Status = "Declined";
            payment.DeclineReason = reason.Length > 1000 ? reason[..1000] : reason;

            if (!string.IsNullOrWhiteSpace(payment.RequestedByLawyerId)) {
                var ar = payment.RequestedByLawyer?.PreferredLanguage == "ar";
                _db.SystemNotifications.Add(new SystemNotification {
                    RecipientId = payment.RequestedByLawyerId,
                    Title = ar ? "تم رفض طلب دفع" : "Payment request declined",
                    Body = ar
                        ? $"رفض العميل طلب الدفع الخاص بالقضية: {payment.Case.CaseName}. السبب: {payment.DeclineReason}"
                        : $"The client declined the payment request for case: {payment.Case.CaseName}. Reason: {payment.DeclineReason}",
                    Category = "Payment",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = Text("The payment request was declined and the lawyer was notified.", "تم رفض طلب الدفع وإبلاغ المحامي.");
            return RedirectToAction("Index", "Messages", new { contactId = payment.RequestedByLawyerId });
        }

        private async Task<ClientPaymentViewModel?> BuildPaymentViewModelAsync(int paymentId, string clientId) {
            return await _db.Payments.AsNoTracking()
                .Where(x => x.Id == paymentId && x.PaidById == clientId)
                .Select(x => new ClientPaymentViewModel {
                    PaymentId = x.Id,
                    CaseId = x.CaseId,
                    CaseName = x.Case == null ? string.Empty : x.Case.CaseName,
                    LawyerName = x.RequestedByLawyer == null ? "Lawyer" : x.RequestedByLawyer.FirstName + " " + x.RequestedByLawyer.LastName,
                    Amount = x.Amount,
                    BillingType = x.BillingType,
                    Note = x.Note,
                    Status = x.Status,
                    TransactionRef = x.TransactionRef
                })
                .FirstOrDefaultAsync();
        }

        private string Text(string en, string ar) => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? ar : en;
    }
}
