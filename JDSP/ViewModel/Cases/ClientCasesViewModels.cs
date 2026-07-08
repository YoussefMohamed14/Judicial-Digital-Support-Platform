namespace JDSP.ViewModels.Cases {
    public class MyCasesViewModel {
        public string? StatusFilter { get; set; }
        public IReadOnlyList<ClientCaseListItemViewModel> Cases { get; set; } = Array.Empty<ClientCaseListItemViewModel>();
    }
    public class ClientCaseListItemViewModel {
        public int CaseId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string CaseType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? AssignedLawyerName { get; set; }
        public DateTime? NextHearingDate { get; set; }
    }
    public class ClientCaseDetailsViewModel : ClientCaseListItemViewModel {
        public string? LawyerRequestStatus { get; set; }
        public string? NextHearingLocation { get; set; }
        public string? NextHearingType { get; set; }
    }
}

namespace JDSP.ViewModels.Cases {
    public class LawyerAssignedCasesViewModel {
        public string? StatusFilter { get; set; }
        public IReadOnlyList<LawyerAssignedCaseListItemViewModel> Cases { get; set; } = Array.Empty<LawyerAssignedCaseListItemViewModel>();
    }

    public class LawyerAssignedCaseListItemViewModel {
        public int CaseId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string CaseType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public DateTime? NextHearingDate { get; set; }
        public string? NextHearingLocation { get; set; }
    }
}
