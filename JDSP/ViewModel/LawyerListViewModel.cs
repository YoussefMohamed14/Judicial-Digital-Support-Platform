//it has view model only because they are also ApplicationUsers

namespace JDSP.ViewModel
{
    public class LawyerListViewModel
    {
        public string LawyerId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CaseId { get; set; }
    }
}
