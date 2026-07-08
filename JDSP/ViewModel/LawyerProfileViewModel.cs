using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModels.Lawyers {
    public class LawyerProfileViewModel {
        public int LawyerProfileId { get; set; }

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

        public bool IsAvailable { get; set; } = true;
    }
}