using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace JDSP.Controllers {
    public class AccountController : Controller {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _db;
        public AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IWebHostEnvironment env, ApplicationDbContext db) { _users = users; _signIn = signIn; _env = env; _db = db; }

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> Register() {
            if (User.Identity?.IsAuthenticated == true) {
                var user = await _users.GetUserAsync(User);
                if (user != null) {
                    if (user.MustChangePassword)
                        return RedirectToAction(nameof(ChangeTemporaryPassword));
                    if (await IsLawyerAwaitingApprovalAsync(user))
                        return RedirectToAction(nameof(LawyerPendingApproval));
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
            if (model.Role != Roles.Client && model.Role != Roles.Lawyer)
                ModelState.AddModelError(nameof(model.Role), "You can only register as Client or Lawyer.");

            if (model.Role == Roles.Lawyer) {
                if (model.NationalIdFile == null || model.NationalIdFile.Length == 0)
                    ModelState.AddModelError(nameof(model.NationalIdFile), "Upload your national/legal ID.");
                if (model.LawyerIdFile == null || model.LawyerIdFile.Length == 0)
                    ModelState.AddModelError(nameof(model.LawyerIdFile), "Upload your lawyer ID or proof.");
            }

            if (!ModelState.IsValid) return View(model);

            var national = model.NationalNumber.Trim();
            if (await _users.Users.AnyAsync(x => x.NationalNumber == national)) { ModelState.AddModelError(nameof(model.NationalNumber), "This national number is already registered."); return View(model); }
            var selectedLanguage = ResolveRequestedLanguage();
            var user = new ApplicationUser {
                UserName = model.Email.Trim(), Email = model.Email.Trim(), FirstName = model.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? null : model.MiddleName.Trim(), LastName = model.LastName.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(), NationalNumber = national, AccountStatus = "Active", PreferredLanguage = selectedLanguage, IsProfileCompleted = false,
                LawyerApprovalStatus = model.Role == Roles.Lawyer ? VerificationStatus.Pending : VerificationStatus.NotRequired,
                CreatedAt = DateTime.Now
            };
            var result = await _users.CreateAsync(user, model.Password);
            if (!result.Succeeded) {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _users.AddToRoleAsync(user, model.Role);

            if (model.Role == Roles.Lawyer) {
                var nationalUpload = await VerificationFileHelper.SaveAsync(model.NationalIdFile, _env);
                if (!nationalUpload.Success) {
                    await _users.DeleteAsync(user);
                    ModelState.AddModelError(nameof(model.NationalIdFile), nationalUpload.Error ?? "Upload failed.");
                    return View(model);
                }

                var lawyerUpload = await VerificationFileHelper.SaveAsync(model.LawyerIdFile, _env);
                if (!lawyerUpload.Success) {
                    await _users.DeleteAsync(user);
                    ModelState.AddModelError(nameof(model.LawyerIdFile), lawyerUpload.Error ?? "Upload failed.");
                    return View(model);
                }

                _db.LawyerVerificationRequests.Add(new LawyerVerificationRequest {
                    LawyerId = user.Id,
                    NationalIdFileName = nationalUpload.OriginalName ?? "National ID",
                    NationalIdFilePath = nationalUpload.StoredName ?? string.Empty,
                    LawyerIdFileName = lawyerUpload.OriginalName ?? "Lawyer ID",
                    LawyerIdFilePath = lawyerUpload.StoredName ?? string.Empty,
                    Status = VerificationStatus.Pending,
                    RequestedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();

                await _signIn.SignInAsync(user, false);
                return RedirectToAction(nameof(LawyerPendingApproval));
            }

            await _signIn.SignInAsync(user, false);
            return RedirectToAction(nameof(CompleteProfile));
        }

        [Authorize, HttpGet]
        public async Task<IActionResult> CompleteProfile() {
            var user = await _users.GetUserAsync(User); if (user == null) return RedirectToAction(nameof(Login));
            if (await IsLawyerAwaitingApprovalAsync(user)) return RedirectToAction(nameof(LawyerPendingApproval));
            if (user.IsProfileCompleted) return await DashboardAsync(user);
            return View(new CompleteProfileViewModel { Bio = user.Bio ?? string.Empty });
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel model) {
            var user = await _users.GetUserAsync(User); if (user == null) return RedirectToAction(nameof(Login));
            if (await IsLawyerAwaitingApprovalAsync(user)) return RedirectToAction(nameof(LawyerPendingApproval));
            if (!ModelState.IsValid) return View(model);
            var upload = await ProfileImageHelper.SaveAsync(model.Photo, _env, user.PhotoPath);
            if (!upload.Success) { ModelState.AddModelError(nameof(model.Photo), upload.Error ?? "Upload failed."); return View(model); }
            user.PhotoPath = upload.Path; user.Bio = model.Bio.Trim(); user.IsProfileCompleted = true;
            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model); }
            await EnsureLawyerProfileExistsAsync(user);
            await _signIn.RefreshSignInAsync(user); TempData["Success"] = "Your profile is ready."; return await DashboardAsync(user);
        }

        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> Login() {
            if (User.Identity?.IsAuthenticated == true) {
                var user = await _users.GetUserAsync(User);
                if (user != null) {
                    if (user.MustChangePassword)
                        return RedirectToAction(nameof(ChangeTemporaryPassword));
                    if (await IsLawyerAwaitingApprovalAsync(user))
                        return RedirectToAction(nameof(LawyerPendingApproval));
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
            if (user == null || user.AccountStatus != "Active") return InvalidLogin(model);
            var result = await _signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
            if (!result.Succeeded) return InvalidLogin(model);
            if (Request.Cookies.ContainsKey(CookieRequestCultureProvider.DefaultCookieName)) {
                var selectedLanguage = ResolveRequestedLanguage();
                if (user.PreferredLanguage != selectedLanguage) { user.PreferredLanguage = selectedLanguage; await _users.UpdateAsync(user); }
            }
            if (user.MustChangePassword) return RedirectToAction(nameof(ChangeTemporaryPassword));
            if (await IsLawyerAwaitingApprovalAsync(user)) return RedirectToAction(nameof(LawyerPendingApproval));
            if (!user.IsProfileCompleted && await NeedsProfileAsync(user)) return RedirectToAction(nameof(CompleteProfile));
            return await DashboardAsync(user);
        }

        [Authorize, HttpGet]
        public async Task<IActionResult> LawyerPendingApproval() {
            var user = await _users.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));
            if (!await _users.IsInRoleAsync(user, Roles.Lawyer)) return await DashboardAsync(user);
            if (user.LawyerApprovalStatus == VerificationStatus.Approved) return await DashboardAsync(user);

            var request = await _db.LawyerVerificationRequests.AsNoTracking()
                .Where(x => x.LawyerId == user.Id)
                .OrderByDescending(x => x.RequestedAt)
                .FirstOrDefaultAsync();

            ViewBag.Status = user.LawyerApprovalStatus;
            ViewBag.RejectionReason = user.LawyerApprovalRejectionReason;
            ViewBag.RequestedAt = request?.RequestedAt;
            return View();
        }

        [Authorize, HttpGet]
        public async Task<IActionResult> ChangeTemporaryPassword() {
            var user = await _users.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));
            if (!user.MustChangePassword) return await DashboardAsync(user);
            return View(new ChangeTemporaryPasswordViewModel());
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeTemporaryPassword(ChangeTemporaryPasswordViewModel model) {
            var user = await _users.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Login));
            if (!ModelState.IsValid) return View(model);

            var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded) {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            user.MustChangePassword = false;
            await _users.UpdateAsync(user);
            await _signIn.RefreshSignInAsync(user);
            TempData["Success"] = "Password changed successfully.";
            return await DashboardAsync(user);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout() { await _signIn.SignOutAsync(); return RedirectToAction("Index", "Home"); }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAccount() {
            await _signIn.SignOutAsync();
            TempData["Success"] = ResolveRequestedLanguage() == "ar" ? "اختر الحساب الذي تريد الدخول به." : "Choose the account you want to open.";
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous] public IActionResult AccessDenied() => View();
        [AllowAnonymous] public IActionResult Error() => RedirectToAction("Index", "Error", new { code = 500 });

        private IActionResult InvalidLogin(LoginViewModel model) {
            var ar = ResolveRequestedLanguage() == "ar";
            var message = ar
                ? "البريد الإلكتروني أو كلمة المرور غير صحيحة. يرجى التحقق والمحاولة مرة أخرى."
                : "Invalid email or password. Please check your details and try again.";

            ModelState.AddModelError(string.Empty, message);
            ViewData["LoginError"] = message;
            return View(model);
        }

        private string ResolveRequestedLanguage() {
            var feature = HttpContext.Features.Get<IRequestCultureFeature>();
            var language = feature?.RequestCulture.UICulture.TwoLetterISOLanguageName;
            return language == "ar" ? "ar" : "en";
        }
        private void LoadRoles() => ViewBag.Roles = new SelectList(new[] { Roles.Client, Roles.Lawyer });
        private async Task<bool> NeedsProfileAsync(ApplicationUser user) => await _users.IsInRoleAsync(user, Roles.Client) || await _users.IsInRoleAsync(user, Roles.Lawyer);
        private async Task<bool> IsLawyerAwaitingApprovalAsync(ApplicationUser user) =>
            await _users.IsInRoleAsync(user, Roles.Lawyer) && user.LawyerApprovalStatus != VerificationStatus.Approved;

        private async Task EnsureLawyerProfileExistsAsync(ApplicationUser user) {
            if (!await _users.IsInRoleAsync(user, Roles.Lawyer))
                return;

            var exists = await _db.LawyerProfiles.AnyAsync(x => x.UserId == user.Id);
            if (exists)
                return;

            _db.LawyerProfiles.Add(new LawyerProfile {
                UserId = user.Id,
                Bio = LawyerProfileRules.DefaultIncompleteBio,
                Specialization = LawyerProfileRules.DefaultSpecialization,
                YearsOfExperience = 0,
                ConsultationPrice = 0,
                ConsultationPriceUnit = UiText.PriceUnitHour,
                IsAvailable = true,
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
        }

        private async Task<IActionResult> DashboardAsync(ApplicationUser user) {
            if (await _users.IsInRoleAsync(user, Roles.Admin)) return RedirectToAction("AdminDashboard", "Dashboard");
            if (await _users.IsInRoleAsync(user, Roles.CourtEmployee)) return RedirectToAction("CourtEmployeeDashboard", "Dashboard");
            if (await _users.IsInRoleAsync(user, Roles.Lawyer)) {
                if (user.LawyerApprovalStatus != VerificationStatus.Approved)
                    return RedirectToAction(nameof(LawyerPendingApproval));

                var profile = await _db.LawyerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (!LawyerProfileRules.IsProfessionalProfileComplete(profile))
                    return RedirectToAction("Index", "Profile", new { professionalRequired = true });

                return RedirectToAction("LawyerDashboard", "Dashboard");
            }
            if (await _users.IsInRoleAsync(user, Roles.Client)) return RedirectToAction("ClientDashboard", "Dashboard");
            await _signIn.SignOutAsync(); return RedirectToAction(nameof(Login));
        }
    }
}
