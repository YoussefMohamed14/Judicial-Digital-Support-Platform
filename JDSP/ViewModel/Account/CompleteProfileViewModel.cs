using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace JDSP.ViewModels.Account {
    public class CompleteProfileViewModel {
        [Required(ErrorMessage = "Please choose a profile photo.")]
        public IFormFile? Photo { get; set; }
        [Required, StringLength(1000, MinimumLength = 10)]
        public string Bio { get; set; } = string.Empty;
    }
}
