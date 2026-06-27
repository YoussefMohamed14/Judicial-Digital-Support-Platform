namespace JDSP.ViewModel
{
    public class MyRequestStatusViewModel
    {
        public int CaseLawyerId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string LawyerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal? ProposedPrice { get; set; }
    }
}
