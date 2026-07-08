using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace JDSP.Controllers {
    [Authorize(Roles = Roles.Client + "," + Roles.Lawyer)]
    public class SettingsController : Controller {
        private readonly UserManager<ApplicationUser> _users; private readonly SignInManager<ApplicationUser> _signIn;
        public SettingsController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) { _users = users; _signIn = signIn; }
        [HttpGet]
        public async Task<IActionResult> Index() {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();
            return View(new SettingsViewModel { CurrentEmail = user.Email ?? "", PreferredLanguage = user.PreferredLanguage });
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(SettingsViewModel model) {
            var user = await _users.GetUserAsync(User); if (user == null) return Challenge();
            model.CurrentEmail = user.Email ?? ""; model.PreferredLanguage = user.PreferredLanguage;
            ModelState.Remove(nameof(model.CurrentEmail));
            ModelState.Remove(nameof(model.PreferredLanguage));
            if (string.IsNullOrWhiteSpace(model.NewEmail)) ModelState.AddModelError(nameof(model.NewEmail), "Enter a new email address.");
            if (string.IsNullOrWhiteSpace(model.CurrentPassword)) ModelState.AddModelError(nameof(model.CurrentPassword), "Enter your current password.");
            if (!ModelState.IsValid) return View("Index", model);
            if (!await _users.CheckPasswordAsync(user, model.CurrentPassword!)) { ModelState.AddModelError(nameof(model.CurrentPassword), "The current password is incorrect."); return View("Index", model); }
            var existing = await _users.FindByEmailAsync(model.NewEmail!.Trim()); if (existing != null && existing.Id != user.Id) { ModelState.AddModelError(nameof(model.NewEmail), "This email is already in use."); return View("Index", model); }
            user.Email = model.NewEmail.Trim(); user.UserName = model.NewEmail.Trim(); var result = await _users.UpdateAsync(user);
            if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View("Index", model); }
            await _signIn.RefreshSignInAsync(user); TempData["Success"] = "Email updated successfully."; return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeLanguage(string language, string? returnUrl = null) {
            language = language == "ar" ? "ar" : "en"; var user = await _users.GetUserAsync(User); if (user != null) { user.PreferredLanguage = language; await _users.UpdateAsync(user); }
            Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language)), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
        }
    }
}
