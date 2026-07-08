using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models
{
    public class CaseLawyerSubscription
    {
        [Key]
        public int CaseLawyerSubscriptionId { get; set; }
        [Required]
        public int CaseLawyerId { get; set; }
        [ForeignKey("CaseLawyerId")]
        public virtual CaseLawyer? Caselawyer { get; set; }
        [MaxLength(50)]
        public string Status { get; set; } = "Active";
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        [MaxLength(50)]
        public string BillingCycle { get; set; } = "Monthly";
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; }
    }
}
