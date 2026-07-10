using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModels.Account {
    public class ChangeTemporaryPasswordViewModel {
        [Required, DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
