using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models
{
    public class Case
    {
        [Key]
        public int CaseID { get; set; }
        
        [Required(ErrorMessage = "Case name is required.")]
        [MaxLength(200, ErrorMessage = "Case name cannot exceed 200 characters.")]
        public string CaseName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Case type is required.")]
        [MaxLength(100, ErrorMessage = "Case type cannot exceed 100 characters.")]
        public string CaseType { get; set; } = string.Empty;
        
        [MaxLength(50, ErrorMessage = "Case status cannot exceed 50 characters.")]
        public string Status { get; set; } = "Open";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Foreign key for ApplicationUser
        [Required(ErrorMessage = "Creator ID is required.")]
        public string CreatedBy_Id { get; set; } = String.Empty;
        
        [ForeignKey("CreatedBy_Id")]
        public ApplicationUser? Creator { get; set; }


        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
