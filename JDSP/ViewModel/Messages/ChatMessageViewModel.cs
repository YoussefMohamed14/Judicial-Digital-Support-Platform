namespace JDSP.ViewModels.Messages {
    public class ChatMessageViewModel {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string MessageType { get; set; } = "User";
        public int? RelatedCaseId { get; set; }
        public int? PaymentId { get; set; }
        public PaymentMessageViewModel? Payment { get; set; }
        public bool IsMine { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PaymentMessageViewModel {
        public int PaymentId { get; set; }
        public int CaseId { get; set; }
        public string CaseName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BillingType { get; set; } = "OneTime";
        public string Status { get; set; } = string.Empty;
        public string TransactionRef { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string? DeclineReason { get; set; }
        public string? RequestedByLawyerId { get; set; }
    }
}
