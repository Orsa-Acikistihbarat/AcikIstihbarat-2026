using System.ComponentModel.DataAnnotations;

namespace AcikIstihbarat.API.Models.DTOs
{
    public class TriggerMailRequest
    {
        [Required(ErrorMessage = "En az bir bülten seçilmelidir.")]
        [MinLength(1, ErrorMessage = "En az bir bülten seçilmelidir.")]
        public List<string> NewsletterKeys { get; set; } = new();

        public bool ForceResend { get; set; } = false;
    }
}
