using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models {
    public class OfficialCaseRequest {
        public int Id { get; set; }

        [Required]
        public int CaseId { get; set; }

        [ForeignKey(nameof(CaseId))]
        public Case? Case { get; set; }

        [Required]
        public string LawyerId { get; set; } = string.Empty;

        [ForeignKey(nameof(LawyerId))]
        public ApplicationUser? Lawyer { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending / Approved / Rejected

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        public string? ReviewedById { get; set; }

        [ForeignKey(nameof(ReviewedById))]
        public ApplicationUser? ReviewedBy { get; set; }

        [MaxLength(1000)]
        public string? RejectionReason { get; set; }

        public DateTime? HearingDate { get; set; }

        public DateTime? HearingEndDate { get; set; }

        [MaxLength(20)]
        public string? HearingType { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(1000)]
        public string? CourtNotes { get; set; }
    }
}
