using System;
using System.Collections.Generic;

namespace AcikIstihbarat.API.Models.DTOs
{
    public class HaberDto
    {
        public int Id { get; set; }
        public string Baslik { get; set; }
        public string Spot { get; set; }
        public string HtmlIcerigi { get; set; }
        public int KategoriId { get; set; }
        public KategoriDto Kategori { get; set; }
        public bool Mansetmi { get; set; }
        public DateTime Tarih { get; set; }
        public string LegacyResimAdresi { get; set; }
        public List<MedyaDto> Gorseller { get; set; } = new List<MedyaDto>();
        public List<MedyaDto> Belgeler { get; set; } = new List<MedyaDto>();
    }

    public class HaberListDto
    {
        public int Id { get; set; }
        public string Baslik { get; set; }
        public string Spot { get; set; }
        public DateTime Tarih { get; set; }
        public string ThumbnailUrl { get; set; }
        public int KategoriId { get; set; }
        public string KategoriAd { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
