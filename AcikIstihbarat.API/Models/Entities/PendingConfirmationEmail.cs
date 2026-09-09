using System.ComponentModel.DataAnnotations;

namespace AcikIstihbarat.API.Models.Entities
{
    // Outbox row for a single subscribe-batch confirmation email. Written transactionally
    // alongside the MailSubscriber rows it corresponds to (Phase 2.3), and picked up/sent by the
    // existing mail scheduler background poller (Phase 2.4) rather than sent inline on the HTTP
    // request path - see Evaluation Finding #1 in
    // ProjectImplementationDocs/Implementation-Plan-AcikMedya-Newsletter-Subscription-UX.md.
    public class PendingConfirmationEmail
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public Guid ConfirmToken { get; set; }

        // Comma-joined, already display-name-mapped newsletter names for the confirmation email body.
        [Required]
        [MaxLength(1024)]
        public string TemplateDisplayNamesCsv { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SentAt { get; set; }

        public int AttemptCount { get; set; } = 0;

        [MaxLength(512)]
        public string? LastError { get; set; }
    }
}
