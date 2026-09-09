namespace AcikIstihbarat.API.Models.DTOs
{
    public class MailOptions
    {
        public string TemplatesDataDir { get; set; } = string.Empty;
        public string PublicApiBaseUrl { get; set; } = string.Empty;
        public string PublicSiteBaseUrl { get; set; } = string.Empty;
        public int PollIntervalSeconds { get; set; } = 120;
        public bool DryRun { get; set; } = false;
        public GmailOAuthOptions GmailOAuth { get; set; } = new();
        public MailGuardrailOptions Guardrails { get; set; } = new();
    }

    public class GmailOAuthOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string SenderAddress { get; set; } = string.Empty;
    }

    public class MailGuardrailOptions
    {
        public int BatchSize { get; set; } = 25;
        public int MinDelayMs { get; set; } = 1000;
        public int MaxDelayMs { get; set; } = 5000;
        public int InterBatchDelayMs { get; set; } = 30000;
        public int MaxConsecutiveFailures { get; set; } = 3;
        public int? MaxSendsPerRun { get; set; } = null;
    }
}
