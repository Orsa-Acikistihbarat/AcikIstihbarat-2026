using AcikIstihbarat.API.Data;
using AcikIstihbarat.API.Models.DTOs;
using AcikIstihbarat.API.Models.Entities;
using AcikIstihbarat.API.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Services
{
    public class HaberService : IHaberService
    {
        private readonly AppDbContext _context;

        public HaberService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<HaberListDto>> GetHaberlerAsync(int page, int pageSize)
        {
            var query = _context.Haberler
                .Include(h => h.Kategori)
                .Include(h => h.Medyalar).ThenInclude(hm => hm.Medya)
                .OrderByDescending(h => h.Tarih);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<HaberListDto>
            {
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                Items = items.Select(MapToListDto).ToList()
            };
        }

        public async Task<IEnumerable<SliderItemDto>> GetMansetlerAsync()
        {
            int targetCount = 12;
            var tenDaysAgo = DateTime.UtcNow.AddDays(-10);

            var haberler = await _context.Haberler
                .Include(h => h.Kategori)
                .Include(h => h.Medyalar).ThenInclude(hm => hm.Medya)
                .Where(h => h.Mansetmi && h.Tarih >= tenDaysAgo)
                .OrderByDescending(h => h.Tarih)
                .Take(targetCount)
                .ToListAsync();

            var sliderItems = haberler.Select(h =>
            {
                var listDto = MapToListDto(h);
                return new SliderItemDto
                {
                    Id = h.Id,
                    Tip = "Haber",
                    Baslik = h.Baslik,
                    Spot = h.Spot,
                    Tarih = h.Tarih,
                    ThumbnailUrl = listDto.ThumbnailUrl,
                    BadgeLabel = h.Kategori?.Ad
                };
            }).ToList();

            int remaining = targetCount - sliderItems.Count;
            if (remaining > 0)
            {
                var excludedHaberIds = sliderItems.Select(s => s.Id).ToList();

                var randomHaberler = await _context.Haberler
                    .Include(h => h.Kategori)
                    .Include(h => h.Medyalar).ThenInclude(hm => hm.Medya)
                    .Where(h => !excludedHaberIds.Contains(h.Id))
                    .OrderBy(h => Guid.NewGuid())
                    .Take(remaining)
                    .ToListAsync();

                var randomYazilar = await _context.Yazilar
                    .Include(y => y.Yazar)
                    .OrderBy(y => Guid.NewGuid())
                    .Take(remaining)
                    .ToListAsync();

                var combinedFallback = new List<SliderItemDto>();

                combinedFallback.AddRange(randomHaberler.Select(h => 
                {
                    var listDto = MapToListDto(h);
                    return new SliderItemDto
                    {
                        Id = h.Id,
                        Tip = "Haber",
                        Baslik = h.Baslik,
                        Spot = h.Spot,
                        Tarih = h.Tarih,
                        ThumbnailUrl = listDto.ThumbnailUrl,
                        BadgeLabel = h.Kategori?.Ad
                    };
                }));

                combinedFallback.AddRange(randomYazilar.Select(y => new SliderItemDto
                {
                    Id = y.YaziId,
                    Tip = "Yazi",
                    Baslik = y.Baslik,
                    Spot = y.OnIzlemeMetni,
                    Tarih = y.Tarih,
                    ThumbnailUrl = "",
                    BadgeLabel = y.Yazar?.YazarAdi
                }));

                var finalFallback = combinedFallback.OrderBy(x => Guid.NewGuid()).Take(remaining);
                sliderItems.AddRange(finalFallback);
            }

            return sliderItems;
        }

        public async Task<PagedResult<HaberListDto>> GetHaberlerByKategoriAsync(int kategoriId, int page, int pageSize)
        {
            var query = _context.Haberler
                .Include(h => h.Kategori)
                .Include(h => h.Medyalar).ThenInclude(hm => hm.Medya)
                .Where(h => h.KategoriId == kategoriId || h.Kategori.ParentId == kategoriId)
                .OrderByDescending(h => h.Tarih);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<HaberListDto>
            {
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                Items = items.Select(MapToListDto).ToList()
            };
        }

        public async Task<HaberDto> GetHaberByIdAsync(int id)
        {
            var haber = await _context.Haberler
                .Include(h => h.Kategori)
                .Include(h => h.Medyalar).ThenInclude(hm => hm.Medya)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (haber == null) return null;

            return MapToDto(haber);
        }

        public async Task<HaberDto> CreateHaberAsync(HaberCreateDto dto)
        {
            var haber = new Haber
            {
                Baslik = dto.Baslik,
                Spot = dto.Spot,
                HtmlIcerigi = dto.HtmlIcerigi,
                KategoriId = dto.KategoriId,
                Mansetmi = dto.Mansetmi,
                Tarih = DateTime.UtcNow,
                LegacyResimAdresi = "",
                KullaniciId = 1, // Default to 1 (Admin/System)
                Saat = DateTime.UtcNow.ToString("HH:mm"),
                YorumaAcikMi = true, // Default open for comments
                Okunma = 0, // Starts at 0 reads
                Onay = true, // Default to approved
                SilindiMi = false
            };

            _context.Haberler.Add(haber);
            await _context.SaveChangesAsync();

            return await GetHaberByIdAsync(haber.Id);
        }

        public async Task<HaberDto> UpdateHaberAsync(int id, HaberCreateDto dto)
        {
            var haber = await _context.Haberler.FindAsync(id);
            if (haber == null) return null;

            haber.Baslik = dto.Baslik;
            haber.Spot = dto.Spot;
            haber.HtmlIcerigi = dto.HtmlIcerigi;
            haber.KategoriId = dto.KategoriId;
            haber.Mansetmi = dto.Mansetmi;

            _context.Haberler.Update(haber);
            await _context.SaveChangesAsync();

            return await GetHaberByIdAsync(haber.Id);
        }

        public async Task<bool> DeleteHaberAsync(int id)
        {
            var haber = await _context.Haberler.FindAsync(id);
            if (haber == null) return false;

            haber.SilindiMi = true;
            _context.Haberler.Update(haber);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetMedyalarAsync(int id, List<HaberMedyaDto> medyalar)
        {
            var haber = await _context.Haberler
                .Include(h => h.Medyalar)
                .FirstOrDefaultAsync(h => h.Id == id);
                
            if (haber == null) return false;

            _context.HaberMedyalar.RemoveRange(haber.Medyalar);
            
            foreach (var mdto in medyalar)
            {
                _context.HaberMedyalar.Add(new HaberMedya
                {
                    HaberId = id,
                    MedyaId = mdto.MedyaId,
                    OncuResimMi = mdto.OncuResimMi,
                    Sira = mdto.Sira
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CleanupHtmlInSpotAsync()
        {
            int totalProcessed = 0;
            int batchSize = 500;
            bool hasMore = true;
            int lastId = 0;
            
            while (hasMore)
            {
                var recordsToClean = await _context.Haberler
                    .Where(h => h.Id > lastId && h.Spot != null && h.Spot.Contains("<"))
                    .OrderBy(h => h.Id)
                    .Take(batchSize)
                    .ToListAsync();
                    
                if (!recordsToClean.Any())
                {
                    hasMore = false;
                    break;
                }
                
                foreach (var haber in recordsToClean)
                {
                    haber.Spot = HtmlCleanupHelper.StripHtml(haber.Spot);
                }
                
                await _context.SaveChangesAsync();
                totalProcessed += recordsToClean.Count;
                lastId = recordsToClean.Last().Id;
            }
            
            return totalProcessed;
        }

        private HaberListDto MapToListDto(Haber haber)
        {
            var images = haber.Medyalar?
                .Where(m => m.Medya.DosyaTipi.StartsWith("image/"))
                .OrderBy(m => m.Sira).ToList();

            string thumbUrl = null;
            if (images != null && images.Any())
            {
                var featured = images.FirstOrDefault(m => m.OncuResimMi) ?? images.First();
                thumbUrl = featured.Medya.DosyaUrl;
            }
            else if (!string.IsNullOrEmpty(haber.LegacyResimAdresi))
            {
                thumbUrl = haber.LegacyResimAdresi;
            }

            return new HaberListDto
            {
                Id = haber.Id,
                Baslik = haber.Baslik,
                Spot = haber.Spot,
                Tarih = haber.Tarih,
                KategoriId = haber.KategoriId,
                KategoriAd = haber.Kategori?.Ad,
                ThumbnailUrl = thumbUrl
            };
        }

        private HaberDto MapToDto(Haber haber)
        {
            var gorseller = haber.Medyalar?
                .Where(m => m.Medya.DosyaTipi.StartsWith("image/"))
                .OrderBy(m => m.Sira)
                .Select(m => MapMedyaToDto(m.Medya)).ToList() ?? new List<MedyaDto>();

            var belgeler = haber.Medyalar?
                .Where(m => !m.Medya.DosyaTipi.StartsWith("image/"))
                .OrderBy(m => m.Sira)
                .Select(m => MapMedyaToDto(m.Medya)).ToList() ?? new List<MedyaDto>();

            return new HaberDto
            {
                Id = haber.Id,
                Baslik = haber.Baslik,
                Spot = haber.Spot,
                HtmlIcerigi = haber.HtmlIcerigi,
                KategoriId = haber.KategoriId,
                Kategori = haber.Kategori != null ? new KategoriDto 
                { 
                    Id = haber.Kategori.Id, 
                    Ad = haber.Kategori.Ad, 
                    Slug = haber.Kategori.Slug 
                } : null,
                Mansetmi = haber.Mansetmi,
                Tarih = haber.Tarih,
                LegacyResimAdresi = haber.LegacyResimAdresi,
                Gorseller = gorseller,
                Belgeler = belgeler
            };
        }

        private MedyaDto MapMedyaToDto(Medya medya)
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
