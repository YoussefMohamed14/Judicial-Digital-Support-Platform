using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.CourtEmployee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JDSP.Controllers {
    [Authorize(Roles = Roles.CourtEmployee + "," + Roles.Admin)]
    public class LawyerApprovalsController : Controller {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IWebHostEnvironment _env;

        public LawyerApprovalsController(ApplicationDbContext db, UserManager<ApplicationUser> users, IWebHostEnvironment env) {
            _db = db;
            _users = users;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? status) {
            var selectedStatus = string.IsNullOrWhiteSpace(status) ? VerificationStatus.Pending : status;
            var allowed = new[] { VerificationStatus.Pending, VerificationStatus.Approved, VerificationStatus.Rejected };
            if (!allowed.Contains(selectedStatus)) selectedStatus = VerificationStatus.Pending;

            var items = await _db.LawyerVerificationRequests.AsNoTracking()
                .Where(x => x.Status == selectedStatus && x.Lawyer != null)
                .OrderByDescending(x => x.RequestedAt)
                .Select(x => new PendingLawyerApprovalItemViewModel {
                    RequestId = x.Id,
                    LawyerName = x.Lawyer!.FirstName + " " + x.Lawyer.LastName,
                    Email = x.Lawyer.Email ?? string.Empty,
                    NationalNumber = x.Lawyer.NationalNumber,
                    RequestedAt = x.RequestedAt
                })
                .ToListAsync();

            ViewBag.Status = selectedStatus;
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id) {
            var model = await BuildDetailsAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id) {
            var request = await _db.LawyerVerificationRequests.Include(x => x.Lawyer).FirstOrDefaultAsync(x => x.Id == id);
            var reviewer = await _users.GetUserAsync(User);
            if (request == null || request.Lawyer == null || reviewer == null) return NotFound();

            request.Status = VerificationStatus.Approved;
            request.RejectionReason = null;
            request.ReviewedAt = DateTime.Now;
            request.ReviewedById = reviewer.Id;

            request.Lawyer.LawyerApprovalStatus = VerificationStatus.Approved;
            request.Lawyer.LawyerApprovalRejectionReason = null;
            request.Lawyer.LawyerApprovalReviewedAt = DateTime.Now;
            request.Lawyer.LawyerApprovalReviewedById = reviewer.Id;

            await EnsureLawyerProfileExistsAsync(request.Lawyer);
            await _db.SaveChangesAsync();

            TempData["Success"] = "The lawyer account was approved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Reject(int id) {
            var request = await _db.LawyerVerificationRequests.AsNoTracking().Include(x => x.Lawyer).FirstOrDefaultAsync(x => x.Id == id);
            if (request == null || request.Lawyer == null) return NotFound();

            return View(new RejectLawyerApprovalViewModel {
                RequestId = request.Id,
                LawyerName = request.Lawyer.FirstName + " " + request.Lawyer.LastName,
                Email = request.Lawyer.Email ?? string.Empty
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(RejectLawyerApprovalViewModel model) {
            if (string.IsNullOrWhiteSpace(model.RejectionReason))
                ModelState.AddModelError(nameof(model.RejectionReason), "Rejection reason is required.");
            if (!ModelState.IsValid) return View(model);

            var request = await _db.LawyerVerificationRequests.Include(x => x.Lawyer).FirstOrDefaultAsync(x => x.Id == model.RequestId);
            var reviewer = await _users.GetUserAsync(User);
            if (request == null || request.Lawyer == null || reviewer == null) return NotFound();

            request.Status = VerificationStatus.Rejected;
            request.RejectionReason = model.RejectionReason.Trim();
            request.ReviewedAt = DateTime.Now;
            request.ReviewedById = reviewer.Id;

            request.Lawyer.LawyerApprovalStatus = VerificationStatus.Rejected;
            request.Lawyer.LawyerApprovalRejectionReason = model.RejectionReason.Trim();
            request.Lawyer.LawyerApprovalReviewedAt = DateTime.Now;
            request.Lawyer.LawyerApprovalReviewedById = reviewer.Id;

            await _db.SaveChangesAsync();
            TempData["Success"] = "The lawyer account was rejected.";
            return RedirectToAction(nameof(Details), new { id = model.RequestId });
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id, string file) {
            var request = await _db.LawyerVerificationRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (request == null) return NotFound();

            var isLawyerId = string.Equals(file, "lawyer", StringComparison.OrdinalIgnoreCase);
            var storedName = isLawyerId ? request.LawyerIdFilePath : request.NationalIdFilePath;
            var downloadName = isLawyerId ? request.LawyerIdFileName : request.NationalIdFileName;
            var path = VerificationFileHelper.GetPhysicalPath(storedName, _env);
            if (!System.IO.File.Exists(path)) return NotFound();

            return PhysicalFile(path, "application/octet-stream", downloadName);
        }

        private async Task<LawyerApprovalDetailsViewModel?> BuildDetailsAsync(int id) {
            return await _db.LawyerVerificationRequests.AsNoTracking()
                .Where(x => x.Id == id && x.Lawyer != null)
                .Select(x => new LawyerApprovalDetailsViewModel {
                    RequestId = x.Id,
                    LawyerId = x.LawyerId,
                    LawyerName = x.Lawyer!.FirstName + " " + x.Lawyer.LastName,
                    Email = x.Lawyer.Email ?? string.Empty,
                    PhoneNumber = x.Lawyer.PhoneNumber ?? string.Empty,
                    NationalNumber = x.Lawyer.NationalNumber,
                    Status = x.Status,
                    RejectionReason = x.RejectionReason,
                    RequestedAt = x.RequestedAt,
                    ReviewedAt = x.ReviewedAt,
                    NationalIdFileName = x.NationalIdFileName,
                    LawyerIdFileName = x.LawyerIdFileName
                })
                .FirstOrDefaultAsync();
        }

        private async Task EnsureLawyerProfileExistsAsync(ApplicationUser lawyer) {
            if (await _db.LawyerProfiles.AnyAsync(x => x.UserId == lawyer.Id)) return;

            _db.LawyerProfiles.Add(new LawyerProfile {
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
    }
}
