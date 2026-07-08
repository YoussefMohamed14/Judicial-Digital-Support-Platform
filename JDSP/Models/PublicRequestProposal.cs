using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models {
    public class PublicRequestProposal {
        public int PublicRequestProposalId { get; set; }

        [Required]
        public int LegalServiceRequestId { get; set; }

        [ForeignKey(nameof(LegalServiceRequestId))]
        public LegalServiceRequest Request { get; set; } = null!;

        [Required]
        public string LawyerId { get; set; } = string.Empty;

        [ForeignKey(nameof(LawyerId))]
        public ApplicationUser Lawyer { get; set; } = null!;

        [Required, MaxLength(1500)]
        public string Message { get; set; } = string.Empty;

        [Range(0, 1000000)]
        public decimal? ProposedPrice { get; set; }

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
