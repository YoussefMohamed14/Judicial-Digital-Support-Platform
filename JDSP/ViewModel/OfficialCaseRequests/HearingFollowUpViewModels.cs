using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModels.OfficialCaseRequests {
    public class HearingFollowUpListItemViewModel {
        public int CaseId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string CaseType { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string LawyerName { get; set; } = string.Empty;
        public string CaseStatus { get; set; } = string.Empty;
        public DateTime? LastHearingStart { get; set; }
        public DateTime? LastHearingEnd { get; set; }
        public string? LastHearingType { get; set; }
        public string? LastHearingLocation { get; set; }
    }

    public class HearingFollowUpDecisionViewModel {
        public int CaseId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string CaseType { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string LawyerName { get; set; } = string.Empty;
        public string CaseStatus { get; set; } = string.Empty;
        public DateTime? LastHearingStart { get; set; }
        public DateTime? LastHearingEnd { get; set; }

        [Required]
        public string Decision { get; set; } = "ScheduleNext"; // ScheduleNext / Postpone / Close

        public DateTime? HearingDate { get; set; }
        public DateTime? HearingEndDate { get; set; }

        [MaxLength(20)]
        public string HearingType { get; set; } = "Physical";

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(1000)]
        public string? CourtNotes { get; set; }
    }
}
