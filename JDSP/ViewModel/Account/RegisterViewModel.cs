using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModels.Account {
    public class RegisterViewModel {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? MiddleName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string NationalNumber { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public IFormFile? NationalIdFile { get; set; }

        public IFormFile? LawyerIdFile { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}