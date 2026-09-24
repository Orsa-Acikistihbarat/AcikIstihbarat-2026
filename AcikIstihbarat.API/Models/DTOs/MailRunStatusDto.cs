namespace AcikIstihbarat.API.Models.DTOs
{
    public class MailRunStatusDto
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

        public double ProgressPercentage { get; set; }

        public static MailRunStatusDto FromStatus(MailRunStatus status) => new()
        {
            BatchId = status.BatchId,
            ActiveNewsletterKeys = status.ActiveNewsletterKeys,
            CurrentNewsletterKey = status.CurrentNewsletterKey,
            IsRunning = status.IsRunning,
            TotalRecipients = status.TotalRecipients,
            SentCount = status.SentCount,
            SuccessCount = status.SuccessCount,
            FailureCount = status.FailureCount,
            StatusMessage = status.StatusMessage,
            StartedAtUtc = status.StartedAtUtc,
            FinishedAtUtc = status.FinishedAtUtc,
            LastError = status.LastError,
            ProgressPercentage = status.ProgressPercentage
        };
    }
}
