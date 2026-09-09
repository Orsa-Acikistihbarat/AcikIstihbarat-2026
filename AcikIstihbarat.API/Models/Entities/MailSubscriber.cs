using System.ComponentModel.DataAnnotations;

namespace AcikIstihbarat.API.Models.Entities
{
    public class MailSubscriber
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string TemplateBaseName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime? LastSentAt { get; set; }

        [MaxLength(32)]
        public string? LastSendStatus { get; set; }

        public int ConsecutiveFailureCount { get; set; } = 0;

        [Required]
        public Guid UnsubscribeToken { get; set; } = Guid.NewGuid();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Set explicitly by the subscribe controller action on row creation/reuse - deliberately
        // no entity-level default, so a bug that forgets to set these surfaces immediately rather
        // than being masked by a Guid.Empty/default(DateTime) fallback.
        public Guid ConfirmToken { get; set; }
        public DateTime ConfirmTokenExpiresAt { get; set; }
    }
}
