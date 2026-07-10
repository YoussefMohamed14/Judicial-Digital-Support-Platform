namespace JDSP.ViewModels.Dashboard {
    public class ClientDashboardViewModel {
        public string ClientName { get; set; } = string.Empty;
        public int TotalCases { get; set; }
        public int ActiveCases { get; set; }
        public int FollowedLawyers { get; set; }
        public int AssignedLawyers { get; set; }
        public int PendingRequests { get; set; }
        public int PublicRequests { get; set; }
        public DateTime? NextHearingDate { get; set; }
        public DateTime? NextHearingEndDate { get; set; }
        public string HearingCountdownPhase { get; set; } = string.Empty;
        public string? NextHearingLocation { get; set; }
        public string? NextHearingCaseName { get; set; }
        public IReadOnlyList<ClientDashboardCaseViewModel> RecentCases { get; set; } = Array.Empty<ClientDashboardCaseViewModel>();
    }
    public class ClientDashboardCaseViewModel {
        public int CaseId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string CaseType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
