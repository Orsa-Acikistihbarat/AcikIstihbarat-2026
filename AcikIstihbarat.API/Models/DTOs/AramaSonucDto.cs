namespace AcikIstihbarat.API.Models.DTOs
{
    public class AramaSonucDto
    {
        public PagedResult<HaberListDto> Haberler { get; set; }
        public PagedResult<SliderItemDto> Yazilar { get; set; }
    }
}
