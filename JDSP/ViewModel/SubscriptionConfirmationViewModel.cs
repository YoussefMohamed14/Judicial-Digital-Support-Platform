namespace JDSP.ViewModel
{
    public class SubscriptionConfirmationViewModel
    {
        public int SubscriptionId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string LawyerName { get; set; } = string.Empty;
        public decimal Price { get; set; } 
        public string BillingCycle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
