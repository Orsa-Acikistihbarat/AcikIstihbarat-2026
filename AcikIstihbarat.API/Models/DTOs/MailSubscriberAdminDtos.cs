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
}
