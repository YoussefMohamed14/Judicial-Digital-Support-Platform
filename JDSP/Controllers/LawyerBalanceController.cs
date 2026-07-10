using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModel.LawyerBalance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace JDSP.Controllers {
    [Authorize(Roles = Roles.Lawyer)]
    public class LawyerBalanceController : Controller {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public LawyerBalanceController(ApplicationDbContext db, UserManager<ApplicationUser> users) {
            _db = db;
            _users = users;
        }

        [HttpGet]
        public async Task<IActionResult> Index() {
            var lawyerId = _users.GetUserId(User);
            if (lawyerId == null) return Challenge();
            return View(await BuildViewModelAsync(lawyerId));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw([Bind(Prefix = "Withdraw")] WithdrawBalanceViewModel model) {
            var lawyer = await _users.GetUserAsync(User);
            if (lawyer == null) return Challenge();

            var availablePayments = await _db.Payments
                .Include(x => x.Case)
                .Where(x => x.RequestedByLawyerId == lawyer.Id &&
                            x.Status == "Paid" &&
                            x.LawyerWithdrawnAmount < x.Amount)
                .OrderBy(x => x.PaidAt)
                .ToListAsync();

            var availableBalance = availablePayments.Sum(x => x.Amount - x.LawyerWithdrawnAmount);
            if (availableBalance <= 0) {
                TempData["Error"] = Text("There is no available balance to withdraw.", "لا يوجد رصيد متاح للسحب.");
                return RedirectToAction(nameof(Index));
            }

            if (model.Amount <= 0)
                ModelState.AddModelError("Withdraw.Amount", Text("Enter a withdrawal amount.", "اكتب مبلغ السحب."));
            if (model.Amount > availableBalance)
                ModelState.AddModelError("Withdraw.Amount", Text("The requested amount is higher than your available balance.", "المبلغ المطلوب أكبر من الرصيد المتاح."));

            if (!ModelState.IsValid) {
                var vm = await BuildViewModelAsync(lawyer.Id);
                vm.Withdraw = model;
                return View(nameof(Index), vm);
            }

            var now = DateTime.UtcNow;
            var reference = $"WD-{now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
            var last4 = LastFour(model.CardNumber);
            var remaining = model.Amount;

            foreach (var payment in availablePayments) {
                if (remaining <= 0) break;

                var paymentAvailable = payment.Amount - payment.LawyerWithdrawnAmount;
                if (paymentAvailable <= 0) continue;

                var take = Math.Min(paymentAvailable, remaining);
                payment.LawyerWithdrawnAmount += take;
                remaining -= take;
                payment.LawyerPayoutStatus = payment.LawyerWithdrawnAmount >= payment.Amount ? "Withdrawn" : "PartiallyWithdrawn";
                payment.LawyerPayoutRequestedAt = now;
                payment.LawyerPayoutCompletedAt = now;
                payment.LawyerPayoutCardLast4 = last4;
                payment.LawyerPayoutReference = reference;
            }

            _db.SystemNotifications.Add(new SystemNotification {
                RecipientId = lawyer.Id,
                Title = lawyer.PreferredLanguage == "ar" ? "تم سحب مبلغ من الرصيد" : "Balance amount withdrawn",
                Body = lawyer.PreferredLanguage == "ar"
                    ? $"تم تحويل {model.Amount:0.00} إلى البطاقة المنتهية بـ {last4}. رقم العملية: {reference}."
                    : $"{model.Amount:0.00} was transferred to the card ending in {last4}. Reference: {reference}.",
                Category = "Balance",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = Text("Withdrawal completed successfully.", "تم السحب بنجاح.");
            return RedirectToAction(nameof(Index));
        }

        private async Task<LawyerBalanceViewModel> BuildViewModelAsync(string lawyerId) {
            var payments = await _db.Payments.AsNoTracking()
                .Where(x => x.RequestedByLawyerId == lawyerId && x.Status == "Paid")
                .OrderByDescending(x => x.PaidAt)
                .Select(x => new LawyerBalancePaymentViewModel {
                    PaymentId = x.Id,
                    CaseId = x.CaseId,
                    CaseName = x.Case == null ? string.Empty : x.Case.CaseName,
                    ClientName = x.PaidBy == null ? "Client" : x.PaidBy.FirstName + " " + x.PaidBy.LastName,
                    Amount = x.Amount,
                    WithdrawnAmount = x.LawyerWithdrawnAmount,
                    AvailableAmount = x.Amount - x.LawyerWithdrawnAmount,
                    BillingType = x.BillingType,
                    PaidAt = x.PaidAt,
                    LawyerPayoutStatus = x.LawyerPayoutStatus,
                    LawyerPayoutCompletedAt = x.LawyerPayoutCompletedAt,
                    LawyerPayoutCardLast4 = x.LawyerPayoutCardLast4,
                    LawyerPayoutReference = x.LawyerPayoutReference
                })
                .ToListAsync();

            return new LawyerBalanceViewModel {
                AvailableBalance = payments.Sum(x => x.AvailableAmount),
                WithdrawnBalance = payments.Sum(x => x.WithdrawnAmount),
                TotalPaid = payments.Sum(x => x.Amount),
                AvailablePaymentCount = payments.Count(x => x.AvailableAmount > 0),
                Payments = payments
            };
        }

        private static string LastFour(string value) {
            var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            return digits.Length <= 4 ? digits : digits[^4..];
        }

        private string Text(string en, string ar)
            => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? ar : en;
    }
}
