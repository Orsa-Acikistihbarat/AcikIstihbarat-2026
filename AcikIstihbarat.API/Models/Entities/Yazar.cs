using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcikIstihbarat.API.Models.Entities
{
    [Table("Yazarlar")]
    public class Yazar
    {
        [Key]
        public int YazarID { get; set; }

        [Required]
        [MaxLength(255)]
        public string YazarAdi { get; set; } = string.Empty;

        public ICollection<Yazi> Yazilar { get; set; }
    }
}
