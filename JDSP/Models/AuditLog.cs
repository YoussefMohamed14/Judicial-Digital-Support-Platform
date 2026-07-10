using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(20)]
        public string ActionType { get; set; } = string.Empty; // Create / Update / Delete / View

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; } = string.Empty; // e.g. "Document", "Hearing", "Payment"

        [MaxLength(50)]
        public string? EntityId { get; set; }

        [MaxLength(500)]
        public string? Details { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}