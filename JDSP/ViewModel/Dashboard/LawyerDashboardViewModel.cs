namespace JDSP.ViewModels.Dashboard {
    public class LawyerDashboardViewModel {
        public string LawyerName { get; set; } = string.Empty;
        public int Followers { get; set; }
        public int CurrentClients { get; set; }
        public int PendingRequests { get; set; }
        public int AssignedCases { get; set; }
        public decimal AvailableBalance { get; set; }
        public DateTime? NextHearingDate { get; set; }
        public DateTime? NextHearingEndDate { get; set; }
        public string HearingCountdownPhase { get; set; } = string.Empty;
        public string? NextHearingLocation { get; set; }
        public string? NextHearingCaseName { get; set; }
        public IReadOnlyList<LawyerDashboardRequestViewModel> RecentRequests { get; set; } = Array.Empty<LawyerDashboardRequestViewModel>();
    }

    public class LawyerDashboardRequestViewModel {
        public int RequestId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
