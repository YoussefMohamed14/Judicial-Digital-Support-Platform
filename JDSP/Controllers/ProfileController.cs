using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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
        public async Task<IActionResult> Index(bool professionalRequired = false) {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();

            var model = await MapAsync(user);
            model.ProfessionalProfileRequired = professionalRequired || (model.IsLawyer && !await IsProfessionalProfileCompleteAsync(user.Id));
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ClientProfileViewModel model) {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();

            var isLawyer = await _users.IsInRoleAsync(user, Roles.Lawyer);
            model.IsLawyer = isLawyer;

            foreach (var key in new[] {
                nameof(model.FullName), nameof(model.Email), nameof(model.PhoneNumber),
                nameof(model.NationalNumber), nameof(model.Role), nameof(model.LawyerProfileId),
                nameof(model.ProfessionalProfileRequired)
            }) ModelState.Remove(key);

            foreach (var key in new[] {
                nameof(model.ProfessionalBio), nameof(model.Specialization), nameof(model.CustomSpecialization),
                nameof(model.YearsOfExperience), nameof(model.ConsultationPrice), nameof(model.ConsultationPriceUnit),
                nameof(model.IsAvailable)
            }) ModelState.Remove(key);

            if (isLawyer) {
                ValidateProfessionalProfile(model);
            }

            if (!ModelState.IsValid) {
                var current = await MapAsync(user);
                CopyEditableFields(model, current, isLawyer);
                current.ProfessionalProfileRequired = isLawyer && !await IsProfessionalProfileCompleteAsync(user.Id);
                return View("Index", current);
            }

            if (model.NewPhoto != null) {
                var upload = await ProfileImageHelper.SaveAsync(model.NewPhoto, _env, user.PhotoPath);
                if (!upload.Success) {
                    ModelState.AddModelError(nameof(model.NewPhoto), upload.Error ?? "Upload failed.");
                    var current = await MapAsync(user);
                    CopyEditableFields(model, current, isLawyer);
                    current.ProfessionalProfileRequired = isLawyer && !await IsProfessionalProfileCompleteAsync(user.Id);
                    return View("Index", current);
                }
                user.PhotoPath = upload.Path;
            }

            user.Bio = model.Bio.Trim();
            user.IsProfileCompleted = true;

            if (isLawyer) {
                var lawyerProfile = await EnsureLawyerProfileExistsAsync(user);
                lawyerProfile.Bio = model.ProfessionalBio.Trim();
                lawyerProfile.Specialization = LawyerProfileRules.NormalizeSpecialization(model.Specialization, model.CustomSpecialization);
                lawyerProfile.YearsOfExperience = model.YearsOfExperience;
                lawyerProfile.ConsultationPrice = model.ConsultationPrice;
                lawyerProfile.ConsultationPriceUnit = model.ConsultationPriceUnit;
                lawyerProfile.IsAvailable = model.IsAvailable;
            }

            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded) {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View("Index", await MapAsync(user));
            }

            await _db.SaveChangesAsync();
            await _signIn.RefreshSignInAsync(user);
            TempData["Success"] = isLawyer ? "Profile and professional details updated successfully." : "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestIdentityChange(ClientProfileViewModel model) {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();

            ModelState.Clear();

            var existingPendingRequest = await _db.IdentityChangeRequests
                .AsNoTracking()
                .AnyAsync(x => x.RequestedById == user.Id && x.Status == VerificationStatus.Pending);

            if (existingPendingRequest) {
                TempData["Error"] = Text(
                    "You already have a pending identity change request. Please wait for court employee review.",
                    "لديك بالفعل طلب تعديل هوية قيد المراجعة. يرجى انتظار مراجعة موظف المحكمة.");
                return RedirectToAction(nameof(Index));
            }

            var currentFullName = BuildFullName(user);
            var requestedFullName = NormalizeSpaces(model.RequestedFullName);
            var requestedPhone = NormalizeSpaces(model.RequestedPhoneNumber);
            var requestedNational = NormalizeSpaces(model.RequestedNationalNumber);

            if (string.IsNullOrWhiteSpace(requestedFullName))
                ModelState.AddModelError(nameof(model.RequestedFullName), Text("Full name is required.", "الاسم الكامل مطلوب."));
            else if (requestedFullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2)
                ModelState.AddModelError(nameof(model.RequestedFullName), Text("Enter at least first and last name.", "أدخل الاسم الأول والأخير على الأقل."));

            if (string.IsNullOrWhiteSpace(requestedNational))
                ModelState.AddModelError(nameof(model.RequestedNationalNumber), Text("National number is required.", "الرقم القومي مطلوب."));

            var nameChanged = !string.Equals(currentFullName, requestedFullName, StringComparison.Ordinal);
            var phoneChanged = !string.Equals(user.PhoneNumber ?? string.Empty, requestedPhone, StringComparison.Ordinal);
            var nationalChanged = !string.Equals(user.NationalNumber, requestedNational, StringComparison.Ordinal);

            if (!nameChanged && !phoneChanged && !nationalChanged)
                ModelState.AddModelError(string.Empty, Text("Change at least one identity field before sending the request.", "غيّر خانة واحدة على الأقل قبل إرسال الطلب."));

            if (model.LegalIdFile == null || model.LegalIdFile.Length == 0)
                ModelState.AddModelError(nameof(model.LegalIdFile), Text("Attach your legal ID as a reference for court review.", "أرفق بطاقة الهوية القانونية كمرجع لمراجعة المحكمة."));

            if (!ModelState.IsValid) {
                var current = await MapAsync(user);
                CopyIdentityRequestFields(model, current);
                return View("Index", current);
            }

            var upload = await IdentityFileHelper.SaveAsync(model.LegalIdFile, _env);
            if (!upload.Success) {
                ModelState.AddModelError(nameof(model.LegalIdFile), Text(upload.Error ?? "Upload failed.", upload.Error ?? "فشل رفع الملف."));
                var current = await MapAsync(user);
                CopyIdentityRequestFields(model, current);
                return View("Index", current);
            }

            _db.IdentityChangeRequests.Add(new IdentityChangeRequest {
                RequestedById = user.Id,
                CurrentFullName = currentFullName,
                RequestedFullName = requestedFullName,
                CurrentPhoneNumber = user.PhoneNumber,
                RequestedPhoneNumber = string.IsNullOrWhiteSpace(requestedPhone) ? null : requestedPhone,
                CurrentNationalNumber = user.NationalNumber,
                RequestedNationalNumber = requestedNational,
                LegalIdFileName = upload.OriginalName ?? "legal-id",
                LegalIdFilePath = upload.StoredName ?? string.Empty,
                Status = VerificationStatus.Pending,
                RequestedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = Text(
                "Your identity change request was sent to the court employee for review.",
                "تم إرسال طلب تعديل الهوية إلى موظف المحكمة للمراجعة.");
            return RedirectToAction(nameof(Index));
        }

        private void ValidateProfessionalProfile(ClientProfileViewModel model) {
            var ar = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
            var specialization = LawyerProfileRules.NormalizeSpecialization(model.Specialization, model.CustomSpecialization);

            if (string.IsNullOrWhiteSpace(model.ProfessionalBio))
                ModelState.AddModelError(nameof(model.ProfessionalBio), ar ? "النبذة المهنية مطلوبة." : "Professional bio is required.");
            else if (model.ProfessionalBio.Trim().Length < 10)
                ModelState.AddModelError(nameof(model.ProfessionalBio), ar ? "يجب أن تكون النبذة المهنية 10 أحرف على الأقل." : "Professional bio must be at least 10 characters.");

            if (string.IsNullOrWhiteSpace(specialization))
                ModelState.AddModelError(nameof(model.Specialization), ar ? "اختر تخصصاً أو اكتب تخصصاً في خيار أخرى." : "Please choose a specialization or type one in Other.");

            if (model.YearsOfExperience < 0 || model.YearsOfExperience > 60)
                ModelState.AddModelError(nameof(model.YearsOfExperience), ar ? "سنوات الخبرة يجب أن تكون بين 0 و 60." : "Years of experience must be between 0 and 60.");

            if (model.ConsultationPrice <= 0)
                ModelState.AddModelError(nameof(model.ConsultationPrice), ar ? "يجب أن يكون السعر أكبر من صفر." : "Price must be greater than 0.");

            if (!LawyerProfileRules.IsValidPriceUnit(model.ConsultationPriceUnit))
                ModelState.AddModelError(nameof(model.ConsultationPriceUnit), ar ? "اختر هل السعر بالساعة أم بالشهر." : "Please choose whether the price is per hour or per month.");
        }

        private static void CopyEditableFields(ClientProfileViewModel source, ClientProfileViewModel target, bool isLawyer) {
            target.Bio = source.Bio;
            if (!isLawyer) return;

            target.ProfessionalBio = source.ProfessionalBio;
            target.Specialization = source.Specialization;
            target.CustomSpecialization = source.CustomSpecialization;
            target.YearsOfExperience = source.YearsOfExperience;
            target.ConsultationPrice = source.ConsultationPrice;
            target.ConsultationPriceUnit = source.ConsultationPriceUnit;
            target.IsAvailable = source.IsAvailable;
        }

        private static void CopyIdentityRequestFields(ClientProfileViewModel source, ClientProfileViewModel target) {
            target.RequestedFullName = source.RequestedFullName;
            target.RequestedPhoneNumber = source.RequestedPhoneNumber;
            target.RequestedNationalNumber = source.RequestedNationalNumber;
        }

        private async Task<ClientProfileViewModel> MapAsync(ApplicationUser user) {
            var roles = await _users.GetRolesAsync(user);
            var middle = string.IsNullOrWhiteSpace(user.MiddleName) ? "" : $" {user.MiddleName}";
            var isLawyer = roles.Contains(Roles.Lawyer);
            var model = new ClientProfileViewModel {
                FullName = $"{user.FirstName}{middle} {user.LastName}",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                NationalNumber = user.NationalNumber,
                Role = roles.FirstOrDefault() ?? "User",
                CreatedAt = user.CreatedAt,
                PhotoPath = user.PhotoPath,
                Bio = user.Bio ?? "",
                IsLawyer = isLawyer
            };

            var pendingIdentityRequest = await _db.IdentityChangeRequests.AsNoTracking()
                .Where(x => x.RequestedById == user.Id && x.Status == VerificationStatus.Pending)
                .OrderByDescending(x => x.RequestedAt)
                .FirstOrDefaultAsync();

            model.HasPendingIdentityChangeRequest = pendingIdentityRequest != null;
            model.PendingIdentityChangeRequestedAt = pendingIdentityRequest?.RequestedAt;
            model.RequestedFullName = pendingIdentityRequest?.RequestedFullName ?? model.FullName;
            model.RequestedPhoneNumber = pendingIdentityRequest?.RequestedPhoneNumber ?? model.PhoneNumber;
            model.RequestedNationalNumber = pendingIdentityRequest?.RequestedNationalNumber ?? model.NationalNumber;

            if (isLawyer) {
                var profile = await EnsureLawyerProfileExistsAsync(user);
                model.LawyerProfileId = profile.LawyerProfileId;
                model.ProfessionalBio = profile.Bio == LawyerProfileRules.DefaultIncompleteBio ? string.Empty : profile.Bio;
                model.Specialization = LawyerProfileRules.ToSelectedSpecialization(profile.Specialization);
                model.CustomSpecialization = LawyerProfileRules.ToCustomSpecialization(profile.Specialization);
                model.YearsOfExperience = profile.YearsOfExperience;
                model.ConsultationPrice = profile.ConsultationPrice;
                model.ConsultationPriceUnit = string.IsNullOrWhiteSpace(profile.ConsultationPriceUnit) ? UiText.PriceUnitHour : profile.ConsultationPriceUnit;
                model.IsAvailable = profile.IsAvailable;
                model.ProfessionalProfileRequired = !LawyerProfileRules.IsProfessionalProfileComplete(profile);
            }

            return model;
        }

        private async Task<LawyerProfile> EnsureLawyerProfileExistsAsync(ApplicationUser user) {
            var profile = await _db.LawyerProfiles.FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (profile != null) {
                if (string.IsNullOrWhiteSpace(profile.ConsultationPriceUnit)) profile.ConsultationPriceUnit = UiText.PriceUnitHour;
                return profile;
            }

            profile = new LawyerProfile {
                UserId = user.Id,
                Bio = LawyerProfileRules.DefaultIncompleteBio,
                Specialization = LawyerProfileRules.DefaultSpecialization,
                YearsOfExperience = 0,
                ConsultationPrice = 0,
                ConsultationPriceUnit = UiText.PriceUnitHour,
                IsAvailable = true,
                CreatedAt = DateTime.Now
            };

            _db.LawyerProfiles.Add(profile);
            await _db.SaveChangesAsync();
            return profile;
        }

        private async Task<bool> IsProfessionalProfileCompleteAsync(string userId) {
            var profile = await _db.LawyerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
            return LawyerProfileRules.IsProfessionalProfileComplete(profile);
        }

        private string Text(string en, string ar) {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? ar : en;
        }

        private static string BuildFullName(ApplicationUser user) {
            return NormalizeSpaces($"{user.FirstName} {user.MiddleName} {user.LastName}");
        }

        private static string NormalizeSpaces(string? value) {
            return string.Join(" ", (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
