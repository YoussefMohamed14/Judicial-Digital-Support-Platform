using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JDSP.Controllers {
    [Authorize]
    public class LawyerRequestController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LawyerRequestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = Roles.Client), HttpGet]
        public async Task<IActionResult> Confirm(int caseId, string lawyerId) {
            var clientId = _userManager.GetUserId(User);
            if (clientId == null) return Challenge();

            var item = await _context.Cases.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CaseID == caseId && x.CreatedBy_Id == clientId);
            var lawyer = await _userManager.FindByIdAsync(lawyerId);

            if (item == null || lawyer == null || !await _userManager.IsInRoleAsync(lawyer, Roles.Lawyer))
                return NotFound();

            return View(new SendRequestViewModel {
                CaseId = caseId,
                CaseName = item.CaseName,
                LawyerId = lawyerId,
                LawyerName = $"{lawyer.FirstName} {lawyer.MiddleName} {lawyer.LastName}"
            });
        }

        [Authorize(Roles = Roles.Client), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(SendRequestViewModel vm) {
            var clientId = _userManager.GetUserId(User);
            if (clientId == null) return Challenge();

            var ownsCase = await _context.Cases.AnyAsync(x => x.CaseID == vm.CaseId && x.CreatedBy_Id == clientId);
            var lawyer = await _userManager.FindByIdAsync(vm.LawyerId);
            if (!ownsCase || lawyer == null || !await _userManager.IsInRoleAsync(lawyer, Roles.Lawyer))
                return NotFound();

            var alreadyExists = await _context.CaseLawyers.AnyAsync(cl =>
                cl.CaseId == vm.CaseId &&
                cl.LawyerId == vm.LawyerId &&
                cl.Status != "Rejected");

            if (alreadyExists) {
                TempData["ErrorMessage"] = "You have already sent a request to this lawyer for this case.";
                return RedirectToAction("Index", "Lawyers");
            }

            _context.CaseLawyers.Add(new CaseLawyer {
                CaseId = vm.CaseId,
                LawyerId = vm.LawyerId,
                Status = "Pending",
                AssignedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Request sent successfully.";
            return RedirectToAction("MyCases", "Cases");
        }

        [Authorize(Roles = Roles.Lawyer), HttpGet]
        public async Task<IActionResult> IncomingRequests() {
            var lawyerId = _userManager.GetUserId(User);
            if (lawyerId == null) return Challenge();

            var requests = await _context.CaseLawyers.AsNoTracking()
                .Where(cl => cl.LawyerId == lawyerId && cl.Status == "Pending")
                .Select(cl => new RespondToRequestViewModel {
                    CaseLawyerID = cl.CaseLawyerId,
                    CaseName = cl.Case!.CaseName,
                    ClientName = cl.Case.Creator!.FirstName + " " + cl.Case.Creator.MiddleName + " " + cl.Case.Creator.LastName
                })
                .ToListAsync();

            return View(requests);
        }

        [Authorize(Roles = Roles.Lawyer), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(RespondToRequestViewModel requests) {
            var lawyerId = _userManager.GetUserId(User);
            if (lawyerId == null) return Challenge();
            if (requests.Decision != "Accepted" && requests.Decision != "Rejected") return BadRequest();

            var request = await _context.CaseLawyers
                .FirstOrDefaultAsync(x => x.CaseLawyerId == requests.CaseLawyerID && x.LawyerId == lawyerId);
            if (request == null) return NotFound();

            if (requests.Decision == "Accepted" && await HasHearingConflictAsync(lawyerId, request.CaseId)) {
                TempData["ErrorMessage"] = "This case has a hearing that conflicts with another active client's case.";
                return RedirectToAction(nameof(IncomingRequests));
            }

            request.Status = requests.Decision;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Response {requests.Decision} sent successfully.";
            return RedirectToAction(nameof(IncomingRequests));
        }

        [Authorize(Roles = Roles.Client), HttpGet]
        public async Task<IActionResult> MyRequests() {
            var clientId = _userManager.GetUserId(User);
            if (clientId == null) return Challenge();

            var requests = await _context.CaseLawyers.AsNoTracking()
                .Where(cl => cl.Case!.CreatedBy_Id == clientId)
                .Select(cl => new MyRequestStatusViewModel {
                    CaseLawyerId = cl.CaseLawyerId,
                    CaseName = cl.Case!.CaseName,
                    LawyerName = cl.Lawyer!.FirstName + " " + cl.Lawyer.MiddleName + " " + cl.Lawyer.LastName,
                    Status = cl.Status,
                    ProposedPrice = cl.ProposedPrice
                })
                .ToListAsync();

            return View(requests);
        }

        [Authorize(Roles = Roles.Lawyer), HttpGet]
        public async Task<IActionResult> ProposePrice(int caseLawyerId) {
            var lawyerId = _userManager.GetUserId(User);
            if (lawyerId == null) return Challenge();

            var request = await _context.CaseLawyers.AsNoTracking()
                .Where(cl => cl.CaseLawyerId == caseLawyerId && cl.LawyerId == lawyerId && cl.Status == "Accepted")
                .Select(cl => new ProposePriceViewModel {
                    CaseLawyerId = cl.CaseLawyerId,
                    CaseName = cl.Case!.CaseName,
                    ClientName = cl.Case.Creator!.FirstName + " " + cl.Case.Creator.MiddleName + " " + cl.Case.Creator.LastName
                })
                .FirstOrDefaultAsync();

            return request == null ? NotFound() : View(request);
        }

        [Authorize(Roles = Roles.Lawyer), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ProposePrice(ProposePriceViewModel vm) {
            var lawyerId = _userManager.GetUserId(User);
            if (lawyerId == null) return Challenge();

            var request = await _context.CaseLawyers
                .Include(x => x.Case).ThenInclude(x => x!.Creator)
                .FirstOrDefaultAsync(x => x.CaseLawyerId == vm.CaseLawyerId && x.LawyerId == lawyerId && x.Status == "Accepted");
            if (request == null) return NotFound();

            if (!ModelState.IsValid) {
                vm.CaseName = request.Case?.CaseName ?? string.Empty;
                vm.ClientName = request.Case?.Creator == null
                    ? string.Empty
                    : $"{request.Case.Creator.FirstName} {request.Case.Creator.MiddleName} {request.Case.Creator.LastName}";
                return View(vm);
            }

            request.ProposedPrice = vm.ProposedPrice;
            request.Status = "Price Proposed";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Price proposed, waiting for the client to accept.";
            return RedirectToAction(nameof(IncomingRequests));
        }

        [Authorize(Roles = Roles.Client), HttpGet]
        public async Task<IActionResult> OfferDetails(int caseLawyerId) {
            var clientId = _userManager.GetUserId(User);
            if (clientId == null) return Challenge();

            var offer = await _context.CaseLawyers.AsNoTracking()
                .Where(cl => cl.CaseLawyerId == caseLawyerId && cl.Case!.CreatedBy_Id == clientId && cl.Status == "Price Proposed" && cl.ProposedPrice != null)
                .Select(cl => new AcceptOfferViewModel {
                    CaseLawyerId = cl.CaseLawyerId,
                    CaseName = cl.Case!.CaseName,
                    LawyerName = cl.Lawyer!.FirstName + " " + cl.Lawyer.MiddleName + " " + cl.Lawyer.LastName,
                    ProposedPrice = cl.ProposedPrice!.Value
                })
                .FirstOrDefaultAsync();

            return offer == null ? NotFound() : View(offer);
        }

        [Authorize(Roles = Roles.Client), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptOffer(AcceptOfferViewModel vm) {
            var clientId = _userManager.GetUserId(User);
            if (clientId == null) return Challenge();

            var request = await _context.CaseLawyers
                .Include(x => x.Case)
                .FirstOrDefaultAsync(x => x.CaseLawyerId == vm.CaseLawyerId && x.Case!.CreatedBy_Id == clientId && x.Status == "Price Proposed" && x.ProposedPrice != null);
            if (request == null) return NotFound();

            request.Status = "OfferAccepted";
            var subscription = new CaseLawyerSubscription {
                CaseLawyerId = request.CaseLawyerId,
                Price = request.ProposedPrice!.Value,
                Status = "Active",
                BillingCycle = "Monthly",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(1)
            };

            _context.CaseLawyerSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Offer accepted! Your subscription is now active.";
            return RedirectToAction("Confirmation", "Subscriptions", new { subscriptionId = subscription.CaseLawyerSubscriptionId });
        }

        private async Task<bool> HasHearingConflictAsync(string lawyerId, int requestedCaseId) {
            var requestedHearings = await _context.Hearings.AsNoTracking()
                .Where(h => h.CaseId == requestedCaseId && h.Status == "Scheduled" && h.EndDate >= DateTime.Now)
                .Select(h => new { h.HearingDate, h.EndDate })
                .ToListAsync();

            if (requestedHearings.Count == 0) return false;

            var activeCaseIds = _context.CaseLawyers.AsNoTracking()
                .Where(x => x.LawyerId == lawyerId && x.CaseId != requestedCaseId &&
                    (x.Status == "Accepted" || x.Status == "Price Proposed" || x.Status == "OfferAccepted"))
                .Select(x => x.CaseId);

            var otherHearings = await _context.Hearings.AsNoTracking()
                .Where(h => activeCaseIds.Contains(h.CaseId) && h.Status == "Scheduled" && h.EndDate >= DateTime.Now)
                .Select(h => new { h.HearingDate, h.EndDate })
                .ToListAsync();

            return requestedHearings.Any(a => otherHearings.Any(b => a.HearingDate < b.EndDate && b.HearingDate < a.EndDate));
        }
    }
}
