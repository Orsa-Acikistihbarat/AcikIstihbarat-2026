using System;

namespace AcikIstihbarat.API.Models.DTOs
{
    public class MedyaDto
    {
        public int Id { get; set; }
        public string DosyaAdi { get; set; }
        public string DosyaUrl { get; set; }
        public string DosyaTipi { get; set; }
        public long DosyaBoyutu { get; set; }
        public string Baslik { get; set; }
        public string AnahtarKelimeler { get; set; }
        public DateTime YuklenmeTarihi { get; set; }
    }
}
