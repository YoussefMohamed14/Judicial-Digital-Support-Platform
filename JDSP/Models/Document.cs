using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JDSP.Models
{
    public class Document
    {
        public int Id { get; set; }
        [Required]
        public string FileName { get; set; } = string.Empty;
        [Required]
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        public int CaseId { get; set; }

        [ForeignKey(nameof(CaseId))]
        public Case? Case { get; set; }
        
        public string UploadedById { get; set; } = string.Empty;

        [ForeignKey(nameof(UploadedById))]
        public ApplicationUser? UploadedBy { get; set; }


    }
}
