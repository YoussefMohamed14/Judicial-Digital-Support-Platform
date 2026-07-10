using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModels.Cases {
    public class MockPaymentViewModel {
        public int CaseId { get; set; }

        public int? PaymentId { get; set; }
        public bool IsEdit { get; set; }

        public string CaseName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;

        [Required]
        [Range(1, 1000000)]
        public decimal Amount { get; set; }

        [Required, MaxLength(50)]
        public string BillingType { get; set; } = "OneTime";

        [MaxLength(1000)]
        public string? Note { get; set; }
    }

    public class CasePaymentListItemViewModel {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string BillingType { get; set; } = "OneTime";
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TransactionRef { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime PaidAt { get; set; }
    }

    public class ClientPaymentViewModel {
        public int PaymentId { get; set; }
        public int CaseId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string LawyerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BillingType { get; set; } = "OneTime";
        public string? Note { get; set; }
        public string Status { get; set; } = string.Empty;
        public string TransactionRef { get; set; } = string.Empty;

        [Required, MaxLength(80)]
        public string CardHolderName { get; set; } = string.Empty;

        [Required, CreditCard]
        public string CardNumber { get; set; } = string.Empty;

        [Required, MaxLength(5)]
        public string Expiry { get; set; } = string.Empty;

        [Required, MinLength(3), MaxLength(4)]
        public string Cvv { get; set; } = string.Empty;
    }
}
