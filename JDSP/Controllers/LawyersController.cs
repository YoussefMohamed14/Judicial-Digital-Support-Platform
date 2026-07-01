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

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? specialization) {
            var currentUserId = _userManager.GetUserId(User);

            var query = _context.LawyerProfiles
                .Include(x => x.User)
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
                .Select(x => new LawyerListItemViewModel {
                    LawyerProfileId = x.LawyerProfileId,
                    LawyerUserId = x.UserId,
                    FullName = x.User.FirstName + " " + x.User.LastName,
                    Email = x.User.Email ?? "",
                    Specialization = x.Specialization,
                    YearsOfExperience = x.YearsOfExperience,
                    ConsultationPrice = x.ConsultationPrice,
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

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id) {
            var lawyer = await _context.LawyerProfiles
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.LawyerProfileId == id);

            if (lawyer == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);

            var isFollowed = currentUserId != null &&
                await _context.LawyerFollows.AnyAsync(x =>
                    x.FollowerId == currentUserId &&
                    x.LawyerId == lawyer.UserId);

            var model = new LawyerListItemViewModel {
                LawyerProfileId = lawyer.LawyerProfileId,
                LawyerUserId = lawyer.UserId,
                FullName = lawyer.User.FirstName + " " + lawyer.User.LastName,
                Email = lawyer.User.Email ?? "",
                Specialization = lawyer.Specialization,
                YearsOfExperience = lawyer.YearsOfExperience,
                ConsultationPrice = lawyer.ConsultationPrice,
                IsAvailable = lawyer.IsAvailable,
                IsFollowed = isFollowed
            };

            ViewBag.Bio = lawyer.Bio;

            return View(model);
        }

        [Authorize(Roles = Roles.Lawyer)]
        [HttpGet]
        public async Task<IActionResult> CreateProfile() {
            var userId = _userManager.GetUserId(User);

            var existingProfile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (existingProfile != null)
                return RedirectToAction(nameof(EditProfile));

            return View(new LawyerProfileViewModel());
        }

        [Authorize(Roles = Roles.Lawyer)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfile(LawyerProfileViewModel model) {
            if (!ModelState.IsValid)
                return View(model);

            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Unauthorized();

            var existingProfile = await _context.LawyerProfiles
                .AnyAsync(x => x.UserId == userId);

            if (existingProfile) {
                TempData["Error"] = "You already have a lawyer profile.";
                return RedirectToAction(nameof(EditProfile));
            }

            var profile = new LawyerProfile {
                UserId = userId,
                Bio = model.Bio,
                Specialization = model.Specialization,
                YearsOfExperience = model.YearsOfExperience,
                ConsultationPrice = model.ConsultationPrice,
                IsAvailable = model.IsAvailable,
                CreatedAt = DateTime.Now
            };

            _context.LawyerProfiles.Add(profile);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Lawyer profile created successfully.";
            return RedirectToAction(nameof(Details), new { id = profile.LawyerProfileId });
        }

        [Authorize(Roles = Roles.Lawyer)]
        [HttpGet]
        public async Task<IActionResult> EditProfile() {
            var userId = _userManager.GetUserId(User);

            var profile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile == null)
                return RedirectToAction(nameof(CreateProfile));

            var model = new LawyerProfileViewModel {
                LawyerProfileId = profile.LawyerProfileId,
                Bio = profile.Bio,
                Specialization = profile.Specialization,
                YearsOfExperience = profile.YearsOfExperience,
                ConsultationPrice = profile.ConsultationPrice,
                IsAvailable = profile.IsAvailable
            };

            return View(model);
        }

        [Authorize(Roles = Roles.Lawyer)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(LawyerProfileViewModel model) {
            if (!ModelState.IsValid)
                return View(model);

            var userId = _userManager.GetUserId(User);

            var profile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile == null)
                return NotFound();

            profile.Bio = model.Bio;
            profile.Specialization = model.Specialization;
            profile.YearsOfExperience = model.YearsOfExperience;
            profile.ConsultationPrice = model.ConsultationPrice;
            profile.IsAvailable = model.IsAvailable;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Lawyer profile updated successfully.";
            return RedirectToAction(nameof(Details), new { id = profile.LawyerProfileId });
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
                .Where(x => x.FollowerId == clientId)
                .Include(x => x.Lawyer)
                .Join(
                    _context.LawyerProfiles,
                    follow => follow.LawyerId,
                    profile => profile.UserId,
                    (follow, profile) => new LawyerListItemViewModel {
                        LawyerProfileId = profile.LawyerProfileId,
                        LawyerUserId = profile.UserId,
                        FullName = follow.Lawyer.FirstName + " " + follow.Lawyer.LastName,
                        Email = follow.Lawyer.Email ?? "",
                        Specialization = profile.Specialization,
                        YearsOfExperience = profile.YearsOfExperience,
                        ConsultationPrice = profile.ConsultationPrice,
                        IsAvailable = profile.IsAvailable,
                        IsFollowed = true
                    })
                .ToListAsync();

            return View(lawyers);
        }
    }
}