using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AcikIstihbarat.API.Models.DTOs
{
    public class MedyaUploadDto
    {
        [Required]
        public IFormFile File { get; set; }

        public string Baslik { get; set; }

        public string AnahtarKelimeler { get; set; }
    }
}
