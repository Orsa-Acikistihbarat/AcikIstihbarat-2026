using AcikIstihbarat.API.Data;
using AcikIstihbarat.API.Models.DTOs;
using AcikIstihbarat.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Services
{
    public class MedyaService : IMedyaService
    {
        private readonly AppDbContext _context;
        private readonly IDosyaService _dosyaService;

        public MedyaService(AppDbContext context, IDosyaService dosyaService)
        {
            _context = context;
            _dosyaService = dosyaService;
        }

        public async Task<MedyaDto> UploadMedyaAsync(MedyaUploadDto dto)
        {
            var savedFile = await _dosyaService.SaveFileAsync(dto.File);

            var medya = new Medya
            {
                DosyaAdi = savedFile.FileName,
                DosyaYolu = savedFile.FilePath,
                DosyaUrl = savedFile.FileUrl,
                DosyaTipi = dto.File.ContentType,
                DosyaBoyutu = dto.File.Length,
                Baslik = dto.Baslik ?? dto.File.FileName,
                AnahtarKelimeler = dto.AnahtarKelimeler ?? "",
                YuklenmeTarihi = DateTime.UtcNow
            };

            _context.MedyaKutuphanesi.Add(medya);
            await _context.SaveChangesAsync();

            return MapToDto(medya);
        }

        public async Task<IEnumerable<MedyaDto>> GetAllMedyaAsync()
        {
            var medyalar = await _context.MedyaKutuphanesi.OrderByDescending(m => m.YuklenmeTarihi).ToListAsync();
            return medyalar.Select(MapToDto);
        }

        public async Task<MedyaDto> GetMedyaByIdAsync(int id)
        {
            var medya = await _context.MedyaKutuphanesi.FindAsync(id);
            if (medya == null) return null;

            return MapToDto(medya);
        }

        public async Task<bool> DeleteMedyaAsync(int id)
        {
            var medya = await _context.MedyaKutuphanesi.FindAsync(id);
            if (medya == null) return false;

            _dosyaService.DeleteFile(medya.DosyaYolu);

            _context.MedyaKutuphanesi.Remove(medya);
            await _context.SaveChangesAsync();

            return true;
        }

        private MedyaDto MapToDto(Medya medya)
        {
            return new MedyaDto
            {
                Id = medya.Id,
                DosyaAdi = medya.DosyaAdi,
                DosyaUrl = medya.DosyaUrl,
                DosyaTipi = medya.DosyaTipi,
                DosyaBoyutu = medya.DosyaBoyutu,
                Baslik = medya.Baslik,
                AnahtarKelimeler = medya.AnahtarKelimeler,
                YuklenmeTarihi = medya.YuklenmeTarihi
            };
        }
    }
}
