using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Case is required.")]
        public int CaseId { get; set; }

        [ForeignKey(nameof(CaseId))]
        public Case? Case { get; set; }

        [Required]
        public string PaidById { get; set; } = string.Empty;

        [ForeignKey(nameof(PaidById))]
        public ApplicationUser? PaidBy { get; set; }

        public string? RequestedByLawyerId { get; set; }

        [ForeignKey(nameof(RequestedByLawyerId))]
        public ApplicationUser? RequestedByLawyer { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(1, 1000000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string BillingType { get; set; } = "OneTime"; // OneTime / Monthly / Hourly

        [Required(ErrorMessage = "Payment method is required.")]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Pending"; // Pending until the client pays, then Credit Card in the mock flow

        [MaxLength(50)]
        public string Status { get; set; } = "Requested"; // Requested / Paid / Declined

        [Required]
        [MaxLength(50)]
        public string TransactionRef { get; set; } = string.Empty; // mock receipt / transaction number

        [MaxLength(1000)]
        public string? Note { get; set; }

        [MaxLength(1000)]
        public string? DeclineReason { get; set; }

        [Required]
        [MaxLength(30)]
        public string LawyerPayoutStatus { get; set; } = "Available"; // Available / Withdrawn

        public DateTime? LawyerPayoutRequestedAt { get; set; }
        public DateTime? LawyerPayoutCompletedAt { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal LawyerWithdrawnAmount { get; set; }

        [MaxLength(4)]
        public string? LawyerPayoutCardLast4 { get; set; }

        [MaxLength(50)]
        public string? LawyerPayoutReference { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    }
}
