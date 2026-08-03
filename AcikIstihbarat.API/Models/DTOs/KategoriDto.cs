using System.Collections.Generic;

namespace AcikIstihbarat.API.Models.DTOs
{
    public class KategoriDto
    {
        public int Id { get; set; }
        public string Ad { get; set; }
        public string Slug { get; set; }
        public int? ParentId { get; set; }
        public int Sira { get; set; }
        public List<KategoriDto> AltKategoriler { get; set; } = new List<KategoriDto>();
    }
}
