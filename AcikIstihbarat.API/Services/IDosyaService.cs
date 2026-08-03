using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Services
{
    public interface IDosyaService
    {
        Task<(string FilePath, string FileUrl, string FileName)> SaveFileAsync(IFormFile file);
        void DeleteFile(string filePath);
    }
}
