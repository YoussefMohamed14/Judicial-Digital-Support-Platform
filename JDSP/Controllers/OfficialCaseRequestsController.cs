using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.OfficialCaseRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace JDSP.Controllers {
    [Authorize]
    public class OfficialCaseRequestsController : Controller {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public OfficialCaseRequestsController(ApplicationDbContext db, UserManager<ApplicationUser> users) {
            _db = db;
            _users = users;
        }

        [Authorize(Roles = Roles.Lawyer), HttpGet]
        public async Task<IActionResult> Create(int caseId) {
            var lawyerId = _users.GetUserId(User);
            if (lawyerId == null) return Challenge();

            var model = await _db.CaseLawyers.AsNoTracking()
                .Where(x => x.CaseId == caseId && x.LawyerId == lawyerId && x.Case != null &&
                    (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"))
                .Select(x => new OfficialCaseRequestCreateViewModel {
                    CaseId = x.CaseId,
                    CaseName = x.Case!.CaseName,
                    ClientName = x.Case.Creator == null ? "Client" : x.Case.Creator.FirstName + " " + x.Case.Creator.LastName
                })
                .FirstOrDefaultAsync();

            if (model == null) return NotFound();

            var pending = await _db.OfficialCaseRequests.AsNoTracking()
                .AnyAsync(x => x.CaseId == caseId && x.LawyerId == lawyerId && x.Status == VerificationStatus.Pending);
            if (pending) {
                TempData["Error"] = Text("There is already a pending official-case request for this case.", "يوجد بالفعل طلب معلق لهذه القضية.");
                return RedirectToAction("LawyerDetails", "Cases", new { id = caseId });
            }

            return View(model);
        }

        [Authorize(Roles = Roles.Lawyer), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OfficialCaseRequestCreateViewModel model) {
            var lawyer = await _users.GetUserAsync(User);
            if (lawyer == null) return Challenge();

            var assignment = await _db.CaseLawyers
                .Include(x => x.Case)
                .ThenInclude(x => x!.Creator)
                .FirstOrDefaultAsync(x => x.CaseId == model.CaseId && x.LawyerId == lawyer.Id && x.Case != null &&
                    (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"));

            if (assignment?.Case == null) return NotFound();

            var pending = await _db.OfficialCaseRequests.AsNoTracking()
                .AnyAsync(x => x.CaseId == model.CaseId && x.LawyerId == lawyer.Id && x.Status == VerificationStatus.Pending);
            if (pending)
                ModelState.AddModelError(string.Empty, Text("There is already a pending request for this case.", "يوجد بالفعل طلب معلق لهذه القضية."));

            if (!ModelState.IsValid) {
                model.CaseName = assignment.Case.CaseName;
                model.ClientName = assignment.Case.Creator == null ? "Client" : assignment.Case.Creator.FirstName + " " + assignment.Case.Creator.LastName;
                return View(model);
            }

            _db.OfficialCaseRequests.Add(new OfficialCaseRequest {
                CaseId = model.CaseId,
                LawyerId = lawyer.Id,
                Reason = model.Reason.Trim(),
                Status = VerificationStatus.Pending,
                RequestedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = Text("Your official-case and hearing request was sent to the Court Employee.", "تم إرسال طلب جعل القضية رسمية وجدولة جلسة إلى موظف المحكمة.");
            return RedirectToAction("LawyerDetails", "Cases", new { id = model.CaseId });
        }

        [Authorize(Roles = Roles.CourtEmployee + "," + Roles.Admin), HttpGet]
        public async Task<IActionResult> Index(string? status = null) {
            var query = _db.OfficialCaseRequests.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);

            var items = await query
                .OrderByDescending(x => x.RequestedAt)
                .Select(x => new OfficialCaseRequestListItemViewModel {
                    RequestId = x.Id,
                    CaseId = x.CaseId,
                    CaseName = x.Case == null ? string.Empty : x.Case.CaseName,
                    LawyerName = x.Lawyer == null ? "Lawyer" : x.Lawyer.FirstName + " " + x.Lawyer.LastName,
                    ClientName = x.Case == null || x.Case.Creator == null ? "Client" : x.Case.Creator.FirstName + " " + x.Case.Creator.LastName,
                    Status = x.Status,
                    RequestedAt = x.RequestedAt
                })
                .ToListAsync();

            ViewBag.Status = status;
            return View(items);
        }

        [Authorize(Roles = Roles.CourtEmployee + "," + Roles.Admin), HttpGet]
        public async Task<IActionResult> Details(int id) {
            var item = await LoadDetailsAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [Authorize(Roles = Roles.CourtEmployee + "," + Roles.Admin), HttpGet]
        public async Task<IActionResult> Approve(int id) {
            var request = await _db.OfficialCaseRequests.AsNoTracking()
                .Where(x => x.Id == id && x.Status == VerificationStatus.Pending)
                .Select(x => new {
                    x.Id,
                    CaseName = x.Case == null ? string.Empty : x.Case.CaseName,
                    LawyerName = x.Lawyer == null ? "Lawyer" : x.Lawyer.FirstName + " " + x.Lawyer.LastName,
                    ClientName = x.Case == null || x.Case.Creator == null ? "Client" : x.Case.Creator.FirstName + " " + x.Case.Creator.LastName
                })
                .FirstOrDefaultAsync();

            if (request == null) return NotFound();

            var defaultDate = DateTime.Now.AddDays(14);
            var defaultHearingDate = new DateTime(defaultDate.Year, defaultDate.Month, defaultDate.Day, 10, 0, 0);

            var item = new OfficialCaseRequestApproveViewModel {
                RequestId = request.Id,
                CaseName = request.CaseName,
                LawyerName = request.LawyerName,
                ClientName = request.ClientName,
                HearingDate = defaultHearingDate,
                HearingEndDate = defaultHearingDate.AddHours(2),
                HearingType = "Physical"
            };

            return View(item);
        }

        [Authorize(Roles = Roles.CourtEmployee + "," + Roles.Admin), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(OfficialCaseRequestApproveViewModel model) {
            var employee = await _users.GetUserAsync(User);
            if (employee == null) return Challenge();

            var request = await _db.OfficialCaseRequests
                .Include(x => x.Case)
                .ThenInclude(x => x!.Creator)
                .Include(x => x.Lawyer)
                .FirstOrDefaultAsync(x => x.Id == model.RequestId && x.Status == VerificationStatus.Pending);

            if (request?.Case?.Creator == null || request.Lawyer == null) return NotFound();
            if (model.HearingDate <= DateTime.Now)
                ModelState.AddModelError(nameof(model.HearingDate), Text("Choose a future hearing start date.", "اختر وقت بداية جلسة في المستقبل."));
            if (model.HearingEndDate <= model.HearingDate)
                ModelState.AddModelError(nameof(model.HearingEndDate), Text("The hearing end time must be after the start time.", "يجب أن يكون وقت نهاية الجلسة بعد وقت البداية."));

            if (!ModelState.IsValid) {
                model.CaseName = request.Case.CaseName;
                model.LawyerName = request.Lawyer.FirstName + " " + request.Lawyer.LastName;
                model.ClientName = request.Case.Creator.FirstName + " " + request.Case.Creator.LastName;
                return View(model);
            }

            request.Status = VerificationStatus.Approved;
            request.ReviewedById = employee.Id;
            request.ReviewedAt = DateTime.UtcNow;
            request.HearingDate = model.HearingDate;
            request.HearingEndDate = model.HearingEndDate;
            request.HearingType = model.HearingType;
            request.Location = model.Location;
            request.CourtNotes = model.CourtNotes;
            request.Case.Status = "In Progress";

            _db.Hearings.Add(new Hearing {
                CaseId = request.CaseId,
                HearingDate = model.HearingDate,
                EndDate = model.HearingEndDate,
                HearingType = model.HearingType,
                Location = model.Location,
                Status = "Scheduled",
                ScheduledById = employee.Id,
                CreatedAt = DateTime.UtcNow
            });

            AddSystemNotification(request.Lawyer.Id, "Official case approved", "تم قبول طلب القضية الرسمية",
                $"Your request for case '{request.Case.CaseName}' was approved. Hearing: {model.HearingDate:yyyy-MM-dd HH:mm} - {model.HearingEndDate:HH:mm}. Location: {model.Location ?? "To be confirmed"}.",
                $"تم قبول طلب جعل القضية '{request.Case.CaseName}' رسمية. موعد الجلسة: {model.HearingDate:yyyy-MM-dd HH:mm} - {model.HearingEndDate:HH:mm}. المكان: {model.Location ?? "سيتم التأكيد"}.",
                request.Lawyer.PreferredLanguage);

            AddSystemNotification(request.Case.CreatedBy_Id, "Hearing scheduled", "تم تحديد جلسة",
                $"A hearing was scheduled for case '{request.Case.CaseName}' on {model.HearingDate:yyyy-MM-dd HH:mm} - {model.HearingEndDate:HH:mm}. Location: {model.Location ?? "To be confirmed"}.",
                $"تم تحديد جلسة للقضية '{request.Case.CaseName}' بتاريخ {model.HearingDate:yyyy-MM-dd HH:mm} - {model.HearingEndDate:HH:mm}. المكان: {model.Location ?? "سيتم التأكيد"}.",
                request.Case.Creator.PreferredLanguage);

            await _db.SaveChangesAsync();
            TempData["Success"] = Text("The request was approved and the hearing was scheduled.", "تم قبول الطلب وجدولة الجلسة.");
            return RedirectToAction(nameof(Details), new { id = request.Id });
        }

        [Authorize(Roles = Roles.CourtEmployee + "," + Roles.Admin), HttpGet]
        public async Task<IActionResult> Reject(int id) {
            var item = await _db.OfficialCaseRequests.AsNoTracking()
                .Where(x => x.Id == id && x.Status == VerificationStatus.Pending)
                .Select(x => new OfficialCaseRequestRejectViewModel {
                    RequestId = x.Id,
                    CaseName = x.Case == null ? string.Empty : x.Case.CaseName,
                    LawyerName = x.Lawyer == null ? "Lawyer" : x.Lawyer.FirstName + " " + x.Lawyer.LastName
                })
                .FirstOrDefaultAsync();

            if (item == null) return NotFound();
            return View(item);
        }

        [Authorize(Roles = Roles.CourtEmployee + "," + Roles.Admin), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(OfficialCaseRequestRejectViewModel model) {
            var employee = await _users.GetUserAsync(User);
            if (employee == null) return Challenge();

            var request = await _db.OfficialCaseRequests
                .Include(x => x.Case)
                .Include(x => x.Lawyer)
                .FirstOrDefaultAsync(x => x.Id == model.RequestId && x.Status == VerificationStatus.Pending);

            if (request?.Case == null || request.Lawyer == null) return NotFound();
            if (!ModelState.IsValid) {
                model.CaseName = request.Case.CaseName;
                model.LawyerName = request.Lawyer.FirstName + " " + request.Lawyer.LastName;
                return View(model);
            }

            request.Status = VerificationStatus.Rejected;
            request.ReviewedById = employee.Id;
            request.ReviewedAt = DateTime.UtcNow;
            request.RejectionReason = model.RejectionReason.Trim();

            AddSystemNotification(request.LawyerId, "Official case request rejected", "تم رفض طلب القضية الرسمية",
                $"Your official-case request for '{request.Case.CaseName}' was rejected. Reason: {request.RejectionReason}",
                $"تم رفض طلب جعل القضية '{request.Case.CaseName}' رسمية. السبب: {request.RejectionReason}",
                request.Lawyer.PreferredLanguage);

            await _db.SaveChangesAsync();
            TempData["Success"] = Text("The request was rejected and the lawyer was notified.", "تم رفض الطلب وإبلاغ المحامي.");
            return RedirectToAction(nameof(Details), new { id = request.Id });
        }

        private async Task<OfficialCaseRequestDetailsViewModel?> LoadDetailsAsync(int id) {
            return await _db.OfficialCaseRequests.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new OfficialCaseRequestDetailsViewModel {
                    RequestId = x.Id,
                    CaseId = x.CaseId,
                    CaseName = x.Case == null ? string.Empty : x.Case.CaseName,
                    LawyerName = x.Lawyer == null ? "Lawyer" : x.Lawyer.FirstName + " " + x.Lawyer.LastName,
                    ClientName = x.Case == null || x.Case.Creator == null ? "Client" : x.Case.Creator.FirstName + " " + x.Case.Creator.LastName,
                    Status = x.Status,
                    RequestedAt = x.RequestedAt,
                    Reason = x.Reason,
                    RejectionReason = x.RejectionReason,
                    ReviewedAt = x.ReviewedAt,
                    HearingDate = x.HearingDate,
                    HearingEndDate = x.HearingEndDate,
                    HearingType = x.HearingType,
                    Location = x.Location,
                    CourtNotes = x.CourtNotes
                })
                .FirstOrDefaultAsync();
        }

        private void AddSystemNotification(string userId, string titleEn, string titleAr, string bodyEn, string bodyAr, string? language) {
            var ar = language == "ar";
            _db.SystemNotifications.Add(new SystemNotification {
                RecipientId = userId,
                Title = ar ? titleAr : titleEn,
                Body = ar ? bodyAr : bodyEn,
                Category = "OfficialCaseRequest",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        private string Text(string en, string ar) => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? ar : en;
    }
}
