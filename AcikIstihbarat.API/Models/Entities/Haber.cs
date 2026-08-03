using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcikIstihbarat.API.Models.Entities
{
    [Table("Haber")]
    public class Haber
    {
        [Column("HaberId")]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("HaberBaslik")]
        public string Baslik { get; set; }

        [Column("HaberOnIzlemeMetni")]
        public string Spot { get; set; }

        [Column("HaberTamMetin")]
        public string HtmlIcerigi { get; set; }

        [Column("HaberHaberTuruId")]
        public int KategoriId { get; set; }

        [Column("HaberMansetMi")]
        public bool Mansetmi { get; set; }

        [Column("HaberTarih")]
        public DateTime Tarih { get; set; }

        [Column("HaberResimAdresi")]
        [MaxLength(255)]
        public string LegacyResimAdresi { get; set; }

        [Column("HaberSilindiMi")]
        public bool SilindiMi { get; set; }

        [Column("HaberKullaniciId")]
        public int KullaniciId { get; set; }

        [Column("HaberSaat")]
        [MaxLength(6)]
        public string Saat { get; set; }

        [Column("HaberYorumaAcikMi")]
        public bool YorumaAcikMi { get; set; }

        [Column("HaberOkunma")]
        public int Okunma { get; set; }

        [Column("HaberOnay")]
        public bool Onay { get; set; }

        public HaberKategori Kategori { get; set; }
        public ICollection<HaberMedya> Medyalar { get; set; }
    }
}
