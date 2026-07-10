using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models
{
    public class Hearing
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Case is required.")]
        public int CaseId { get; set; }

        [ForeignKey(nameof(CaseId))]
        public Case? Case { get; set; }

        [Required(ErrorMessage = "Hearing date is required.")]
        public DateTime HearingDate { get; set; }

        [Required(ErrorMessage = "Hearing end date is required.")]
        public DateTime EndDate { get; set; }

        public DateTime? CourtFollowUpNotifiedAt { get; set; }

        [Required(ErrorMessage = "Hearing type is required.")]
        [MaxLength(20)]
        public string HearingType { get; set; } = "Physical"; // Online / Physical

        [MaxLength(200)]
        public string? Location { get; set; } // room name, or meeting link if online

        [MaxLength(50)]
        public string Status { get; set; } = "Scheduled"; // Scheduled / Completed / Cancelled

        [Required]
        public string ScheduledById { get; set; } = string.Empty;

        [ForeignKey(nameof(ScheduledById))]
        public ApplicationUser? ScheduledBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}