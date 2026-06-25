using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models
{
    public class CaseLawyer
    {
        [Key]
        public int CaseLawyerId { get; set; }
        [MaxLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; } = DateTime.Now;

        // Foreign key for Case
        [Required(ErrorMessage = "Case ID is required.")]
        public int CaseId { get; set; }
        [ForeignKey("CaseId")]
        public virtual Case? Case { get; set; }

        // Foreign key for ApplicationUser (Lawyer)
        public string LawyerId { get; set; } = string.Empty;
        [ForeignKey("LawyerId")]
        public virtual ApplicationUser? Lawyer { get; set; }

        public decimal? ProposedPrice { get; set; }
    }
}
