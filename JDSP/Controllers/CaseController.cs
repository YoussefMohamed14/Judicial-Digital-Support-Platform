using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using JDSP.Data;
using JDSP.Models;
using JDSP.ViewModel;

namespace JDSP.Controllers
{
    [Authorize]
    public class CaseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CaseController(ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCaseViewModel model)
        {
            if (ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return View(model);
            }

            var newCase = new Case
            {
                CaseName = model.CaseName,
                CaseType = model.CaseType,
                Description = model.Description,
                CreatedBy_Id = user.Id,
                CreatedAt = DateTime.Now,
                Status = "Open"
            };
            
            _context.Add(newCase);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Case created successfully!";
            return RedirectToAction("Index", "Lawyers", new { caseId = newCase.CaseID });
        }

        public async Task<IActionResult> MyCases()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var myCases = _context.Cases
                .Where(c => c.CreatedBy_Id == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();
            return View(myCases);
        }
    }
}

