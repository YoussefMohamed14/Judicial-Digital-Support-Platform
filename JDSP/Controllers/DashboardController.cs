using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Dashboard;
using JDSP.ViewModels.CourtEmployee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JDSP.Controllers {
    [Authorize]
    public class DashboardController : Controller {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> users) {
            _db = db;
            _users = users;
        }

        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> AdminDashboard() {
            var model = await BuildCourtReviewDashboardAsync();
            return View(model);
        }

        [Authorize(Roles = Roles.CourtEmployee)]
        public async Task<IActionResult> CourtEmployeeDashboard() {
            var model = await BuildCourtReviewDashboardAsync();
            return View(model);
        }

        private async Task<CourtEmployeeDashboardViewModel> BuildCourtReviewDashboardAsync() {
            await UpdateEndedHearingFollowUpsAsync();
            var clients = await _users.GetUsersInRoleAsync(Roles.Client);

            return new CourtEmployeeDashboardViewModel {
                PendingLawyerApprovals = await _db.LawyerVerificationRequests.CountAsync(x => x.Status == VerificationStatus.Pending),
                ApprovedLawyers = await _db.LawyerVerificationRequests.CountAsync(x => x.Status == VerificationStatus.Approved),
                RejectedLawyers = await _db.LawyerVerificationRequests.CountAsync(x => x.Status == VerificationStatus.Rejected),
                PendingIdentityChangeRequests = await _db.IdentityChangeRequests.CountAsync(x => x.Status == VerificationStatus.Pending),
                PendingOfficialCaseRequests = await _db.OfficialCaseRequests.CountAsync(x => x.Status == VerificationStatus.Pending),
                WaitingForHearingFollowUp = await _db.Cases.CountAsync(x => x.Status == "Waiting for next hearing date"),
                TotalClients = clients.Count,
                TotalCases = await _db.Cases.CountAsync(),
                PendingLawyers = await _db.LawyerVerificationRequests.AsNoTracking()
                    .Where(x => x.Status == VerificationStatus.Pending && x.Lawyer != null)
                    .OrderBy(x => x.RequestedAt)
                    .Take(10)
                    .Select(x => new PendingLawyerApprovalItemViewModel {
                        RequestId = x.Id,
                        LawyerName = x.Lawyer!.FirstName + " " + x.Lawyer.LastName,
                        Email = x.Lawyer.Email ?? string.Empty,
                        NationalNumber = x.Lawyer.NationalNumber,
                        RequestedAt = x.RequestedAt
                    })
                    .ToListAsync(),
                PendingIdentityChanges = await _db.IdentityChangeRequests.AsNoTracking()
                    .Where(x => x.Status == VerificationStatus.Pending && x.RequestedBy != null)
                    .OrderBy(x => x.RequestedAt)
                    .Take(10)
                    .Select(x => new PendingIdentityChangeItemViewModel {
                        RequestId = x.Id,
                        RequestedByName = x.RequestedBy!.FirstName + " " + x.RequestedBy.LastName,
                        Email = x.RequestedBy.Email ?? string.Empty,
                        RequestedFullName = x.RequestedFullName,
                        RequestedAt = x.RequestedAt
                    })
                    .ToListAsync(),
                PendingOfficialCaseRequestsList = await _db.OfficialCaseRequests.AsNoTracking()
                    .Where(x => x.Status == VerificationStatus.Pending && x.Case != null && x.Lawyer != null)
                    .OrderBy(x => x.RequestedAt)
                    .Take(10)
                    .Select(x => new PendingOfficialCaseRequestItemViewModel {
                        RequestId = x.Id,
                        CaseName = x.Case!.CaseName,
                        LawyerName = x.Lawyer!.FirstName + " " + x.Lawyer.LastName,
                        ClientName = x.Case.Creator == null ? "Client" : x.Case.Creator.FirstName + " " + x.Case.Creator.LastName,
                        RequestedAt = x.RequestedAt
                    })
                    .ToListAsync()
            };
        }

        [Authorize(Roles = Roles.Lawyer)]
        public async Task<IActionResult> LawyerDashboard() {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();
            await UpdateEndedHearingFollowUpsAsync();

            var activeCaseAssignments = _db.CaseLawyers.AsNoTracking()
                .Where(x => x.LawyerId == user.Id &&
                    (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"));

            var assignedCaseIds = activeCaseAssignments.Select(x => x.CaseId);
            var nextHearing = await _db.Hearings.AsNoTracking()
                .Where(h => assignedCaseIds.Contains(h.CaseId) && h.EndDate >= DateTime.Now && h.Status == "Scheduled")
                .OrderBy(h => h.HearingDate)
                .Select(h => new {
                    h.HearingDate,
                    h.EndDate,
                    h.Location,
                    CaseName = h.Case == null ? string.Empty : h.Case.CaseName
                })
                .FirstOrDefaultAsync();

            var caseClientIds = await activeCaseAssignments
                .Where(x => x.Case != null)
                .Select(x => x.Case!.CreatedBy_Id)
                .Distinct()
                .ToListAsync();

            var requestClientIds = await _db.LegalServiceRequests.AsNoTracking()
                .Where(x => x.LawyerId == user.Id && x.Status == "Accepted")
                .Select(x => x.ClientId)
                .Distinct()
                .ToListAsync();

            var model = new LawyerDashboardViewModel {
                LawyerName = $"{user.FirstName} {user.LastName}",
                Followers = await _db.LawyerFollows.CountAsync(x => x.LawyerId == user.Id),
                CurrentClients = caseClientIds.Concat(requestClientIds).Distinct().Count(),
                PendingRequests = await _db.LegalServiceRequests.CountAsync(x => x.LawyerId == user.Id && x.RequestType == "Direct" && x.Status == "Pending"),
                AssignedCases = await assignedCaseIds.Distinct().CountAsync(),
                AvailableBalance = await _db.Payments
                    .Where(x => x.RequestedByLawyerId == user.Id && x.Status == "Paid")
                    .SumAsync(x => (decimal?)(x.Amount - x.LawyerWithdrawnAmount)) ?? 0,
                NextHearingDate = nextHearing?.HearingDate,
                NextHearingEndDate = nextHearing?.EndDate,
                HearingCountdownPhase = HearingPhase(nextHearing?.HearingDate, nextHearing?.EndDate),
                NextHearingLocation = nextHearing?.Location,
                NextHearingCaseName = nextHearing?.CaseName,
                RecentRequests = await _db.LegalServiceRequests.AsNoTracking()
                    .Where(x => x.LawyerId == user.Id && x.RequestType == "Direct")
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(5)
                    .Select(x => new LawyerDashboardRequestViewModel {
                        RequestId = x.LegalServiceRequestId,
                        Subject = x.Subject,
                        ClientName = x.Client.FirstName + " " + x.Client.LastName,
                        RequestType = x.RequestType,
                        Status = x.Status,
                        CreatedAt = x.CreatedAt
                    })
                    .ToListAsync()
            };

            return View(model);
        }

        [Authorize(Roles = Roles.Client)]
        public async Task<IActionResult> ClientDashboard() {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();
            await UpdateEndedHearingFollowUpsAsync();

            var cases = _db.Cases.AsNoTracking().Where(c => c.CreatedBy_Id == user.Id);
            var next = await _db.Hearings.AsNoTracking()
                .Where(h => h.Case != null && h.Case.CreatedBy_Id == user.Id && h.EndDate >= DateTime.Now && h.Status == "Scheduled")
                .OrderBy(h => h.HearingDate)
                .Select(h => new { h.HearingDate, h.EndDate, h.Location, Name = h.Case!.CaseName })
                .FirstOrDefaultAsync();

            var model = new ClientDashboardViewModel {
                ClientName = $"{user.FirstName} {user.LastName}",
                TotalCases = await cases.CountAsync(),
                ActiveCases = await cases.CountAsync(c => c.Status != "Closed"),
                FollowedLawyers = await _db.LawyerFollows.CountAsync(f => f.FollowerId == user.Id),
                AssignedLawyers = await _db.CaseLawyers
                    .Where(x => x.Case != null && x.Case.CreatedBy_Id == user.Id && (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"))
                    .Select(x => x.LawyerId)
                    .Distinct()
                    .CountAsync(),
                PendingRequests = await _db.LegalServiceRequests.CountAsync(x => x.ClientId == user.Id && x.Status == "Pending"),
                PublicRequests = await _db.LegalServiceRequests.CountAsync(x => x.ClientId == user.Id && x.RequestType == "Public"),
                NextHearingDate = next?.HearingDate,
                NextHearingEndDate = next?.EndDate,
                HearingCountdownPhase = HearingPhase(next?.HearingDate, next?.EndDate),
                NextHearingLocation = next?.Location,
                NextHearingCaseName = next?.Name,
                RecentCases = await cases
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .Select(c => new ClientDashboardCaseViewModel {
                        CaseId = c.CaseID,
                        CaseName = c.CaseName,
                        CaseType = c.CaseType,
                        Status = c.Status,
                        CreatedAt = c.CreatedAt
                    })
                    .ToListAsync()
            };

            return View(model);
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

        private static string HearingPhase(DateTime? start, DateTime? end) {
            if (!start.HasValue || !end.HasValue) return string.Empty;
            var now = DateTime.Now;
            if (now < start.Value) return "BeforeStart";
            if (now <= end.Value) return "InSession";
            return "Ended";
        }

    }
}