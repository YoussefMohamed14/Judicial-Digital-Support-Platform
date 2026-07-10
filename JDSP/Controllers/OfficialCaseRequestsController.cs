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


        [Authorize(Roles = Roles.CourtEmployee + "," + Roles.Admin), HttpGet]
        public async Task<IActionResult> HearingFollowUps() {
            await UpdateEndedHearingFollowUpsAsync();
            var waitingCases = await BuildHearingFollowUpListQuery().ToListAsync();
            return View(waitingCases);
        }

        [Authorize(Roles = Roles.CourtEmployee + "," + Roles.Admin), HttpGet]
        public async Task<IActionResult> ManageHearingFollowUp(int caseId) {
            await UpdateEndedHearingFollowUpsAsync();
            var model = await BuildHearingFollowUpDecisionModelAsync(caseId);
            if (model == null) return NotFound();

            var defaultDate = DateTime.Now.AddDays(14);
            var defaultStart = new DateTime(defaultDate.Year, defaultDate.Month, defaultDate.Day, 10, 0, 0);
            model.HearingDate = defaultStart;
            model.HearingEndDate = defaultStart.AddHours(2);
            model.HearingType = "Physical";
            return View(model);
        }

        [Authorize(Roles = Roles.CourtEmployee + "," + Roles.Admin), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageHearingFollowUp(HearingFollowUpDecisionViewModel model) {
            var employee = await _users.GetUserAsync(User);
            if (employee == null) return Challenge();

            var caseItem = await _db.Cases
                .Include(c => c.Creator)
                .FirstOrDefaultAsync(c => c.CaseID == model.CaseId &&
                    (c.Status == "Waiting for next hearing date" || c.Status == "Postponed" || c.Status == "In Progress"));

            if (caseItem == null) return NotFound();

            var latestHearing = await _db.Hearings
                .Where(h => h.CaseId == model.CaseId)
                .OrderByDescending(h => h.HearingDate)
                .FirstOrDefaultAsync();

            var lawyerIds = await _db.CaseLawyers.AsNoTracking()
                .Where(x => x.CaseId == model.CaseId &&
                    (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"))
                .Select(x => x.LawyerId)
                .Distinct()
                .ToListAsync();

            var decision = model.Decision?.Trim() ?? string.Empty;
            if (decision != "ScheduleNext" && decision != "Postpone" && decision != "Close")
                ModelState.AddModelError(nameof(model.Decision), Text("Choose a valid follow-up decision.", "اختر قرار متابعة صحيح."));

            if (decision == "ScheduleNext") {
                if (!model.HearingDate.HasValue)
                    ModelState.AddModelError(nameof(model.HearingDate), Text("Choose the next hearing start time.", "اختر وقت بداية الجلسة القادمة."));
                if (!model.HearingEndDate.HasValue)
                    ModelState.AddModelError(nameof(model.HearingEndDate), Text("Choose the next hearing end time.", "اختر وقت نهاية الجلسة القادمة."));
                if (model.HearingDate.HasValue && model.HearingDate.Value <= DateTime.Now)
                    ModelState.AddModelError(nameof(model.HearingDate), Text("Choose a future hearing start time.", "اختر وقت بداية جلسة في المستقبل."));
                if (model.HearingDate.HasValue && model.HearingEndDate.HasValue && model.HearingEndDate.Value <= model.HearingDate.Value)
                    ModelState.AddModelError(nameof(model.HearingEndDate), Text("The hearing end time must be after the start time.", "يجب أن يكون وقت نهاية الجلسة بعد وقت البداية."));
            }

            if (!ModelState.IsValid) {
                await FillHearingFollowUpDisplayFieldsAsync(model);
                return View(model);
            }

            if (latestHearing != null && latestHearing.EndDate <= DateTime.Now && latestHearing.Status == "Scheduled")
                latestHearing.Status = "Completed";

            if (decision == "ScheduleNext") {
                caseItem.Status = "In Progress";
                _db.Hearings.Add(new Hearing {
                    CaseId = caseItem.CaseID,
                    HearingDate = model.HearingDate!.Value,
                    EndDate = model.HearingEndDate!.Value,
                    HearingType = string.IsNullOrWhiteSpace(model.HearingType) ? "Physical" : model.HearingType,
                    Location = model.Location,
                    Status = "Scheduled",
                    ScheduledById = employee.Id,
                    CreatedAt = DateTime.UtcNow
                });

                await NotifyCaseParticipantsAsync(caseItem, lawyerIds,
                    "Next hearing scheduled", "تم تحديد الجلسة القادمة",
                    $"The next hearing for case '{caseItem.CaseName}' was scheduled on {model.HearingDate:yyyy-MM-dd HH:mm} - {model.HearingEndDate:HH:mm}. Location: {model.Location ?? "To be confirmed"}.",
                    $"تم تحديد الجلسة القادمة للقضية '{caseItem.CaseName}' بتاريخ {model.HearingDate:yyyy-MM-dd HH:mm} - {model.HearingEndDate:HH:mm}. المكان: {model.Location ?? "سيتم التأكيد"}.");

                TempData["Success"] = Text("The next hearing was scheduled and both parties were notified.", "تم تحديد الجلسة القادمة وإبلاغ الطرفين.");
            }
            else if (decision == "Postpone") {
                caseItem.Status = "Postponed";
                await NotifyCaseParticipantsAsync(caseItem, lawyerIds,
                    "Case hearing postponed", "تم تأجيل الجلسة",
                    $"Case '{caseItem.CaseName}' was postponed. Notes: {model.CourtNotes ?? "No notes"}",
                    $"تم تأجيل القضية '{caseItem.CaseName}'. الملاحظات: {model.CourtNotes ?? "لا توجد ملاحظات"}");

                TempData["Success"] = Text("The case was marked as postponed and both parties were notified.", "تم وضع القضية كـ مؤجلة وإبلاغ الطرفين.");
            }
            else {
                caseItem.Status = "Closed";
                await NotifyCaseParticipantsAsync(caseItem, lawyerIds,
                    "Case closed", "تم إغلاق القضية",
                    $"Case '{caseItem.CaseName}' was closed by the Court Employee. Notes: {model.CourtNotes ?? "No notes"}",
                    $"تم إغلاق القضية '{caseItem.CaseName}' بواسطة موظف المحكمة. الملاحظات: {model.CourtNotes ?? "لا توجد ملاحظات"}");

                TempData["Success"] = Text("The case was closed and both parties were notified.", "تم إغلاق القضية وإبلاغ الطرفين.");
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(HearingFollowUps));
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


        private IQueryable<HearingFollowUpListItemViewModel> BuildHearingFollowUpListQuery() {
            return _db.Cases.AsNoTracking()
                .Where(c => c.Status == "Waiting for next hearing date" || c.Status == "Postponed")
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new HearingFollowUpListItemViewModel {
                    CaseId = c.CaseID,
                    CaseName = c.CaseName,
                    CaseType = c.CaseType,
                    CaseStatus = c.Status,
                    ClientName = c.Creator == null ? "Client" : c.Creator.FirstName + " " + c.Creator.LastName,
                    LawyerName = _db.CaseLawyers
                        .Where(cl => cl.CaseId == c.CaseID && cl.Lawyer != null &&
                            (cl.Status == "Accepted" || cl.Status == "Price Proposed" || cl.Status == "OfferAccepted"))
                        .OrderByDescending(cl => cl.AssignedAt)
                        .Select(cl => cl.Lawyer!.FirstName + " " + cl.Lawyer.LastName)
                        .FirstOrDefault() ?? "Lawyer",
                    LastHearingStart = _db.Hearings
                        .Where(h => h.CaseId == c.CaseID)
                        .OrderByDescending(h => h.HearingDate)
                        .Select(h => (DateTime?)h.HearingDate)
                        .FirstOrDefault(),
                    LastHearingEnd = _db.Hearings
                        .Where(h => h.CaseId == c.CaseID)
                        .OrderByDescending(h => h.HearingDate)
                        .Select(h => (DateTime?)h.EndDate)
                        .FirstOrDefault(),
                    LastHearingType = _db.Hearings
                        .Where(h => h.CaseId == c.CaseID)
                        .OrderByDescending(h => h.HearingDate)
                        .Select(h => h.HearingType)
                        .FirstOrDefault(),
                    LastHearingLocation = _db.Hearings
                        .Where(h => h.CaseId == c.CaseID)
                        .OrderByDescending(h => h.HearingDate)
                        .Select(h => h.Location)
                        .FirstOrDefault()
                });
        }

        private async Task<HearingFollowUpDecisionViewModel?> BuildHearingFollowUpDecisionModelAsync(int caseId) {
            var model = await _db.Cases.AsNoTracking()
                .Where(c => c.CaseID == caseId && (c.Status == "Waiting for next hearing date" || c.Status == "Postponed" || c.Status == "In Progress"))
                .Select(c => new HearingFollowUpDecisionViewModel {
                    CaseId = c.CaseID,
                    CaseName = c.CaseName,
                    CaseType = c.CaseType,
                    CaseStatus = c.Status,
                    ClientName = c.Creator == null ? "Client" : c.Creator.FirstName + " " + c.Creator.LastName,
                    LawyerName = _db.CaseLawyers
                        .Where(cl => cl.CaseId == c.CaseID && cl.Lawyer != null &&
                            (cl.Status == "Accepted" || cl.Status == "Price Proposed" || cl.Status == "OfferAccepted"))
                        .OrderByDescending(cl => cl.AssignedAt)
                        .Select(cl => cl.Lawyer!.FirstName + " " + cl.Lawyer.LastName)
                        .FirstOrDefault() ?? "Lawyer",
                    LastHearingStart = _db.Hearings
                        .Where(h => h.CaseId == c.CaseID)
                        .OrderByDescending(h => h.HearingDate)
                        .Select(h => (DateTime?)h.HearingDate)
                        .FirstOrDefault(),
                    LastHearingEnd = _db.Hearings
                        .Where(h => h.CaseId == c.CaseID)
                        .OrderByDescending(h => h.HearingDate)
                        .Select(h => (DateTime?)h.EndDate)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            return model;
        }

        private async Task FillHearingFollowUpDisplayFieldsAsync(HearingFollowUpDecisionViewModel model) {
            var details = await BuildHearingFollowUpDecisionModelAsync(model.CaseId);
            if (details == null) return;
            model.CaseName = details.CaseName;
            model.CaseType = details.CaseType;
            model.ClientName = details.ClientName;
            model.LawyerName = details.LawyerName;
            model.CaseStatus = details.CaseStatus;
            model.LastHearingStart = details.LastHearingStart;
            model.LastHearingEnd = details.LastHearingEnd;
        }

        private async Task UpdateEndedHearingFollowUpsAsync() {
            var now = DateTime.Now;
            var ended = await _db.Hearings
                .Include(h => h.Case)
                .Where(h => h.Status == "Scheduled" && h.EndDate <= now && h.CourtFollowUpNotifiedAt == null)
                .Take(50)
                .ToListAsync();

            if (ended.Count == 0) return;

            foreach (var hearing in ended) {
                hearing.CourtFollowUpNotifiedAt = DateTime.UtcNow;
                if (hearing.Case != null && hearing.Case.Status == "In Progress")
                    hearing.Case.Status = "Waiting for next hearing date";

                _db.SystemNotifications.Add(new SystemNotification {
                    RecipientId = hearing.ScheduledById,
                    Title = "Hearing needs follow-up",
                    Body = $"The hearing for case '{hearing.Case?.CaseName ?? hearing.CaseId.ToString()}' ended. Please close the case, postpone it, or schedule the next hearing date.",
                    Category = "HearingFollowUp",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        private async Task NotifyCaseParticipantsAsync(Case caseItem, IReadOnlyCollection<string> lawyerIds, string titleEn, string titleAr, string bodyEn, string bodyAr) {
            var recipientIds = lawyerIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Append(caseItem.CreatedBy_Id)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var recipients = await _db.Users.AsNoTracking()
                .Where(u => recipientIds.Contains(u.Id))
                .Select(u => new { u.Id, u.PreferredLanguage })
                .ToListAsync();

            foreach (var recipient in recipients) {
                var isAr = recipient.PreferredLanguage == "ar";
                _db.SystemNotifications.Add(new SystemNotification {
                    RecipientId = recipient.Id,
                    Title = isAr ? titleAr : titleEn,
                    Body = isAr ? bodyAr : bodyEn,
                    Category = "HearingFollowUp",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
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
