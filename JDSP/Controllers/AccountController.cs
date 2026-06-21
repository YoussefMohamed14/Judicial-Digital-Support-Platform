using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JDSP.Controllers {
    public class AccountController : Controller {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager) {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Register() {
            LoadRoles();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model) {
            LoadRoles();

            if (!ModelState.IsValid)
                return View(model);

            if (model.Role != Roles.Client && model.Role != Roles.Lawyer) {
                ModelState.AddModelError("Role", "You can only register as Client or Lawyer.");
                return View(model);
            }

            var user = new ApplicationUser {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                NationalNumber = model.NationalNumber,
                AccountStatus = "Active",
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded) {
                await _userManager.AddToRoleAsync(user, model.Role);

                TempData["Success"] = "Account created successfully. Please login.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors) {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login() {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model) {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null) {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (user.AccountStatus != "Active") {
                ModelState.AddModelError("", "Your account is not active.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded) {
                return await RedirectToDashboard(user);
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout() {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied() {
            return View();
        }

        private void LoadRoles() {
            ViewBag.Roles = new SelectList(new[]
            {
                Roles.Client,
                Roles.Lawyer
            });
        }

        private async Task<IActionResult> RedirectToDashboard(ApplicationUser user) {
            if (await _userManager.IsInRoleAsync(user, Roles.Admin))
                return RedirectToAction("AdminDashboard", "Dashboard");

            if (await _userManager.IsInRoleAsync(user, Roles.CourtEmployee))
                return RedirectToAction("CourtEmployeeDashboard", "Dashboard");

            if (await _userManager.IsInRoleAsync(user, Roles.Lawyer))
                return RedirectToAction("LawyerDashboard", "Dashboard");

            if (await _userManager.IsInRoleAsync(user, Roles.Client))
                return RedirectToAction("ClientDashboard", "Dashboard");

            return RedirectToAction("Index", "Home");
        }
    }
}