using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace JDSP.Controllers {
    [AllowAnonymous]
    public class HomeController : Controller {
        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeLanguage(string language, string? returnUrl = null) {
            language = language == "ar" ? "ar" : "en";

            Response.Cookies.Delete(CookieRequestCultureProvider.DefaultCookieName, new CookieOptions { Path = "/Settings" });
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language)),
                new CookieOptions {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                });

            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(Index));
        }
    }
}
