using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AcikIstihbarat.API.Models.DTOs
{
    public class HaberCreateDto
    {
        [Required]
        [MaxLength(255)]
        public string Baslik { get; set; }

        [MaxLength(4000)]
        public string Spot { get; set; }

        public string HtmlIcerigi { get; set; }

        public int KategoriId { get; set; }

        public bool Mansetmi { get; set; }
    }

    public class HaberMedyaDto
    {
        public int MedyaId { get; set; }
        public bool OncuResimMi { get; set; }
        public int Sira { get; set; }
    }
}
