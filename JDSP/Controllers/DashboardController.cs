using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Dashboard;
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
        public IActionResult AdminDashboard() => View();

        [Authorize(Roles = Roles.CourtEmployee)]
        public IActionResult CourtEmployeeDashboard() => View();

        [Authorize(Roles = Roles.Lawyer)]
        public async Task<IActionResult> LawyerDashboard() {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();

            var activeCaseAssignments = _db.CaseLawyers.AsNoTracking()
                .Where(x => x.LawyerId == user.Id &&
                    (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"));

            var assignedCaseIds = activeCaseAssignments.Select(x => x.CaseId);
            var nextHearing = await _db.Hearings.AsNoTracking()
                .Where(h => assignedCaseIds.Contains(h.CaseId) && h.HearingDate >= DateTime.Now && h.Status == "Scheduled")
                .OrderBy(h => h.HearingDate)
                .Select(h => new {
                    h.HearingDate,
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
                NextHearingDate = nextHearing?.HearingDate,
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

            var cases = _db.Cases.AsNoTracking().Where(c => c.CreatedBy_Id == user.Id);
            var next = await _db.Hearings.AsNoTracking()
                .Where(h => h.Case != null && h.Case.CreatedBy_Id == user.Id && h.HearingDate >= DateTime.Now && h.Status == "Scheduled")
                .OrderBy(h => h.HearingDate)
                .Select(h => new { h.HearingDate, h.Location, Name = h.Case!.CaseName })
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
    }
}
