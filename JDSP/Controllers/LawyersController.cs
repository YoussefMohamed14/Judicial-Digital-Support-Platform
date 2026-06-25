using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JDSP.Models;
using JDSP.ViewModel;

namespace JDSP.Controllers
{
    [Authorize]
    public class LawyersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public LawyersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int caseId)
        {
            var lawyers = await _userManager.GetUsersInRoleAsync("Lawyer");

            var vm = lawyers.Select(l => new LawyerListViewModel
            {
                LawyerId = l.Id,
                FullName = $"{l.FirstName} {l.MiddleName} {l.LastName}",
                Email = l.Email ?? string.Empty,
                CaseId = caseId
            }).ToList();
            
            ViewBag.CaseId = caseId;
            return View(vm);
        }
    }
}
