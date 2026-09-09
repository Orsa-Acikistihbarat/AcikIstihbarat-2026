using System.ComponentModel.DataAnnotations;

namespace AcikIstihbarat.API.Models.Entities
{
    public class EmailSendLog
    {
        public long Id { get; set; }

        public int SubscriberId { get; set; }

        public int? ScheduleId { get; set; }

        [Required]
        [MaxLength(255)]
        public string TemplateFileName { get; set; } = string.Empty;

        public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

        public bool Success { get; set; }

        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }
    }
}
