namespace AcikIstihbarat.API.Models.DTOs
{
    public class SliderItemDto
    {
        public int Id { get; set; }
        public string Tip { get; set; }
        public string Baslik { get; set; }
        public string Spot { get; set; }
        public System.DateTime? Tarih { get; set; }
        public string ThumbnailUrl { get; set; }
        public string BadgeLabel { get; set; }
    }
}
