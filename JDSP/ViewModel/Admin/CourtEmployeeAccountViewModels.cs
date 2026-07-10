using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModels.Admin {
    public class CourtEmployeeAccountListViewModel {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string NationalNumber { get; set; } = string.Empty;
        public string AccountStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CourtEmployeeAccountDetailsViewModel : CourtEmployeeAccountListViewModel {
        public bool MustChangePassword { get; set; }
        public string PreferredLanguage { get; set; } = "en";
    }

    public class CreateCourtEmployeeViewModel {
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? MiddleName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string NationalNumber { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(6)]
        public string TemporaryPassword { get; set; } = string.Empty;
    }
}
