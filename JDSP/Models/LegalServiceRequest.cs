using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models {
    public class LegalServiceRequest {
        public int LegalServiceRequestId { get; set; }

        [Required, MaxLength(180)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(3000)]
        public string Brief { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string RequestType { get; set; } = "Direct"; // Direct / Public

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending"; // Pending / Accepted / Rejected / Closed

        [MaxLength(255)]
        public string? OriginalFileName { get; set; }

        [MaxLength(500)]
        public string? FilePath { get; set; }

        [Required]
        public string ClientId { get; set; } = string.Empty;

        [ForeignKey(nameof(ClientId))]
        public ApplicationUser Client { get; set; } = null!;

        public string? LawyerId { get; set; }

        [ForeignKey(nameof(LawyerId))]
        public ApplicationUser? Lawyer { get; set; }

        public int? CaseId { get; set; }

        [ForeignKey(nameof(CaseId))]
        public Case? Case { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }

        public ICollection<PublicRequestProposal> Proposals { get; set; } = new List<PublicRequestProposal>();
    }
}
