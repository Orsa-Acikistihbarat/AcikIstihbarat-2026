using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Services
{
    public class DosyaService : IDosyaService
    {
        private readonly string _storagePath;

        public DosyaService(IConfiguration configuration)
        {
            _storagePath = configuration["FileStorage:MedyaKutuphanesiPath"] 
                           ?? throw new ArgumentNullException("FileStorage:MedyaKutuphanesiPath is missing in configuration.");
            
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        public async Task<(string FilePath, string FileUrl, string FileName)> SaveFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty.");

            // Anti-path traversal check
            var originalFileName = Path.GetFileName(file.FileName); 
            
            var extension = Path.GetExtension(originalFileName);
            var newFileName = $"{Guid.NewGuid()}{extension}";
            
            var fullPath = Path.Combine(_storagePath, newFileName);
            
            // Validate the resolved path is within the allowed directory
            var fullStoragePath = Path.GetFullPath(_storagePath);
            var resolvedFullPath = Path.GetFullPath(fullPath);
            
            if (!resolvedFullPath.StartsWith(fullStoragePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid file path detected.");
            }

            using (var stream = new FileStream(resolvedFullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // In a real production app, FileUrl might point to a CDN or a separate file server endpoint
            var fileUrl = $"/MedyaKutuphanesi/{newFileName}";

            return (resolvedFullPath, fileUrl, newFileName);
        }

        public void DeleteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
