using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModel {
    public class CreateCaseViewModel {
        [Required(ErrorMessage = "Select the client who owns this case.")]
        [Display(Name = "Client")]
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Case name is required.")]
        [MaxLength(200, ErrorMessage = "Case name cannot exceed 200 characters.")]
        [Display(Name = "Case Name")]
        public string CaseName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Case type is required.")]
        [Display(Name = "Case Type")]
        public string CaseType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;
    }
}
