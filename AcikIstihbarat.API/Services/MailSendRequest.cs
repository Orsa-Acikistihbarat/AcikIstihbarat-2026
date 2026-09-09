namespace AcikIstihbarat.API.Services
{
    public class MailSendRequest
    {
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public Guid UnsubscribeToken { get; set; }
    }
}
