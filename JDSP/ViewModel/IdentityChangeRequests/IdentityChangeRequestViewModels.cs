using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModels.IdentityChangeRequests {
    public class IdentityChangeRequestInputViewModel {
        [Required, MaxLength(150)]
        public string RequestedFullName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? RequestedPhoneNumber { get; set; }

        [Required, MaxLength(20)]
        public string RequestedNationalNumber { get; set; } = string.Empty;

        public IFormFile? LegalIdFile { get; set; }
    }

    public class IdentityChangeRequestListItemViewModel {
        public int RequestId { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CurrentFullName { get; set; } = string.Empty;
        public string RequestedFullName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
    }

    public class IdentityChangeRequestDetailsViewModel {
        public int RequestId { get; set; }
        public string RequestedById { get; set; } = string.Empty;
        public string RequestedByName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string CurrentFullName { get; set; } = string.Empty;
        public string RequestedFullName { get; set; } = string.Empty;
        public string? CurrentPhoneNumber { get; set; }
        public string? RequestedPhoneNumber { get; set; }
        public string CurrentNationalNumber { get; set; } = string.Empty;
        public string RequestedNationalNumber { get; set; } = string.Empty;
        public string LegalIdFileName { get; set; } = string.Empty;
    }

    public class RejectIdentityChangeRequestViewModel {
        public int RequestId { get; set; }
        public string RequestedByName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [Required, MaxLength(1000)]
        public string RejectionReason { get; set; } = string.Empty;
    }
}
