using JDSP.Data;
using JDSP.Helpers;
using JDSP.Models;
using JDSP.ViewModels.IdentityChangeRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace JDSP.Controllers {
    [Authorize(Roles = Roles.CourtEmployee + "," + Roles.Admin)]
    public class IdentityChangeRequestsController : Controller {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IWebHostEnvironment _env;

        public IdentityChangeRequestsController(ApplicationDbContext db, UserManager<ApplicationUser> users, IWebHostEnvironment env) {
            _db = db;
            _users = users;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? status) {
            var selectedStatus = string.IsNullOrWhiteSpace(status) ? VerificationStatus.Pending : status;
            var allowed = new[] { VerificationStatus.Pending, VerificationStatus.Approved, VerificationStatus.Rejected };
            if (!allowed.Contains(selectedStatus)) selectedStatus = VerificationStatus.Pending;

            var items = await _db.IdentityChangeRequests.AsNoTracking()
                .Where(x => x.Status == selectedStatus && x.RequestedBy != null)
                .OrderByDescending(x => x.RequestedAt)
                .Select(x => new IdentityChangeRequestListItemViewModel {
                    RequestId = x.Id,
                    RequestedByName = x.RequestedBy!.FirstName + " " + x.RequestedBy.LastName,
                    Email = x.RequestedBy.Email ?? string.Empty,
                    CurrentFullName = x.CurrentFullName,
                    RequestedFullName = x.RequestedFullName,
                    Status = x.Status,
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
            var request = await _db.IdentityChangeRequests.Include(x => x.RequestedBy).FirstOrDefaultAsync(x => x.Id == id);
            var reviewer = await _users.GetUserAsync(User);
            if (request == null || request.RequestedBy == null || reviewer == null) return NotFound();
            if (request.Status != VerificationStatus.Pending) return RedirectToAction(nameof(Details), new { id });

            var duplicateNationalNumber = await _users.Users.AnyAsync(x => x.Id != request.RequestedById && x.NationalNumber == request.RequestedNationalNumber);
            if (duplicateNationalNumber) {
                TempData["Error"] = Text(
                    "Cannot approve this request because the requested national number already belongs to another account.",
                    "لا يمكن قبول هذا الطلب لأن الرقم القومي المطلوب مستخدم في حساب آخر.");
                return RedirectToAction(nameof(Details), new { id });
            }

            ApplyName(request.RequestedBy, request.RequestedFullName);
            request.RequestedBy.PhoneNumber = request.RequestedPhoneNumber;
            request.RequestedBy.NationalNumber = request.RequestedNationalNumber;

            request.Status = VerificationStatus.Approved;
            request.RejectionReason = null;
            request.ReviewedAt = DateTime.Now;
            request.ReviewedById = reviewer.Id;

            AddIdentityNotification(
                request.RequestedBy,
                approved: true,
                reason: null);

            await _db.SaveChangesAsync();
            TempData["Success"] = Text(
                "The identity change request was approved and the user information was updated.",
                "تمت الموافقة على طلب تعديل الهوية وتحديث بيانات المستخدم.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Reject(int id) {
            var request = await _db.IdentityChangeRequests.AsNoTracking().Include(x => x.RequestedBy).FirstOrDefaultAsync(x => x.Id == id);
            if (request == null || request.RequestedBy == null) return NotFound();

            return View(new RejectIdentityChangeRequestViewModel {
                RequestId = request.Id,
                RequestedByName = request.RequestedBy.FirstName + " " + request.RequestedBy.LastName,
                Email = request.RequestedBy.Email ?? string.Empty
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(RejectIdentityChangeRequestViewModel model) {
            if (string.IsNullOrWhiteSpace(model.RejectionReason))
                ModelState.AddModelError(nameof(model.RejectionReason), Text("Rejection reason is required.", "سبب الرفض مطلوب."));
            if (!ModelState.IsValid) return View(model);

            var request = await _db.IdentityChangeRequests.Include(x => x.RequestedBy).FirstOrDefaultAsync(x => x.Id == model.RequestId);
            var reviewer = await _users.GetUserAsync(User);
            if (request == null || request.RequestedBy == null || reviewer == null) return NotFound();
            if (request.Status != VerificationStatus.Pending) return RedirectToAction(nameof(Details), new { id = model.RequestId });

            request.Status = VerificationStatus.Rejected;
            request.RejectionReason = model.RejectionReason.Trim();
            request.ReviewedAt = DateTime.Now;
            request.ReviewedById = reviewer.Id;

            AddIdentityNotification(
                request.RequestedBy,
                approved: false,
                reason: request.RejectionReason);

            await _db.SaveChangesAsync();
            TempData["Success"] = Text(
                "The identity change request was rejected and the user was notified.",
                "تم رفض طلب تعديل الهوية وإخطار المستخدم.");
            return RedirectToAction(nameof(Details), new { id = model.RequestId });
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id) {
            var request = await _db.IdentityChangeRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (request == null) return NotFound();

            var path = IdentityFileHelper.GetPhysicalPath(request.LegalIdFilePath, _env);
            if (!System.IO.File.Exists(path)) return NotFound();

            return PhysicalFile(path, "application/octet-stream", request.LegalIdFileName);
        }

        private async Task<IdentityChangeRequestDetailsViewModel?> BuildDetailsAsync(int id) {
            return await _db.IdentityChangeRequests.AsNoTracking()
                .Where(x => x.Id == id && x.RequestedBy != null)
                .Select(x => new IdentityChangeRequestDetailsViewModel {
                    RequestId = x.Id,
                    RequestedById = x.RequestedById,
                    RequestedByName = x.RequestedBy!.FirstName + " " + x.RequestedBy.LastName,
                    Email = x.RequestedBy.Email ?? string.Empty,
                    Status = x.Status,
                    RejectionReason = x.RejectionReason,
                    RequestedAt = x.RequestedAt,
                    ReviewedAt = x.ReviewedAt,
                    CurrentFullName = x.CurrentFullName,
                    RequestedFullName = x.RequestedFullName,
                    CurrentPhoneNumber = x.CurrentPhoneNumber,
                    RequestedPhoneNumber = x.RequestedPhoneNumber,
                    CurrentNationalNumber = x.CurrentNationalNumber,
                    RequestedNationalNumber = x.RequestedNationalNumber,
                    LegalIdFileName = x.LegalIdFileName
                })
                .FirstOrDefaultAsync();
        }

        private string Text(string en, string ar) {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? ar : en;
        }

        private void AddIdentityNotification(ApplicationUser recipient, bool approved, string? reason) {
            var ar = recipient.PreferredLanguage == "ar";
            string title;
            string body;

            if (approved) {
                title = ar ? "تم قبول طلب تعديل الهوية" : "Identity change request approved";
                body = ar
                    ? "وافق موظف المحكمة على طلب تعديل بيانات هويتك. تم تحديث البيانات في ملفك الشخصي تلقائياً."
                    : "The court employee approved your identity change request. Your profile information was updated automatically.";
            }
            else {
                title = ar ? "تم رفض طلب تعديل الهوية" : "Identity change request rejected";
                body = ar
                    ? $"رفض موظف المحكمة طلب تعديل بيانات هويتك. السبب: {reason}"
                    : $"The court employee rejected your identity change request. Reason: {reason}";
            }

            _db.SystemNotifications.Add(new SystemNotification {
                RecipientId = recipient.Id,
                Title = title,
                Body = body,
                Category = "IdentityChange",
                IsRead = false,
                CreatedAt = DateTime.Now
            });
        }

        private static void ApplyName(ApplicationUser user, string fullName) {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            user.FirstName = parts[0];
            user.LastName = parts.Length == 1 ? parts[0] : parts[^1];
            user.MiddleName = parts.Length > 2 ? string.Join(" ", parts.Skip(1).Take(parts.Length - 2)) : null;
        }
    }
}
