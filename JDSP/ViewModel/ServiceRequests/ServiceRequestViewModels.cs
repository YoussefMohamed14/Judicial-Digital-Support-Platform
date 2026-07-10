using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModels.ServiceRequests {
    public class CreateServiceRequestViewModel {
        public int? LawyerProfileId { get; set; }
        public string? LawyerId { get; set; }
        public string? LawyerName { get; set; }
        public int? CaseId { get; set; }
        public string? CaseName { get; set; }

        [Required, StringLength(180, MinimumLength = 5)]
        [Display(Name = "Request subject")]
        public string Subject { get; set; } = string.Empty;

        [Required, StringLength(3000, MinimumLength = 20)]
        [Display(Name = "Case brief")]
        public string Brief { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Case file")]
        public IFormFile? CaseFile { get; set; }
    }

    public class ServiceRequestListItemViewModel {
        public int RequestId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Brief { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? LawyerName { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? OriginalFileName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ProposalCount { get; set; }
        public int? CaseId { get; set; }
        public string? CaseName { get; set; }
        public bool HasProposed { get; set; }
    }

    public class PublicRequestProposalViewModel {
        public int RequestId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;

        [Required, StringLength(1500, MinimumLength = 10)]
        [Display(Name = "Proposal message")]
        public string Message { get; set; } = string.Empty;

        [Range(0, 1000000)]
        [Display(Name = "Proposed price")]
        public decimal? ProposedPrice { get; set; }
    }

    public class ClientProposalListItemViewModel {
        public int ProposalId { get; set; }
        public string LawyerName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal? ProposedPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ClientRequestDetailsViewModel {
        public ServiceRequestListItemViewModel Request { get; set; } = new();
        public IReadOnlyList<ClientProposalListItemViewModel> Proposals { get; set; } = Array.Empty<ClientProposalListItemViewModel>();
    }
}
