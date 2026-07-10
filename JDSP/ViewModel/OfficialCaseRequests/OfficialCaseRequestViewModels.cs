using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModels.OfficialCaseRequests {
    public class OfficialCaseRequestCreateViewModel {
        public int CaseId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
    }

    public class OfficialCaseRequestListItemViewModel {
        public int RequestId { get; set; }
        public int CaseId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string LawyerName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
    }

    public class OfficialCaseRequestDetailsViewModel : OfficialCaseRequestListItemViewModel {
        public string Reason { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? HearingDate { get; set; }
        public DateTime? HearingEndDate { get; set; }
        public string? HearingType { get; set; }
        public string? Location { get; set; }
        public string? CourtNotes { get; set; }
    }

    public class OfficialCaseRequestApproveViewModel {
        public int RequestId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string LawyerName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;

        [Required]
        public DateTime HearingDate { get; set; }

        [Required]
        public DateTime HearingEndDate { get; set; }

        [Required, MaxLength(20)]
        public string HearingType { get; set; } = "Physical";

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(1000)]
        public string? CourtNotes { get; set; }
    }

    public class OfficialCaseRequestRejectViewModel {
        public int RequestId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string LawyerName { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string RejectionReason { get; set; } = string.Empty;
    }
}
