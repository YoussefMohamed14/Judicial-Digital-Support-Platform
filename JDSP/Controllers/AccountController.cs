using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace JDSP.Controllers {
    public class AccountController : Controller {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly IWebHostEnvironment _env;
        public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IWebHostEnvironment env) { _users = users; _signIn = signIn; _env = env; }

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> Register() {
            if (User.Identity?.IsAuthenticated == true) {
                var user = await _users.GetUserAsync(User);
                if (user != null) {
                    if (!user.IsProfileCompleted && await NeedsProfileAsync(user))
                        return RedirectToAction(nameof(CompleteProfile));
                    return await DashboardAsync(user);
                }
            }
            LoadRoles(); return View();
        }

        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model) {
            LoadRoles();
            if (!ModelState.IsValid) return View(model);
            if (model.Role != Roles.Client && model.Role != Roles.Lawyer) { ModelState.AddModelError(nameof(model.Role), "You can only register as Client or Lawyer."); return View(model); }
            var national = model.NationalNumber.Trim();
            if (await _users.Users.AnyAsync(x => x.NationalNumber == national)) { ModelState.AddModelError(nameof(model.NationalNumber), "This national number is already registered."); return View(model); }
            var user = new ApplicationUser {
                UserName = model.Email.Trim(), Email = model.Email.Trim(), FirstName = model.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? null : model.MiddleName.Trim(), LastName = model.LastName.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(), NationalNumber = national, AccountStatus = "Active", PreferredLanguage = "en", IsProfileCompleted = false, CreatedAt = DateTime.Now
            };
            var result = await _users.CreateAsync(user, model.Password);
            if (result.Succeeded) { await _users.AddToRoleAsync(user, model.Role); await _signIn.SignInAsync(user, false); return RedirectToAction(nameof(CompleteProfile)); }
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        [Authorize, HttpGet]
        public async Task<IActionResult> CompleteProfile() {
            var user = await _users.GetUserAsync(User); if (user == null) return RedirectToAction(nameof(Login));
            if (user.IsProfileCompleted) return await DashboardAsync(user);
            return View(new CompleteProfileViewModel { Bio = user.Bio ?? string.Empty });
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel model) {
            var user = await _users.GetUserAsync(User); if (user == null) return RedirectToAction(nameof(Login));
            if (!ModelState.IsValid) return View(model);
            var upload = await ProfileImageHelper.SaveAsync(model.Photo, _env, user.PhotoPath);
            if (!upload.Success) { ModelState.AddModelError(nameof(model.Photo), upload.Error ?? "Upload failed."); return View(model); }
            user.PhotoPath = upload.Path; user.Bio = model.Bio.Trim(); user.IsProfileCompleted = true;
            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model); }
            await _signIn.RefreshSignInAsync(user); TempData["Success"] = "Your profile is ready."; return await DashboardAsync(user);
        }

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> Login() {
            if (User.Identity?.IsAuthenticated == true) {
                var user = await _users.GetUserAsync(User);
                if (user != null) {
                    if (!user.IsProfileCompleted && await NeedsProfileAsync(user))
                        return RedirectToAction(nameof(CompleteProfile));
                    return await DashboardAsync(user);
                }
            }
            return View();
        }

        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model) {
            if (!ModelState.IsValid) return View(model);
            var user = await _users.FindByEmailAsync(model.Email.Trim());
            if (user == null || user.AccountStatus != "Active") { ModelState.AddModelError(string.Empty, "Invalid email or password."); return View(model); }
            var result = await _signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
            if (!result.Succeeded) { ModelState.AddModelError(string.Empty, "Invalid email or password."); return View(model); }
            if (!user.IsProfileCompleted && await NeedsProfileAsync(user)) return RedirectToAction(nameof(CompleteProfile));
            return await DashboardAsync(user);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout() { await _signIn.SignOutAsync(); return RedirectToAction("Index", "Home"); }
        [AllowAnonymous] public IActionResult AccessDenied() => View();
        [AllowAnonymous] public IActionResult Error() => View();
        private void LoadRoles() => ViewBag.Roles = new SelectList(new[] { Roles.Client, Roles.Lawyer });
        private async Task<bool> NeedsProfileAsync(ApplicationUser user) => await _users.IsInRoleAsync(user, Roles.Client) || await _users.IsInRoleAsync(user, Roles.Lawyer);
        private async Task<IActionResult> DashboardAsync(ApplicationUser user) {
            if (await _users.IsInRoleAsync(user, Roles.Admin)) return RedirectToAction("AdminDashboard", "Dashboard");
            if (await _users.IsInRoleAsync(user, Roles.CourtEmployee)) return RedirectToAction("CourtEmployeeDashboard", "Dashboard");
            if (await _users.IsInRoleAsync(user, Roles.Lawyer)) return RedirectToAction("LawyerDashboard", "Dashboard");
            if (await _users.IsInRoleAsync(user, Roles.Client)) return RedirectToAction("ClientDashboard", "Dashboard");
            await _signIn.SignOutAsync(); return RedirectToAction(nameof(Login));
        }
    }
}
