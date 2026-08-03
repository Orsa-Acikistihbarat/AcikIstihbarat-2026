using AcikIstihbarat.API.Models.DTOs;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Services
{
    public interface IAramaService
    {
        Task<AramaSonucDto> AraAsync(string q, string mode, int page, int pageSize);
    }
}
