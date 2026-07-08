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
            TempData["SuccessMessage"] = "The case was created and assigned to the client.";
            return RedirectToAction("CourtEmployeeDashboard", "Dashboard");
        }

        [Authorize(Roles = Roles.Client), HttpGet]
        public async Task<IActionResult> MyCases(string? status) {
            var userId = _users.GetUserId(User);
            if (userId == null) return Challenge();

            var query = _db.Cases.AsNoTracking().Where(c => c.CreatedBy_Id == userId);
            var allowed = new[] { "Open", "Pending", "In Progress", "Closed" };
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
                        .Where(h => h.CaseId == c.CaseID && h.HearingDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => (DateTime?)h.HearingDate)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return View(new MyCasesViewModel { StatusFilter = status, Cases = items });
        }

        [Authorize(Roles = Roles.Client), HttpGet]
        public async Task<IActionResult> Details(int id) {
            var userId = _users.GetUserId(User);
            if (userId == null) return Challenge();

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
                .Where(h => h.CaseId == id && h.HearingDate >= DateTime.Now && h.Status == "Scheduled")
                .OrderBy(h => h.HearingDate)
                .Select(h => new { h.HearingDate, h.Location, h.HearingType })
                .FirstOrDefaultAsync();

            item.AssignedLawyerName = request?.Name;
            item.LawyerRequestStatus = request?.Status;
            item.NextHearingDate = hearing?.HearingDate;
            item.NextHearingLocation = hearing?.Location;
            item.NextHearingType = hearing?.HearingType;
            return View(item);
        }

        [Authorize(Roles = Roles.Lawyer), HttpGet]
        public async Task<IActionResult> AssignedCases(string? status) {
            var lawyerId = _users.GetUserId(User);
            if (lawyerId == null) return Challenge();

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
                        .Where(h => h.CaseId == x.CaseId && h.HearingDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => (DateTime?)h.HearingDate)
                        .FirstOrDefault(),
                    NextHearingLocation = _db.Hearings
                        .Where(h => h.CaseId == x.CaseId && h.HearingDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => h.Location)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return View(new LawyerAssignedCasesViewModel { StatusFilter = status, Cases = items });
        }

        [Authorize(Roles = Roles.Lawyer), HttpGet]
        public async Task<IActionResult> LawyerDetails(int id) {
            var lawyerId = _users.GetUserId(User);
            if (lawyerId == null) return Challenge();

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
                        .Where(h => h.CaseId == x.CaseId && h.HearingDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => (DateTime?)h.HearingDate)
                        .FirstOrDefault(),
                    NextHearingLocation = _db.Hearings
                        .Where(h => h.CaseId == x.CaseId && h.HearingDate >= DateTime.Now && h.Status == "Scheduled")
                        .OrderBy(h => h.HearingDate)
                        .Select(h => h.Location)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (item == null) return NotFound();
            return View(item);
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
    }
}
