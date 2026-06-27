namespace JDSP.ViewModel
{
    public class RespondToRequestViewModel
    {
        public int CaseLawyerID { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty; // Accept or Reject

    }
}
