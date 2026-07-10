using System.ComponentModel.DataAnnotations;

namespace JDSP.ViewModel.LawyerBalance {
    public class LawyerBalanceViewModel {
        public decimal AvailableBalance { get; set; }
        public decimal WithdrawnBalance { get; set; }
        public decimal TotalPaid { get; set; }
        public int AvailablePaymentCount { get; set; }
        public IReadOnlyList<LawyerBalancePaymentViewModel> Payments { get; set; } = Array.Empty<LawyerBalancePaymentViewModel>();
        public WithdrawBalanceViewModel Withdraw { get; set; } = new();
    }

    public class LawyerBalancePaymentViewModel {
        public int PaymentId { get; set; }
        public int CaseId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal WithdrawnAmount { get; set; }
        public decimal AvailableAmount { get; set; }
        public string BillingType { get; set; } = "OneTime";
        public DateTime PaidAt { get; set; }
        public string LawyerPayoutStatus { get; set; } = "Available";
        public DateTime? LawyerPayoutCompletedAt { get; set; }
        public string? LawyerPayoutCardLast4 { get; set; }
        public string? LawyerPayoutReference { get; set; }
    }

    public class WithdrawBalanceViewModel {
        [Required]
        [Range(1, 1000000)]
        public decimal Amount { get; set; }

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
