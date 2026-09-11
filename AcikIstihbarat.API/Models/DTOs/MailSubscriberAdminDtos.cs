namespace AcikIstihbarat.API.Models.DTOs
{
    public class MailSubscriberAdminItem
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string NewsletterDisplayName { get; set; } = string.Empty;
        public DateTime? SubscriptionDate { get; set; }
        public DateTime? UnsubscriptionDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class MailSubscriberSummaryItem
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int SubscribedCount { get; set; }
        public int UnsubscribedCount { get; set; }
    }
}
