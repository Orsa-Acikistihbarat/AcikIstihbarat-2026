using System.ComponentModel.DataAnnotations;

namespace AcikIstihbarat.API.Models.DTOs
{
    public class KategoriCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Ad { get; set; }

        public int? ParentId { get; set; }
        
        public int Sira { get; set; }
    }
}
