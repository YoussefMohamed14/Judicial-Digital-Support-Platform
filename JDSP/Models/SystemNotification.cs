using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models {
    public class SystemNotification {
        public int Id { get; set; }

        [Required]
        public string RecipientId { get; set; } = string.Empty;

        [ForeignKey(nameof(RecipientId))]
        public ApplicationUser? Recipient { get; set; }

        [Required, MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Body { get; set; } = string.Empty;

        [Required, MaxLength(40)]
        public string Category { get; set; } = "System";

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
