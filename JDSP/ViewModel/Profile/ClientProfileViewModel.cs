using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace JDSP.ViewModels.Profile {
    public class ClientProfileViewModel {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string NationalNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? PhotoPath { get; set; }
        [Required, StringLength(1000, MinimumLength = 10)] public string Bio { get; set; } = string.Empty;
        public IFormFile? NewPhoto { get; set; }

        public bool HasPendingIdentityChangeRequest { get; set; }
        public DateTime? PendingIdentityChangeRequestedAt { get; set; }
        public string RequestedFullName { get; set; } = string.Empty;
        public string? RequestedPhoneNumber { get; set; }
        public string RequestedNationalNumber { get; set; } = string.Empty;
        public IFormFile? LegalIdFile { get; set; }

        public bool IsLawyer { get; set; }
        public bool ProfessionalProfileRequired { get; set; }
        public int? LawyerProfileId { get; set; }
        [StringLength(1000)] public string ProfessionalBio { get; set; } = string.Empty;
        [MaxLength(100)] public string Specialization { get; set; } = string.Empty;
        [MaxLength(100)] public string? CustomSpecialization { get; set; }
        [Range(0, 60)] public int YearsOfExperience { get; set; }
        [Range(1, 100000)] public decimal ConsultationPrice { get; set; }
        public string ConsultationPriceUnit { get; set; } = "Hour";
        public bool IsAvailable { get; set; } = true;
    }
}
