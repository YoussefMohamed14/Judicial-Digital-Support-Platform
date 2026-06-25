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
    }
}
