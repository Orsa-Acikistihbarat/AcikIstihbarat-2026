using AcikIstihbarat.API.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Services
{
    public interface IKategoriService
    {
        Task<IEnumerable<KategoriDto>> GetAllKategorilerAsync();
        Task<IEnumerable<KategoriDto>> GetKategorilerTreeAsync();
        Task<KategoriDto> GetKategoriByIdAsync(int id);
        Task<KategoriDto> CreateKategoriAsync(KategoriCreateDto dto);
        Task<KategoriDto> UpdateKategoriAsync(int id, KategoriCreateDto dto);
        Task<bool> DeleteKategoriAsync(int id);
    }
}
