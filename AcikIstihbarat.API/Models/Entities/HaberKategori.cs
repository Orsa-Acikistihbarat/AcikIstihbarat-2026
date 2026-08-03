using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AcikIstihbarat.API.Models.Entities
{
    public class HaberKategori
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Ad { get; set; }

        [Required]
        [MaxLength(100)]
        public string Slug { get; set; }

        public int? ParentId { get; set; }
        
        public int Sira { get; set; }

        public HaberKategori ParentKategori { get; set; }
        public ICollection<HaberKategori> AltKategoriler { get; set; }
        public ICollection<Haber> Haberler { get; set; }
    }
}
