using AcikIstihbarat.API.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Services
{
    public interface IHaberService
    {
        Task<PagedResult<HaberListDto>> GetHaberlerAsync(int page, int pageSize);
        Task<IEnumerable<SliderItemDto>> GetMansetlerAsync();
        Task<PagedResult<HaberListDto>> GetHaberlerByKategoriAsync(int kategoriId, int page, int pageSize);
        Task<HaberDto> GetHaberByIdAsync(int id);
        Task<HaberDto> CreateHaberAsync(HaberCreateDto dto);
        Task<HaberDto> UpdateHaberAsync(int id, HaberCreateDto dto);
        Task<bool> DeleteHaberAsync(int id);
        Task<bool> SetMedyalarAsync(int id, List<HaberMedyaDto> medyalar);
        Task<int> CleanupHtmlInSpotAsync();
    }
}
