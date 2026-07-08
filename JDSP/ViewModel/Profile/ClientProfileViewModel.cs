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
    }
}
