namespace JDSP.ViewModels.CourtEmployee {
    public class CourtEmployeeDashboardViewModel {
        public int PendingLawyerApprovals { get; set; }
        public int ApprovedLawyers { get; set; }
        public int RejectedLawyers { get; set; }
        public int PendingIdentityChangeRequests { get; set; }
        public int TotalClients { get; set; }
        public int TotalCases { get; set; }
        public int PendingOfficialCaseRequests { get; set; }
        public int WaitingForHearingFollowUp { get; set; }
        public List<PendingOfficialCaseRequestItemViewModel> PendingOfficialCaseRequestsList { get; set; } = new();
        public List<PendingLawyerApprovalItemViewModel> PendingLawyers { get; set; } = new();
        public List<PendingIdentityChangeItemViewModel> PendingIdentityChanges { get; set; } = new();
    }

    public class PendingLawyerApprovalItemViewModel {
        public int RequestId { get; set; }
        public string LawyerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NationalNumber { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
    }

    public class PendingIdentityChangeItemViewModel {
        public int RequestId { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RequestedFullName { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
    }

    public class PendingOfficialCaseRequestItemViewModel {
        public int RequestId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string LawyerName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
    }

    public class LawyerApprovalDetailsViewModel {
        public int RequestId { get; set; }
        public string LawyerId { get; set; } = string.Empty;
        public string LawyerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string NationalNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string NationalIdFileName { get; set; } = string.Empty;
        public string LawyerIdFileName { get; set; } = string.Empty;
    }

    public class RejectLawyerApprovalViewModel {
        public int RequestId { get; set; }
        public string LawyerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
    }
}
