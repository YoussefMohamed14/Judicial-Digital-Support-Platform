namespace JDSP.ViewModel
{
    public class AcceptOfferViewModel
    {
        public int CaseLawyerId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string LawyerName { get; set; } = string.Empty;
        public decimal ProposedPrice { get; set; }
    }
}
