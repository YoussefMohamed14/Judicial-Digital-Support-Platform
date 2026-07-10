using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.Lawyers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JDSP.Controllers {
    public class LawyersController : Controller {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LawyersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager) {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = Roles.Client)]
        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? specialization, int? caseId) {
            var currentUserId = _userManager.GetUserId(User);

            // The lawyer directory must be based on accounts that actually have the Lawyer role,
            // not only on rows that already exist in LawyerProfiles.
            // Incomplete lawyer accounts get a profile row, but they are not listed
            // to clients until the required professional details are completed.
            var lawyerUsers = await _userManager.GetUsersInRoleAsync(Roles.Lawyer);
            lawyerUsers = lawyerUsers
                .Where(x => x.AccountStatus == "Active" && x.LawyerApprovalStatus == VerificationStatus.Approved)
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ToList();

            await EnsureLawyerProfilesExistAsync(lawyerUsers);

            var lawyerUserIds = lawyerUsers.Select(x => x.Id).ToList();

            var query = _context.LawyerProfiles
                .Include(x => x.User)
                .Where(x => lawyerUserIds.Contains(x.UserId))
                .Where(x => x.ConsultationPrice > 0 &&
                    x.Bio != LawyerProfileRules.DefaultIncompleteBio &&
                    x.Bio.Length >= 10 &&
                    x.Specialization != "" &&
                    (x.ConsultationPriceUnit == UiText.PriceUnitHour || x.ConsultationPriceUnit == UiText.PriceUnitMonth))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm)) {
                query = query.Where(x =>
                    x.User.FirstName.Contains(searchTerm) ||
                    x.User.LastName.Contains(searchTerm) ||
                    x.Specialization.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(specialization)) {
                query = query.Where(x => x.Specialization.Contains(specialization));
            }

            var lawyers = await query
                .OrderBy(x => x.User.FirstName)
                .ThenBy(x => x.User.LastName)
                .Select(x => new LawyerListItemViewModel {
                    LawyerProfileId = x.LawyerProfileId,
                    LawyerUserId = x.UserId,
                    FullName = x.User.FirstName + " " + x.User.LastName,
                    Email = x.User.Email ?? "",
                    PhotoPath = x.User.PhotoPath,
                    Specialization = x.Specialization,
                    YearsOfExperience = x.YearsOfExperience,
                    ConsultationPrice = x.ConsultationPrice,
                    ConsultationPriceUnit = x.ConsultationPriceUnit,
                    IsAvailable = x.IsAvailable,
                    IsFollowed = currentUserId != null &&
                        _context.LawyerFollows.Any(f =>
                            f.FollowerId == currentUserId &&
                            f.LawyerId == x.UserId)
                })
                .ToListAsync();

            var model = new LawyerSearchViewModel {
                SearchTerm = searchTerm,
                Specialization = specialization,
                Lawyers = lawyers
            };

            ViewBag.CaseId = caseId;
            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Details(int id, int? caseId) {
            var lawyer = await _context.LawyerProfiles
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.LawyerProfileId == id);

            if (lawyer == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (User.IsInRole(Roles.Client) &&
                (lawyer.User.AccountStatus != "Active" ||
                 lawyer.User.LawyerApprovalStatus != VerificationStatus.Approved ||
                 !LawyerProfileRules.IsProfessionalProfileComplete(lawyer)))
                return NotFound();

            if (User.IsInRole(Roles.Lawyer) && lawyer.UserId != currentUserId) {
                return Forbid();
            }

            if (!User.IsInRole(Roles.Client) && !User.IsInRole(Roles.Lawyer)) {
                return Forbid();
            }

            var isFollowed = currentUserId != null && User.IsInRole(Roles.Client) &&
                await _context.LawyerFollows.AnyAsync(x =>
                    x.FollowerId == currentUserId &&
                    x.LawyerId == lawyer.UserId);

            var model = new LawyerListItemViewModel {
                LawyerProfileId = lawyer.LawyerProfileId,
                LawyerUserId = lawyer.UserId,
                FullName = lawyer.User.FirstName + " " + lawyer.User.LastName,
                Email = lawyer.User.Email ?? "",
                PhotoPath = lawyer.User.PhotoPath,
                Specialization = lawyer.Specialization,
                YearsOfExperience = lawyer.YearsOfExperience,
                ConsultationPrice = lawyer.ConsultationPrice,
                ConsultationPriceUnit = lawyer.ConsultationPriceUnit,
                IsAvailable = lawyer.IsAvailable,
                IsFollowed = isFollowed
            };

            ViewBag.Bio = lawyer.Bio;
            ViewBag.CaseId = caseId;

            return View(model);
        }

        [Authorize(Roles = Roles.Lawyer)]
        [HttpGet]
        public async Task<IActionResult> CreateProfile() {
            var userId = _userManager.GetUserId(User);

            var existingProfile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            return RedirectToAction("Index", "Profile", new { professionalRequired = existingProfile == null });
        }

        [Authorize(Roles = Roles.Lawyer)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfile(LawyerProfileViewModel model) {
            if (!ModelState.IsValid)
                return RedirectToAction("Index", "Profile", new { professionalRequired = true });

            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Unauthorized();

            var existingProfile = await _context.LawyerProfiles
                .AnyAsync(x => x.UserId == userId);

            if (existingProfile) {
                TempData["Error"] = "You already have a lawyer profile.";
                return RedirectToAction("Index", "Profile");
            }

            var profile = new LawyerProfile {
                UserId = userId,
                Bio = model.Bio,
                Specialization = LawyerProfileRules.NormalizeSpecialization(model.Specialization, model.CustomSpecialization),
                YearsOfExperience = model.YearsOfExperience,
                ConsultationPrice = model.ConsultationPrice,
                ConsultationPriceUnit = model.ConsultationPriceUnit,
                IsAvailable = model.IsAvailable,
                CreatedAt = DateTime.Now
            };

            _context.LawyerProfiles.Add(profile);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Lawyer profile created successfully.";
            return RedirectToAction("Index", "Profile");
        }

        [Authorize(Roles = Roles.Lawyer)]
        [HttpGet]
        public async Task<IActionResult> EditProfile() {
            var userId = _userManager.GetUserId(User);

            var profile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            return RedirectToAction("Index", "Profile", new { professionalRequired = profile == null || !LawyerProfileRules.IsProfessionalProfileComplete(profile) });
        }

        [Authorize(Roles = Roles.Lawyer)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(LawyerProfileViewModel model) {
            if (!ModelState.IsValid)
                return RedirectToAction("Index", "Profile", new { professionalRequired = true });

            var userId = _userManager.GetUserId(User);

            var profile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile == null)
                return NotFound();

            profile.Bio = model.Bio;
            profile.Specialization = LawyerProfileRules.NormalizeSpecialization(model.Specialization, model.CustomSpecialization);
            profile.YearsOfExperience = model.YearsOfExperience;
            profile.ConsultationPrice = model.ConsultationPrice;
            profile.ConsultationPriceUnit = model.ConsultationPriceUnit;
            profile.IsAvailable = model.IsAvailable;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Lawyer profile updated successfully.";
            return RedirectToAction("Index", "Profile");
        }

        [Authorize(Roles = Roles.Client)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(int lawyerProfileId) {
            var clientId = _userManager.GetUserId(User);

            if (clientId == null)
                return Unauthorized();

            var lawyerProfile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(x => x.LawyerProfileId == lawyerProfileId);

            if (lawyerProfile == null)
                return NotFound();

            var alreadyFollowed = await _context.LawyerFollows.AnyAsync(x =>
                x.FollowerId == clientId &&
                x.LawyerId == lawyerProfile.UserId);

            if (!alreadyFollowed) {
                _context.LawyerFollows.Add(new LawyerFollow {
                    FollowerId = clientId,
                    LawyerId = lawyerProfile.UserId,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = lawyerProfileId });
        }

        [Authorize(Roles = Roles.Client)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfollow(int lawyerProfileId) {
            var clientId = _userManager.GetUserId(User);

            if (clientId == null)
                return Unauthorized();

            var lawyerProfile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(x => x.LawyerProfileId == lawyerProfileId);

            if (lawyerProfile == null)
                return NotFound();

            var follow = await _context.LawyerFollows.FirstOrDefaultAsync(x =>
                x.FollowerId == clientId &&
                x.LawyerId == lawyerProfile.UserId);

            if (follow != null) {
                _context.LawyerFollows.Remove(follow);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = lawyerProfileId });
        }

        [Authorize(Roles = Roles.Client)]
        [HttpGet]
        public async Task<IActionResult> FollowedLawyers() {
            var clientId = _userManager.GetUserId(User);

            var lawyers = await _context.LawyerFollows
                .Where(x => x.FollowerId == clientId && x.Lawyer != null && x.Lawyer.AccountStatus == "Active" && x.Lawyer.LawyerApprovalStatus == VerificationStatus.Approved)
                .Include(x => x.Lawyer)
                .Join(
                    _context.LawyerProfiles.Where(p => p.ConsultationPrice > 0 &&
                        p.Bio != LawyerProfileRules.DefaultIncompleteBio &&
                        p.Bio.Length >= 10 &&
                        p.Specialization != "" &&
                        (p.ConsultationPriceUnit == UiText.PriceUnitHour || p.ConsultationPriceUnit == UiText.PriceUnitMonth)),
                    follow => follow.LawyerId,
                    profile => profile.UserId,
                    (follow, profile) => new LawyerListItemViewModel {
                        LawyerProfileId = profile.LawyerProfileId,
                        LawyerUserId = profile.UserId,
                        FullName = follow.Lawyer.FirstName + " " + follow.Lawyer.LastName,
                        Email = follow.Lawyer.Email ?? "",
                        PhotoPath = follow.Lawyer.PhotoPath,
                        Specialization = profile.Specialization,
                        YearsOfExperience = profile.YearsOfExperience,
                        ConsultationPrice = profile.ConsultationPrice,
                        ConsultationPriceUnit = profile.ConsultationPriceUnit,
                        IsAvailable = profile.IsAvailable,
                        IsFollowed = true
                    })
                .ToListAsync();

            return View(lawyers);
        }
        private async Task EnsureLawyerProfilesExistAsync(IEnumerable<ApplicationUser> lawyerUsers) {
            var lawyers = lawyerUsers.ToList();
            if (!lawyers.Any())
                return;

            var lawyerUserIds = lawyers.Select(x => x.Id).ToList();
            var existingProfileUserIds = await _context.LawyerProfiles
                .Where(x => lawyerUserIds.Contains(x.UserId))
                .Select(x => x.UserId)
                .ToListAsync();

            var existingSet = existingProfileUserIds.ToHashSet();
            var missingLawyers = lawyers.Where(x => !existingSet.Contains(x.Id)).ToList();

            if (!missingLawyers.Any())
                return;

            foreach (var lawyer in missingLawyers) {
                _context.LawyerProfiles.Add(new LawyerProfile {
                    UserId = lawyer.Id,
                    Bio = LawyerProfileRules.DefaultIncompleteBio,
                    Specialization = LawyerProfileRules.DefaultSpecialization,
                    YearsOfExperience = 0,
                    ConsultationPrice = 0,
                    ConsultationPriceUnit = UiText.PriceUnitHour,
                    IsAvailable = true,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

    }
}