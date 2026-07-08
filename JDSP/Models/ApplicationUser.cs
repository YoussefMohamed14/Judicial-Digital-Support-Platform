using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace JDSP.Models {
    public class ApplicationUser : IdentityUser {
        [Required, MaxLength(50)] public string FirstName { get; set; } = string.Empty;
        [MaxLength(50)] public string? MiddleName { get; set; }
        [Required, MaxLength(50)] public string LastName { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string NationalNumber { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string AccountStatus { get; set; } = "Active";
        [MaxLength(500)] public string? PhotoPath { get; set; }
        [MaxLength(1000)] public string? Bio { get; set; }
        [Required, MaxLength(5)] public string PreferredLanguage { get; set; } = "en";
        public bool IsProfileCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();
    }
}
