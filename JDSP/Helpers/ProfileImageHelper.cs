using Microsoft.AspNetCore.Http;
namespace JDSP.Helpers {
    public static class ProfileImageHelper {
        private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };
        public static async Task<(bool Success, string? Path, string? Error)> SaveAsync(IFormFile? file, IWebHostEnvironment env, string? oldPath = null) {
            if (file == null || file.Length == 0) return (false, null, "Please choose an image.");
            if (file.Length > 5 * 1024 * 1024) return (false, null, "The image must be 5 MB or smaller.");
            var ext = Path.GetExtension(file.FileName);
            if (!Extensions.Contains(ext) || !Types.Contains(file.ContentType)) return (false, null, "Only JPG, PNG, and WEBP images are allowed.");
            var dir = Path.Combine(env.WebRootPath, "uploads", "profiles"); Directory.CreateDirectory(dir);
            var name = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            await using (var stream = new FileStream(Path.Combine(dir, name), FileMode.CreateNew)) await file.CopyToAsync(stream);
            if (!string.IsNullOrWhiteSpace(oldPath) && oldPath.StartsWith("/uploads/profiles/", StringComparison.OrdinalIgnoreCase)) {
                var old = Path.Combine(dir, Path.GetFileName(oldPath)); if (File.Exists(old)) File.Delete(old);
            }
            return (true, $"/uploads/profiles/{name}", null);
        }
    }
}
