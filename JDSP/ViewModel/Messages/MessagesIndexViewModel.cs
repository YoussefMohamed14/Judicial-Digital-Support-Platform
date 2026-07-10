namespace JDSP.ViewModels.Messages {
    public class MessagesIndexViewModel {
        public IReadOnlyList<MessageContactViewModel> Contacts { get; set; } = Array.Empty<MessageContactViewModel>();
        public IReadOnlyList<SystemNotificationViewModel> SystemMessages { get; set; } = Array.Empty<SystemNotificationViewModel>();
        public int UnreadSystemMessages { get; set; }
        public string? SelectedContactId { get; set; }
        public MessageContactViewModel? SelectedContact { get; set; }
        public IReadOnlyList<ChatMessageViewModel> ChatMessages { get; set; } = Array.Empty<ChatMessageViewModel>();
        public string SystemAvatarPath { get; set; } = "/images/jdsp-court-system.png";
    }

    public class SystemNotificationViewModel {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
