using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModel;
using JDSP.ViewModels.Cases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace JDSP.Controllers {
    [Authorize]
    public class CasesController : Controller {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public CasesController(ApplicationDbContext db, UserManager<ApplicationUser> users) {
            _db = db;
            _users = users;
        }

        [Authorize(Roles = Roles.CourtEmployee), HttpGet]
        public async Task<IActionResult> Create() {
            await LoadClientsAsync();
            return View(new CreateCaseViewModel());
        }

        [Authorize(Roles = Roles.CourtEmployee), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCaseViewModel model) {
            var client = string.IsNullOrWhiteSpace(model.ClientId)
                ? null
                : await _users.FindByIdAsync(model.ClientId);

            if (client == null || !await _users.IsInRoleAsync(client, Roles.Client))
                ModelState.AddModelError(nameof(model.ClientId), "Select a valid client account.");

            if (!ModelState.IsValid) {
                await LoadClientsAsync(model.ClientId);
                return View(model);
            }

            var item = new Case {
                CaseName = model.CaseName.Trim(),
                CaseType = model.CaseType.Trim(),
                Description = model.Description.Trim(),
                CreatedBy_Id = client!.Id,
                CreatedAt = DateTime.Now,
                Status = "Open"
            };

            _db.Cases.Add(item);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = Text("The case was created and assigned to the client.", "تم إنشاء القضية وإسنادها إلى العميل.");
            return RedirectToAction("CourtEmployeeDashboard", "Dashboard");
        }

        [Authorize(Roles = Roles.Client), HttpGet]
        public async Task<IActionResult> MyCases(string? status) {
            var userId = _users.GetUserId(User);
            if (userId == null) return Challenge();
            await UpdateEndedHearingFollowUpsAsync();

            var query = _db.Cases.AsNoTracking().Where(c => c.CreatedBy_Id == userId);
            var allowed = new[] { "Open", "Pending", "In Progress", "Waiting for next hearing date", "Postponed", "Closed" };
            if (!string.IsNullOrWhiteSpace(status) && allowed.Contains(status))
                query = query.Where(c => c.Status == status);

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ClientCaseListItemViewModel {
                    CaseId = c.CaseID,
                    CaseName = c.CaseName,
                    CaseType = c.CaseType,
                    Description = c.Description,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    AssignedLawyerName = _db.CaseLawyers
                        .Where(x => x.CaseId == c.CaseID && (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"))
                        .OrderByDescending(x => x.AssignedAt)
                        .Select(x => x.Lawyer == null ? null : x.Lawyer.FirstName + " " + x.Lawyer.LastName)
                        .FirstOrDefault(),
                    NextHearingDate = _db.Hearings
                        .Where(h => h.CaseId == c.CaseID && h.EndDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => (DateTime?)h.HearingDate)
                        .FirstOrDefault(),
                    NextHearingEndDate = _db.Hearings
                        .Where(h => h.CaseId == c.CaseID && h.EndDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => (DateTime?)h.EndDate)
                        .FirstOrDefault()
                })
                .ToListAsync();

            ApplyHearingDisplayState(items);
            return View(new MyCasesViewModel { StatusFilter = status, Cases = items });
        }

        [Authorize(Roles = Roles.Client), HttpGet]
        public async Task<IActionResult> Details(int id) {
            var userId = _users.GetUserId(User);
            if (userId == null) return Challenge();
            await UpdateEndedHearingFollowUpsAsync();

            var item = await _db.Cases.AsNoTracking()
                .Where(c => c.CaseID == id && c.CreatedBy_Id == userId)
                .Select(c => new ClientCaseDetailsViewModel {
                    CaseId = c.CaseID,
                    CaseName = c.CaseName,
                    CaseType = c.CaseType,
                    Description = c.Description,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (item == null) return NotFound();

            var request = await _db.CaseLawyers.AsNoTracking()
                .Where(x => x.CaseId == id)
                .OrderByDescending(x => x.AssignedAt)
                .Select(x => new {
                    x.Status,
                    Name = x.Lawyer == null ? null : x.Lawyer.FirstName + " " + x.Lawyer.LastName
                })
                .FirstOrDefaultAsync();

            var hearing = await _db.Hearings.AsNoTracking()
                .Where(h => h.CaseId == id && h.EndDate >= DateTime.Now && h.Status == "Scheduled")
                .OrderBy(h => h.HearingDate)
                .Select(h => new { h.HearingDate, h.EndDate, h.Location, h.HearingType })
                .FirstOrDefaultAsync();

            item.AssignedLawyerName = request?.Name;
            item.LawyerRequestStatus = request?.Status;
            item.NextHearingDate = hearing?.HearingDate;
            item.NextHearingEndDate = hearing?.EndDate;
            item.HearingCountdownPhase = HearingPhase(item.NextHearingDate, item.NextHearingEndDate);
            ApplyWaitingStatus(item);
            item.NextHearingLocation = hearing?.Location;
            item.NextHearingType = hearing?.HearingType;
            return View(item);
        }

        [Authorize(Roles = Roles.Lawyer), HttpGet]
        public async Task<IActionResult> AssignedCases(string? status) {
            var lawyerId = _users.GetUserId(User);
            if (lawyerId == null) return Challenge();
            await UpdateEndedHearingFollowUpsAsync();

            var query = _db.CaseLawyers.AsNoTracking()
                .Where(x => x.LawyerId == lawyerId && x.Case != null &&
                    (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"));

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(x => x.Case!.Status == status);

            var items = await query
                .OrderByDescending(x => x.AssignedAt)
                .Select(x => new LawyerAssignedCaseListItemViewModel {
                    CaseId = x.CaseId,
                    CaseName = x.Case!.CaseName,
                    CaseType = x.Case.CaseType,
                    Description = x.Case.Description,
                    Status = x.Case.Status,
                    ClientName = x.Case.Creator == null ? "Client" : x.Case.Creator.FirstName + " " + x.Case.Creator.LastName,
                    AssignedAt = x.AssignedAt,
                    NextHearingDate = _db.Hearings
                        .Where(h => h.CaseId == x.CaseId && h.EndDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => (DateTime?)h.HearingDate)
                        .FirstOrDefault(),
                    NextHearingEndDate = _db.Hearings
                        .Where(h => h.CaseId == x.CaseId && h.EndDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => (DateTime?)h.EndDate)
                        .FirstOrDefault(),
                    NextHearingLocation = _db.Hearings
                        .Where(h => h.CaseId == x.CaseId && h.EndDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => h.Location)
                        .FirstOrDefault(),
                    NextHearingType = _db.Hearings
                        .Where(h => h.CaseId == x.CaseId && h.EndDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => h.HearingType)
                        .FirstOrDefault(),
                    OfficialCaseRequestStatus = _db.OfficialCaseRequests
                        .Where(r => r.CaseId == x.CaseId && r.LawyerId == lawyerId)
                        .OrderByDescending(r => r.RequestedAt)
                        .Select(r => r.Status)
                        .FirstOrDefault(),
                    HasSuccessfulPayment = _db.Payments
                        .Any(p => p.CaseId == x.CaseId && p.RequestedByLawyerId == lawyerId && p.Status == "Paid"),
                    HasPendingPayment = _db.Payments
                        .Any(p => p.CaseId == x.CaseId && p.RequestedByLawyerId == lawyerId && p.Status == "Requested")
                })
                .ToListAsync();

            ApplyHearingDisplayState(items);
            return View(new LawyerAssignedCasesViewModel { StatusFilter = status, Cases = items });
        }

        [Authorize(Roles = Roles.Lawyer), HttpGet]
        public async Task<IActionResult> LawyerDetails(int id) {
            var lawyerId = _users.GetUserId(User);
            if (lawyerId == null) return Challenge();
            await UpdateEndedHearingFollowUpsAsync();

            var item = await _db.CaseLawyers.AsNoTracking()
                .Where(x => x.LawyerId == lawyerId && x.CaseId == id && x.Case != null &&
                    (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"))
                .Select(x => new LawyerAssignedCaseListItemViewModel {
                    CaseId = x.CaseId,
                    CaseName = x.Case!.CaseName,
                    CaseType = x.Case.CaseType,
                    Description = x.Case.Description,
                    Status = x.Case.Status,
                    ClientName = x.Case.Creator == null ? "Client" : x.Case.Creator.FirstName + " " + x.Case.Creator.LastName,
                    AssignedAt = x.AssignedAt,
                    NextHearingDate = _db.Hearings
                        .Where(h => h.CaseId == x.CaseId && h.EndDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => (DateTime?)h.HearingDate)
                        .FirstOrDefault(),
                    NextHearingEndDate = _db.Hearings
                        .Where(h => h.CaseId == x.CaseId && h.EndDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => (DateTime?)h.EndDate)
                        .FirstOrDefault(),
                    NextHearingLocation = _db.Hearings
                        .Where(h => h.CaseId == x.CaseId && h.EndDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => h.Location)
                        .FirstOrDefault(),
                    NextHearingType = _db.Hearings
                        .Where(h => h.CaseId == x.CaseId && h.EndDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => h.HearingType)
                        .FirstOrDefault(),
                    OfficialCaseRequestStatus = _db.OfficialCaseRequests
                        .Where(r => r.CaseId == x.CaseId && r.LawyerId == lawyerId)
                        .OrderByDescending(r => r.RequestedAt)
                        .Select(r => r.Status)
                        .FirstOrDefault(),
                    HasSuccessfulPayment = _db.Payments
                        .Any(p => p.CaseId == x.CaseId && p.RequestedByLawyerId == lawyerId && p.Status == "Paid"),
                    HasPendingPayment = _db.Payments
                        .Any(p => p.CaseId == x.CaseId && p.RequestedByLawyerId == lawyerId && p.Status == "Requested")
                })
                .FirstOrDefaultAsync();

            if (item == null) return NotFound();
            ApplyHearingDisplayState(item);

            ViewBag.PaymentHistory = await _db.Payments.AsNoTracking()
                .Where(x => x.CaseId == id && x.RequestedByLawyerId == lawyerId)
                .OrderByDescending(x => x.RequestedAt)
                .Take(10)
                .Select(x => new CasePaymentListItemViewModel {
                    PaymentId = x.Id,
                    Amount = x.Amount,
                    BillingType = x.BillingType,
                    PaymentMethod = x.PaymentMethod,
                    Status = x.Status,
                    TransactionRef = x.TransactionRef,
                    RequestedAt = x.RequestedAt,
                    PaidAt = x.PaidAt
                })
                .ToListAsync();

            return View(item);
        }

        [Authorize(Roles = Roles.Lawyer), HttpGet]
        public async Task<IActionResult> CreatePaymentRequest(int caseId) {
            var lawyerId = _users.GetUserId(User);
            if (lawyerId == null) return Challenge();

            var assignment = await _db.CaseLawyers.AsNoTracking()
                .Where(x => x.LawyerId == lawyerId && x.CaseId == caseId && x.Case != null &&
                    (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"))
                .Select(x => new {
                    x.CaseId,
                    CaseName = x.Case!.CaseName,
                    ClientName = x.Case.Creator == null ? "Client" : x.Case.Creator.FirstName + " " + x.Case.Creator.LastName
                })
                .FirstOrDefaultAsync();

            if (assignment == null) return NotFound();

            var alreadyPaid = await _db.Payments.AsNoTracking()
                .AnyAsync(x => x.CaseId == caseId && x.RequestedByLawyerId == lawyerId && x.Status == "Paid");
            if (alreadyPaid) {
                TempData["Error"] = Text("This case already has a successful payment, so a new payment request is disabled.", "توجد دفعة ناجحة لهذه القضية، لذلك تم تعطيل طلب دفع جديد.");
                return RedirectToAction(nameof(LawyerDetails), new { id = caseId });
            }

            var pendingPayment = await _db.Payments.AsNoTracking()
                .Where(x => x.CaseId == caseId && x.RequestedByLawyerId == lawyerId && x.Status == "Requested")
                .OrderByDescending(x => x.RequestedAt)
                .FirstOrDefaultAsync();

            return View(new MockPaymentViewModel {
                CaseId = assignment.CaseId,
                PaymentId = pendingPayment?.Id,
                IsEdit = pendingPayment != null,
                CaseName = assignment.CaseName,
                ClientName = assignment.ClientName,
                Amount = pendingPayment?.Amount ?? 0,
                BillingType = pendingPayment?.BillingType ?? "OneTime",
                Note = pendingPayment?.Note
            });
        }

        [Authorize(Roles = Roles.Lawyer), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePaymentRequest(MockPaymentViewModel model) {
            var lawyer = await _users.GetUserAsync(User);
            if (lawyer == null) return Challenge();

            var assignment = await _db.CaseLawyers
                .Include(x => x.Case)
                .ThenInclude(x => x!.Creator)
                .FirstOrDefaultAsync(x => x.LawyerId == lawyer.Id && x.CaseId == model.CaseId && x.Case != null &&
                    (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"));

            if (assignment?.Case?.Creator == null) return NotFound();

            var alreadyPaid = await _db.Payments.AsNoTracking()
                .AnyAsync(x => x.CaseId == model.CaseId && x.RequestedByLawyerId == lawyer.Id && x.Status == "Paid");
            if (alreadyPaid) {
                TempData["Error"] = Text("This case already has a successful payment, so a new payment request is disabled.", "توجد دفعة ناجحة لهذه القضية، لذلك تم تعطيل طلب دفع جديد.");
                return RedirectToAction(nameof(LawyerDetails), new { id = model.CaseId });
            }

            var billingTypes = new[] { "OneTime", "Monthly", "Hourly" };
            if (!billingTypes.Contains(model.BillingType))
                ModelState.AddModelError(nameof(model.BillingType), Text("Select a valid payment type.", "اختر نوع دفع صحيح."));

            if (!ModelState.IsValid || model.Amount <= 0) {
                model.CaseName = assignment.Case.CaseName;
                model.ClientName = assignment.Case.Creator.FirstName + " " + assignment.Case.Creator.LastName;
                model.IsEdit = model.PaymentId.HasValue;
                return View(model);
            }

            var payment = model.PaymentId.HasValue
                ? await _db.Payments.FirstOrDefaultAsync(x => x.Id == model.PaymentId.Value && x.CaseId == model.CaseId && x.RequestedByLawyerId == lawyer.Id && x.Status == "Requested")
                : await _db.Payments.OrderByDescending(x => x.RequestedAt).FirstOrDefaultAsync(x => x.CaseId == model.CaseId && x.RequestedByLawyerId == lawyer.Id && x.Status == "Requested");

            var isEdit = payment != null;
            if (payment == null) {
                payment = new Payment {
                    CaseId = model.CaseId,
                    PaidById = assignment.Case.CreatedBy_Id,
                    RequestedByLawyerId = lawyer.Id,
                    PaymentMethod = "Pending",
                    Status = "Requested",
                    TransactionRef = $"REQ-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
                    PaidAt = DateTime.UtcNow
                };
                _db.Payments.Add(payment);
            }

            payment.Amount = model.Amount;
            payment.BillingType = model.BillingType;
            payment.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();
            payment.RequestedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var ar = assignment.Case.Creator.PreferredLanguage == "ar";
            var body = BuildPaymentRequestBody(lawyer, assignment.Case.CaseName, payment, ar);
            var existingMessage = await _db.ChatMessages
                .FirstOrDefaultAsync(x => x.PaymentId == payment.Id && x.MessageType == "PaymentRequest");

            if (existingMessage == null) {
                _db.ChatMessages.Add(new ChatMessage {
                    SenderId = lawyer.Id,
                    ReceiverId = assignment.Case.CreatedBy_Id,
                    Body = body,
                    MessageType = "PaymentRequest",
                    RelatedCaseId = model.CaseId,
                    PaymentId = payment.Id,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }
            else {
                existingMessage.Body = body;
                existingMessage.IsRead = false;
                existingMessage.CreatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = isEdit
                ? Text("The pending payment request was updated in the same client DM card.", "تم تعديل طلب الدفع المعلق في نفس بطاقة رسائل العميل.")
                : Text("The payment request was submitted and sent to the client DM.", "تم إرسال طلب الدفع إلى رسائل العميل.");

            return RedirectToAction(nameof(LawyerDetails), new { id = model.CaseId });
        }

        // Backward-compatible route for older views/bookmarks from the previous mock-payment phase.
        [Authorize(Roles = Roles.Lawyer), HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> CreateMockPayment(MockPaymentViewModel model) => CreatePaymentRequest(model);

        private static string BuildPaymentRequestBody(ApplicationUser lawyer, string caseName, Payment payment, bool ar) {
            var billing = BillingText(payment.BillingType, ar);
            var note = string.IsNullOrWhiteSpace(payment.Note) ? string.Empty : $"\n{(ar ? "ملاحظة المحامي" : "Lawyer note")}: {payment.Note}";
            return ar
                ? $"أرسل لك المحامي {lawyer.FirstName} {lawyer.LastName} طلب دفع للقضية: {caseName}.\nالمبلغ: {payment.Amount:0.00}\nنوع الدفع: {billing}\nرقم الطلب: {payment.TransactionRef}{note}"
                : $"Lawyer {lawyer.FirstName} {lawyer.LastName} sent you a payment request for case: {caseName}.\nAmount: {payment.Amount:0.00}\nPayment type: {billing}\nReference: {payment.TransactionRef}{note}";
        }

        private async Task UpdateEndedHearingFollowUpsAsync() {
            var now = DateTime.Now;
            var ended = await _db.Hearings
                .Include(h => h.Case)
                .Where(h => h.Status == "Scheduled" && h.EndDate <= now && h.CourtFollowUpNotifiedAt == null)
                .Take(25)
                .ToListAsync();

            if (ended.Count == 0) return;

            foreach (var hearing in ended) {
                hearing.CourtFollowUpNotifiedAt = DateTime.UtcNow;
                if (hearing.Case != null && hearing.Case.Status == "In Progress")
                    hearing.Case.Status = "Waiting for next hearing date";

                _db.SystemNotifications.Add(new SystemNotification {
                    RecipientId = hearing.ScheduledById,
                    Title = "Hearing needs follow-up",
                    Body = $"The hearing for case '{hearing.Case?.CaseName ?? hearing.CaseId.ToString()}' ended. Please close the case or schedule the next hearing date.",
                    Category = "HearingFollowUp",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        private static void ApplyHearingDisplayState(IEnumerable<ClientCaseListItemViewModel> items) {
            foreach (var item in items) ApplyHearingDisplayState(item);
        }

        private static void ApplyHearingDisplayState(IEnumerable<LawyerAssignedCaseListItemViewModel> items) {
            foreach (var item in items) ApplyHearingDisplayState(item);
        }

        private static void ApplyHearingDisplayState(ClientCaseListItemViewModel item) {
            item.HearingCountdownPhase = HearingPhase(item.NextHearingDate, item.NextHearingEndDate);
            ApplyWaitingStatus(item);
        }

        private static void ApplyHearingDisplayState(LawyerAssignedCaseListItemViewModel item) {
            item.HearingCountdownPhase = HearingPhase(item.NextHearingDate, item.NextHearingEndDate);
            ApplyWaitingStatus(item);
        }

        private static void ApplyWaitingStatus(ClientCaseListItemViewModel item) {
            if (item.Status == "In Progress" && !item.NextHearingDate.HasValue)
                item.Status = "Waiting for next hearing date";
        }

        private static void ApplyWaitingStatus(LawyerAssignedCaseListItemViewModel item) {
            if (item.Status == "In Progress" && !item.NextHearingDate.HasValue)
                item.Status = "Waiting for next hearing date";
        }

        private static string HearingPhase(DateTime? start, DateTime? end) {
            if (!start.HasValue || !end.HasValue) return string.Empty;
            var now = DateTime.Now;
            if (now < start.Value) return "BeforeStart";
            if (now <= end.Value) return "InSession";
            return "Ended";
        }

        private async Task LoadClientsAsync(string? selectedClientId = null) {
            var clients = await _users.GetUsersInRoleAsync(Roles.Client);
            ViewBag.Clients = new SelectList(
                clients.OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
                    .Select(x => new { x.Id, Name = $"{x.FirstName} {x.LastName} — {x.Email}" }),
                "Id",
                "Name",
                selectedClientId);
        }

        private string Text(string en, string ar) => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? ar : en;

        private static string BillingText(string billingType, bool ar) {
            return billingType switch {
                "Monthly" => ar ? "شهري" : "per month",
                "Hourly" => ar ? "بالساعة" : "per hour",
                _ => ar ? "مرة واحدة / لكل قضية" : "one time / per case"
            };
        }
    }
}
