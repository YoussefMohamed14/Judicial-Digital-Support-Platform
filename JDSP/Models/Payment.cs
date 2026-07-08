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

        [Required(ErrorMessage = "Amount is required.")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(1, 1000000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty; // VodafoneCash / Fawry / BankTransfer

        [MaxLength(50)]
        public string Status { get; set; } = "Completed"; // mock payments are always completed instantly

        [Required]
        [MaxLength(50)]
        public string TransactionRef { get; set; } = string.Empty; // mock receipt / transaction number

        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    }
}