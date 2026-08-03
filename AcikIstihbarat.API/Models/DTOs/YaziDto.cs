namespace AcikIstihbarat.API.Models.DTOs
{
    public class YaziDto
    {
        public int Id { get; set; }
        public int YazarId { get; set; }
        public string YazarAd { get; set; }
        public string Baslik { get; set; }
        public string OnIzlemeMetni { get; set; }
        public string TamMetin { get; set; }
        public System.DateTime? Tarih { get; set; }
    }
}
