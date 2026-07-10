using System.ComponentModel.DataAnnotations;

namespace JDSP.Models {
    public class LawyerProfile {
        public int LawyerProfileId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Bio { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Specialization { get; set; } = string.Empty;

        [Range(0, 60)]
        public int YearsOfExperience { get; set; }

        [Range(0, 100000)]
        public decimal ConsultationPrice { get; set; }

        [Required]
        [MaxLength(20)]
        public string ConsultationPriceUnit { get; set; } = "Hour";

        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}