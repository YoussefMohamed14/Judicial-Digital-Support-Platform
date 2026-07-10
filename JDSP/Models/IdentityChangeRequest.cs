using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models {
    public class IdentityChangeRequest {
        public int Id { get; set; }

        [Required]
        public string RequestedById { get; set; } = string.Empty;

        [ForeignKey(nameof(RequestedById))]
        public ApplicationUser? RequestedBy { get; set; }

        [Required, MaxLength(150)]
        public string CurrentFullName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string RequestedFullName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? CurrentPhoneNumber { get; set; }

        [MaxLength(30)]
        public string? RequestedPhoneNumber { get; set; }

        [Required, MaxLength(20)]
        public string CurrentNationalNumber { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string RequestedNationalNumber { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LegalIdFileName { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string LegalIdFilePath { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(1000)]
        public string? RejectionReason { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public DateTime? ReviewedAt { get; set; }

        public string? ReviewedById { get; set; }

        [ForeignKey(nameof(ReviewedById))]
        public ApplicationUser? ReviewedBy { get; set; }
    }
}
