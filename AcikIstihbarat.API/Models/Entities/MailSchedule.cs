using System.ComponentModel.DataAnnotations;

namespace AcikIstihbarat.API.Models.Entities
{
    public class MailSchedule
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string TemplateBaseName { get; set; } = string.Empty;

        public MailFrequencyType FrequencyType { get; set; } = MailFrequencyType.Daily;

        public int? IntervalValue { get; set; }

        public int? DaysOfWeekMask { get; set; }

        public TimeSpan TimeOfDayLocal { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastRunAtUtc { get; set; }

        public DateTime NextRunAtUtc { get; set; }
    }
}
