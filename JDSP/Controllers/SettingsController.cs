using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace JDSP.Controllers {
    [Authorize]
    public class SettingsController : Controller {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;

        public SettingsController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) {
            _users = users;
            _signIn = signIn;
        }

        [HttpGet]
        public async Task<IActionResult> Index() {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();

            return View(new SettingsViewModel {
                CurrentEmail = user.Email ?? string.Empty,
                PreferredLanguage = user.PreferredLanguage
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchAccount(SettingsViewModel model) {
            var currentUser = await _users.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            model.CurrentEmail = currentUser.Email ?? string.Empty;
            model.PreferredLanguage = currentUser.PreferredLanguage;
            ModelState.Remove(nameof(model.CurrentEmail));
            ModelState.Remove(nameof(model.PreferredLanguage));

            if (string.IsNullOrWhiteSpace(model.SwitchEmail)) {
                ModelState.AddModelError(nameof(model.SwitchEmail), Text("Enter the email of the account you want to open.", "أدخل بريد الحساب الذي تريد فتحه."));
            }

            if (string.IsNullOrWhiteSpace(model.SwitchPassword)) {
                ModelState.AddModelError(nameof(model.SwitchPassword), Text("Enter the password for that account.", "أدخل كلمة مرور هذا الحساب."));
            }

            if (!ModelState.IsValid) return View("Index", model);

            var targetEmail = model.SwitchEmail!.Trim();
            var targetUser = await _users.FindByEmailAsync(targetEmail);

            if (targetUser == null || targetUser.AccountStatus != "Active") {
                ModelState.AddModelError(string.Empty, Text("Invalid email or password.", "البريد الإلكتروني أو كلمة المرور غير صحيحة."));
                return View("Index", model);
            }

            if (targetUser.Id == currentUser.Id) {
                ModelState.AddModelError(nameof(model.SwitchEmail), Text("You are already using this account.", "أنت تستخدم هذا الحساب بالفعل."));
                return View("Index", model);
            }

            var isClient = await _users.IsInRoleAsync(targetUser, Roles.Client);
            var isLawyer = await _users.IsInRoleAsync(targetUser, Roles.Lawyer);
            if (!isClient && !isLawyer) {
                ModelState.AddModelError(string.Empty, Text("Only client and lawyer accounts can be opened from this page.", "يمكن فتح حسابات العميل والمحامي فقط من هذه الصفحة."));
                return View("Index", model);
            }

            if (!await _users.CheckPasswordAsync(targetUser, model.SwitchPassword!)) {
                ModelState.AddModelError(string.Empty, Text("Invalid email or password.", "البريد الإلكتروني أو كلمة المرور غير صحيحة."));
                return View("Index", model);
            }

            await _signIn.SignOutAsync();
            await _signIn.SignInAsync(targetUser, isPersistent: false);
            SetLanguageCookie(targetUser.PreferredLanguage);

            TempData["Success"] = Text("Account switched successfully.", "تم تبديل الحساب بنجاح.");
            return await DashboardAsync(targetUser);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeLanguage(string language, string? returnUrl = null) {
            language = language == "ar" ? "ar" : "en";
            var user = await _users.GetUserAsync(User);
            if (user != null) {
                user.PreferredLanguage = language;
                await _users.UpdateAsync(user);
            }

            Response.Cookies.Delete(CookieRequestCultureProvider.DefaultCookieName, new CookieOptions { Path = "/Settings" });
            SetLanguageCookie(language);

            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(Index));
        }

        private void SetLanguageCookie(string language) {
            language = language == "ar" ? "ar" : "en";
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language)),
                new CookieOptions {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                });
        }

        private string Text(string en, string ar) {
            var language = HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture.TwoLetterISOLanguageName;
            return language == "ar" ? ar : en;
        }

        private async Task<IActionResult> DashboardAsync(ApplicationUser user) {
            if (!user.IsProfileCompleted) return RedirectToAction("CompleteProfile", "Account");
            if (await _users.IsInRoleAsync(user, Roles.Lawyer)) return RedirectToAction("LawyerDashboard", "Dashboard");
            if (await _users.IsInRoleAsync(user, Roles.Client)) return RedirectToAction("ClientDashboard", "Dashboard");
            await _signIn.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
    }
}
