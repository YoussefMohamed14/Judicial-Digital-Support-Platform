namespace JDSP.ViewModels.Messages {
    public class MessageContactViewModel {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhotoPath { get; set; }
        public string Relationship { get; set; } = string.Empty;
        public int UnreadCount { get; set; }
    }
}
