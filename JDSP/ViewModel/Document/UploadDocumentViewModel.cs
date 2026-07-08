using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModel.Document
{
    public class UploadDocumentViewModel
    {
        public int CaseId { get; set; }

        [Required]
        public IFormFile File { get; set; } = default!;
    }
}
