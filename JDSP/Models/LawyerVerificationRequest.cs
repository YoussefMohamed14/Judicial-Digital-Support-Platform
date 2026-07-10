using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models {
    public class LawyerVerificationRequest {
        public int Id { get; set; }

        [Required]
        public string LawyerId { get; set; } = string.Empty;

        [ForeignKey(nameof(LawyerId))]
        public ApplicationUser? Lawyer { get; set; }

        [Required, MaxLength(100)]
        public string NationalIdFileName { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string NationalIdFilePath { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LawyerIdFileName { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string LawyerIdFilePath { get; set; } = string.Empty;

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
