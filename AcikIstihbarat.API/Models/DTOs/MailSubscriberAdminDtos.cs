namespace AcikIstihbarat.API.Models.DTOs
{
    public class MailSubscriberAdminItem
    {
        public string Email { get; set; } = string.Empty;
        public string NewsletterDisplayName { get; set; } = string.Empty;
        public DateTime? SubscriptionDate { get; set; }
        public DateTime? UnsubscriptionDate { get; set; }
    }
}
