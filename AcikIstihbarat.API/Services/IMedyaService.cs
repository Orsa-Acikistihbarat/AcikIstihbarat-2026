using AcikIstihbarat.API.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Services
{
    public interface IMedyaService
    {
        Task<MedyaDto> UploadMedyaAsync(MedyaUploadDto dto);
        Task<IEnumerable<MedyaDto>> GetAllMedyaAsync();
        Task<MedyaDto> GetMedyaByIdAsync(int id);
        Task<bool> DeleteMedyaAsync(int id);
    }
}
