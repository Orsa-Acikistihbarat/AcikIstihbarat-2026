using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AcikIstihbarat.API.Models.Entities
{
    public class Medya
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string DosyaAdi { get; set; }

        [Required]
        [MaxLength(1000)]
        public string DosyaYolu { get; set; }

        [Required]
        [MaxLength(1000)]
        public string DosyaUrl { get; set; }

        [Required]
        [MaxLength(100)]
        public string DosyaTipi { get; set; }

        public long DosyaBoyutu { get; set; }

        [MaxLength(255)]
        public string Baslik { get; set; }

        [MaxLength(500)]
        public string AnahtarKelimeler { get; set; }

        public DateTime YuklenmeTarihi { get; set; }

        public ICollection<HaberMedya> Haberler { get; set; }
    }
}
