using Microsoft.AspNetCore.Http;

namespace JDSP.Helpers {
    public static class VerificationFileHelper {
        private const long MaxBytes = 10 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) {
            ".pdf", ".jpg", ".jpeg", ".png", ".webp"
        };

        public static async Task<(bool Success, string? StoredName, string? OriginalName, string? Error)> SaveAsync(
            IFormFile? file,
            IWebHostEnvironment environment) {
            if (file == null || file.Length == 0)
                return (false, null, null, "Please upload the required verification document.");

            if (file.Length > MaxBytes)
                return (false, null, null, "The verification document cannot exceed 10 MB.");

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                return (false, null, null, "Allowed formats: PDF, JPG, PNG and WEBP.");

            var folder = Path.Combine(environment.ContentRootPath, "App_Data", "lawyer-verifications");
            Directory.CreateDirectory(folder);

            var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var fullPath = Path.Combine(folder, storedName);

            await using var stream = new FileStream(fullPath, FileMode.CreateNew);
            await file.CopyToAsync(stream);

            return (true, storedName, Path.GetFileName(file.FileName), null);
        }

        public static string GetPhysicalPath(string storedName, IWebHostEnvironment environment) {
            var safeName = Path.GetFileName(storedName);
            return Path.Combine(environment.ContentRootPath, "App_Data", "lawyer-verifications", safeName);
        }
    }
}
