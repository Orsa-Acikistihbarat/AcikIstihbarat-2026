using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcikIstihbarat.API.Models.Entities
{
    [Table("Yazi")]
    public class Yazi
    {
        [Key]
        [Column("YaziId")]
        public int YaziId { get; set; }

        [Column("YaziYazarId")]
        public int YazarId { get; set; }

        [Column("YaziBaslik")]
        public string Baslik { get; set; }

        [Column("YaziOnIzlemeMetni")]
        public string OnIzlemeMetni { get; set; }

        [Column("YaziTamMetin")]
        public string TamMetin { get; set; }

        [Column("YaziTarih")]
        public DateTime? Tarih { get; set; }

        [Column("YaziOkunma")]
        public short Okunma { get; set; }

        [Column("YaziYorumaAcikMi")]
        public bool YorumaAcikMi { get; set; }

        [Column("YaziOnay")]
        public bool Onay { get; set; }

        public Yazar Yazar { get; set; }
    }
}
