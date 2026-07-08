using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JDSP.Controllers {
    [Authorize(Roles = Roles.Client + "," + Roles.Lawyer)]
    public class ProfileController : Controller {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _db;

        public ProfileController(
            UserManager<ApplicationUser> users,
            SignInManager<ApplicationUser> signIn,
            IWebHostEnvironment env,
            ApplicationDbContext db) {
            _users = users;
            _signIn = signIn;
            _env = env;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index() {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();
            return View(await MapAsync(user));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ClientProfileViewModel model) {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();

            foreach (var key in new[] {
                nameof(model.FullName), nameof(model.Email), nameof(model.PhoneNumber),
                nameof(model.NationalNumber), nameof(model.Role)
            }) ModelState.Remove(key);

            if (!ModelState.IsValid) {
                var current = await MapAsync(user);
                current.Bio = model.Bio;
                return View("Index", current);
            }

            if (model.NewPhoto != null) {
                var upload = await ProfileImageHelper.SaveAsync(model.NewPhoto, _env, user.PhotoPath);
                if (!upload.Success) {
                    ModelState.AddModelError(nameof(model.NewPhoto), upload.Error ?? "Upload failed.");
                    var current = await MapAsync(user);
                    current.Bio = model.Bio;
                    return View("Index", current);
                }
                user.PhotoPath = upload.Path;
            }

            user.Bio = model.Bio.Trim();
            user.IsProfileCompleted = true;

            if (await _users.IsInRoleAsync(user, Roles.Lawyer)) {
                var lawyerProfile = await _db.LawyerProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (lawyerProfile != null) lawyerProfile.Bio = user.Bio;
            }

            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded) {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View("Index", await MapAsync(user));
            }

            await _db.SaveChangesAsync();
            await _signIn.RefreshSignInAsync(user);
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<ClientProfileViewModel> MapAsync(ApplicationUser user) {
            var roles = await _users.GetRolesAsync(user);
            var middle = string.IsNullOrWhiteSpace(user.MiddleName) ? "" : $" {user.MiddleName}";
            return new ClientProfileViewModel {
                FullName = $"{user.FirstName}{middle} {user.LastName}",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                NationalNumber = user.NationalNumber,
                Role = roles.FirstOrDefault() ?? "User",
                CreatedAt = user.CreatedAt,
                PhotoPath = user.PhotoPath,
                Bio = user.Bio ?? ""
            };
        }
    }
}
