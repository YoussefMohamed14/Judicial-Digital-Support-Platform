using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModel
{
    public class ProposePriceViewModel
    {
        public int CaseLawyerId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a proposed price.")]
        [Range(1,1000000, ErrorMessage = "Proposed price must be between 1 and 1,000,000.")]
        [Display(Name = "Proposed Price")]
        public decimal ProposedPrice { get; set; }
    }
}
