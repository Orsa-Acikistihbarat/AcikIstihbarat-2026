using AcikIstihbarat.API.Data;
using AcikIstihbarat.API.Models.DTOs;
using AcikIstihbarat.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Services
{
    public class KategoriService : IKategoriService
    {
        private readonly AppDbContext _context;

        public KategoriService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<KategoriDto>> GetAllKategorilerAsync()
        {
            var kategoriler = await _context.HaberKategorileri.OrderBy(k => k.Sira).ToListAsync();
            return kategoriler.Select(MapToDto);
        }

        public async Task<IEnumerable<KategoriDto>> GetKategorilerTreeAsync()
        {
            var allKategoriler = await _context.HaberKategorileri.OrderBy(k => k.Sira).ToListAsync();
            var dtos = allKategoriler.Select(MapToDto).ToList();

            var topLevel = dtos.Where(k => k.ParentId == null).ToList();
            foreach (var category in topLevel)
            {
                PopulateChildren(category, dtos);
            }

            return topLevel;
        }

        private void PopulateChildren(KategoriDto parent, List<KategoriDto> allCategories)
        {
            var children = allCategories.Where(c => c.ParentId == parent.Id).ToList();
            parent.AltKategoriler = children;
            foreach (var child in children)
            {
                PopulateChildren(child, allCategories);
            }
        }

        public async Task<KategoriDto> GetKategoriByIdAsync(int id)
        {
            var kategori = await _context.HaberKategorileri.FindAsync(id);
            if (kategori == null) return null;

            return MapToDto(kategori);
        }

        public async Task<KategoriDto> CreateKategoriAsync(KategoriCreateDto dto)
        {
            var kategori = new HaberKategori
            {
                Ad = dto.Ad,
                Slug = GenerateSlug(dto.Ad),
                ParentId = dto.ParentId,
                Sira = dto.Sira
            };

            _context.HaberKategorileri.Add(kategori);
            await _context.SaveChangesAsync();

            return MapToDto(kategori);
        }

        public async Task<KategoriDto> UpdateKategoriAsync(int id, KategoriCreateDto dto)
        {
            var kategori = await _context.HaberKategorileri.FindAsync(id);
            if (kategori == null) return null;

            kategori.Ad = dto.Ad;
            kategori.Slug = GenerateSlug(dto.Ad);
            kategori.ParentId = dto.ParentId;
            kategori.Sira = dto.Sira;

            _context.HaberKategorileri.Update(kategori);
            await _context.SaveChangesAsync();

            return MapToDto(kategori);
        }

        public async Task<bool> DeleteKategoriAsync(int id)
        {
            var kategori = await _context.HaberKategorileri
                                .Include(k => k.AltKategoriler)
                                .Include(k => k.Haberler)
                                .FirstOrDefaultAsync(k => k.Id == id);
            
            if (kategori == null) return false;

            if (kategori.AltKategoriler.Any())
                throw new InvalidOperationException("Alt kategorisi olan silinemez.");

            if (kategori.Haberler.Any())
                throw new InvalidOperationException("Bu kategoriye ait haberler var, silinemez.");

            _context.HaberKategorileri.Remove(kategori);
            await _context.SaveChangesAsync();
            return true;
        }

        private KategoriDto MapToDto(HaberKategori kategori)
        {
            return new KategoriDto
            {
                Id = kategori.Id,
                Ad = kategori.Ad,
                Slug = kategori.Slug,
                ParentId = kategori.ParentId,
                Sira = kategori.Sira
            };
        }

        private string GenerateSlug(string phrase)
        {
            string str = phrase.ToLowerInvariant();
            str = str.Replace("ş", "s").Replace("ğ", "g").Replace("ı", "i")
                     .Replace("ö", "o").Replace("ç", "c").Replace("ü", "u");
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = Regex.Replace(str, @"\s+", " ").Trim();
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
            str = Regex.Replace(str, @"\s", "-");
            return str;
        }
    }
}
