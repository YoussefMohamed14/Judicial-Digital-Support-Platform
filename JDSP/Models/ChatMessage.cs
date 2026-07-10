using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models {
    public class ChatMessage {
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [ForeignKey(nameof(SenderId))]
        public ApplicationUser? Sender { get; set; }

        [Required]
        public string ReceiverId { get; set; } = string.Empty;

        [ForeignKey(nameof(ReceiverId))]
        public ApplicationUser? Receiver { get; set; }

        [Required, MaxLength(2000)]
        public string Body { get; set; } = string.Empty;

        [Required, MaxLength(40)]
        public string MessageType { get; set; } = "User"; // User / PaymentRequest / System

        public int? RelatedCaseId { get; set; }

        [ForeignKey(nameof(RelatedCaseId))]
        public Case? RelatedCase { get; set; }

        public int? PaymentId { get; set; }

        [ForeignKey(nameof(PaymentId))]
        public Payment? Payment { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
