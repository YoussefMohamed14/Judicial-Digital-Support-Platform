using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using JDSP.Models;
using JDSP.Data;
using JDSP.ViewModel;

namespace JDSP.Controllers
{
    [Authorize]
    public class LawyerRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LawyerRequestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        
        public async Task<IActionResult> Confirm(int caseId, string lawyerId)
        {
            var Case = await _context.Cases.FindAsync(caseId);
            var lawyer = await _userManager.FindByIdAsync(lawyerId);

            if (Case == null || lawyer == null)
            {
                return NotFound();
            }

            var vm = new SendRequestViewModel
            {
                CaseId = caseId,
                CaseName = Case.CaseName,
                LawyerId = lawyerId,
                LawyerName = $"{lawyer.FirstName} {lawyer.MiddleName} {lawyer.LastName}"
            };

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(SendRequestViewModel vm)
        {
            bool alreadyExists = _context.CaseLawyers.Any(cl =>
                        cl.CaseLawyerId == vm.CaseId &&
                        cl.LawyerId == vm.LawyerId &&
                        cl.Status != "Rejected");
            if (alreadyExists)
            {
                TempData["ErrorMessage"] = "You have already sent a request to this lawyer for this case.";
                return RedirectToAction("Index", "Lawyers", new { caseId = vm.CaseId, lawyerId = vm.LawyerId });
            }

            var request = new CaseLawyer
            {
                CaseId = vm.CaseId,
                LawyerId = vm.LawyerId,
                Status = "Pending",
                AssignedAt = DateTime.Now
            };

            _context.CaseLawyers.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Request sent successfully.";
            return RedirectToAction("MyCases", "Cases");
        }

        public async Task<IActionResult> IncomingRequests()
        {
            var lawyer = await _userManager.GetUserAsync(User);

            var requests = _context.CaseLawyers
                .Where(cl => cl.LawyerId == lawyer!.Id && cl.Status == "Pending")
                .Select(cl => new RespondToRequestViewModel
                {
                    CaseLawyerID = cl.CaseLawyerId,
                    CaseName = cl.Case!.CaseName,
                    ClientName = cl.Case.Creator!.FirstName + " " + cl.Case.Creator.MiddleName + " " + cl.Case.Creator.LastName
                }).ToList();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(RespondToRequestViewModel requests)
        {
            var request = await _context.CaseLawyers.FindAsync(requests.CaseLawyerID);
            if (request == null)
            {
                return NotFound();
            }

            if (requests.Decision != "Accepted" && requests.Decision != "Rejected")
            {
                return BadRequest();
            }
            request.Status = requests.Decision;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Response {requests.Decision} sent successfully.";
            return RedirectToAction("IncomingRequests");
        }

        public async Task<IActionResult> MyRequests()
        {
            var user = await _userManager.GetUserAsync(User);

            var requests = _context.CaseLawyers
                .Where(cl => cl.Case!.CreatedBy_Id == user!.Id)
                .Select(cl => new MyRequestStatusViewModel
                {
                    CaseLawyerId = cl.CaseLawyerId,
                    CaseName = cl.Case!.CaseName,
                    LawyerName = cl.Lawyer!.FirstName + " " + cl.Lawyer.MiddleName + " " + cl.Lawyer.LastName,
                    Status = cl.Status,
                    ProposedPrice = cl.ProposedPrice
                }).ToList();
            return View(requests);
        }

        public async Task<IActionResult> ProposePrice(int caseLawyerId)
        {
            var request = _context.CaseLawyers
                .Where(cl => cl.CaseLawyerId == caseLawyerId && cl.Status == "Accepted")
                .Select(cl => new ProposePriceViewModel
                {
                    CaseLawyerId = cl.CaseLawyerId,
                    CaseName = cl.Case!.CaseName,
                    ClientName = cl.Case.Creator!.FirstName + " " + cl.Case.Creator.MiddleName + " " + cl.Case.Creator.LastName
                }).FirstOrDefault();
            if (request == null)
            {
                return NotFound();
            }
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProposePrice(ProposePriceViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }
            var request = await _context.CaseLawyers.FindAsync(vm.CaseLawyerId);
            
            if (request == null)
            {
                return NotFound();
            }
            
            request.ProposedPrice = vm.ProposedPrice;
            request.Status = "Price Proposed";

            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Price proposed, waiting for the client to accept.";
            return RedirectToAction("IncomingRequests");
        }

        public IActionResult OfferDetails(int caseLawyerId)
        {
            var offer = _context.CaseLawyers
                .Where(cl => cl.CaseLawyerId == caseLawyerId && cl.Status == "Price Proposed")
                .Select(cl => new AcceptOfferViewModel
                {
                    CaseLawyerId = cl.CaseLawyerId,
                    CaseName = cl.Case!.CaseName,
                    LawyerName = cl.Lawyer!.FirstName + " " + cl.Lawyer.MiddleName + " " + cl.Lawyer.LastName,
                    ProposedPrice = cl.ProposedPrice!.Value
                }).FirstOrDefault();
            if (offer == null)
            {
                return NotFound();
            }
            return View(offer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptOffer(AcceptOfferViewModel vm)
        {
            var request = await _context.CaseLawyers.FindAsync(vm.CaseLawyerId);

            if (request == null)
            {
                return NotFound();
            }

            request.Status = "OfferAccepted";

            var subscription = new CaseLawyerSubscription
            {
                CaseLawyerId = vm.CaseLawyerId,
                Price = vm.ProposedPrice,
                Status = "Active",
                BillingCycle = "Monthly",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(1)
            };

            _context.CaseLawyerSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Offer accepted! Your subscription is now active.";
            return RedirectToAction("Confirmation", "Subscriptions",
                                    new { subscriptionId = subscription.CaseLawyerSubscriptionId });
        }
    }
}
