namespace AcikIstihbarat.API.Models.DTOs
{
    public class MailRunStatus
    {
        public string BatchId { get; set; } = string.Empty;

        public List<string> ActiveNewsletterKeys { get; set; } = new();

        public string? CurrentNewsletterKey { get; set; }

        public bool IsRunning { get; set; }

        public int TotalRecipients { get; set; }

        public int SentCount { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public string? StatusMessage { get; set; }

        public DateTime? StartedAtUtc { get; set; }

        public DateTime? FinishedAtUtc { get; set; }

        public string? LastError { get; set; }

        public double ProgressPercentage => TotalRecipients > 0
            ? Math.Round((double)SentCount / TotalRecipients * 100, 1)
            : 0;
    }
}
