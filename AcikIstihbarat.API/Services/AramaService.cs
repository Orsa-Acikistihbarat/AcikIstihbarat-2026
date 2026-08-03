using AcikIstihbarat.API.Data;
using AcikIstihbarat.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Services
{
    public class AramaService : IAramaService
    {
        private readonly AppDbContext _context;

        public AramaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AramaSonucDto> AraAsync(string q, string mode, int page, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return new AramaSonucDto
                {
                    Haberler = new PagedResult<HaberListDto> { Items = new(), Page = page, PageSize = pageSize, TotalCount = 0 },
                    Yazilar = new PagedResult<SliderItemDto> { Items = new(), Page = page, PageSize = pageSize, TotalCount = 0 }
                };
            }

            var searchTerm = q.Trim();
            
            // Format search term based on mode
            string formattedQuery = searchTerm;
            if (mode == "exact")
            {
                formattedQuery = $"\"{searchTerm.Replace("\"", "")}\"";
            }
            else if (mode == "and")
            {
                var words = searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(w => $"\"{w.Replace("\"", "")}\"");
                formattedQuery = string.Join(" AND ", words);
            }

            // 1. Haber search
            var haberQuery = _context.Haberler
                .Include(h => h.Kategori)
                .Include(h => h.Medyalar).ThenInclude(hm => hm.Medya)
                .AsQueryable();

            if (mode == "exact" || mode == "and")
            {
                haberQuery = haberQuery.Where(h => EF.Functions.Contains(h.Baslik, formattedQuery) || 
                                                   EF.Functions.Contains(h.Spot, formattedQuery) || 
                                                   EF.Functions.Contains(h.HtmlIcerigi, formattedQuery));
            }
            else
            {
                haberQuery = haberQuery.Where(h => EF.Functions.FreeText(h.Baslik, searchTerm) || 
                                                   EF.Functions.FreeText(h.Spot, searchTerm) || 
                                                   EF.Functions.FreeText(h.HtmlIcerigi, searchTerm));
            }

            haberQuery = haberQuery.OrderByDescending(h => h.Tarih);

            var haberTotal = await haberQuery.CountAsync();
            var haberItems = await haberQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var haberListDtos = haberItems.Select(h => {
                var images = h.Medyalar?.Where(m => m.Medya.DosyaTipi.StartsWith("image/")).OrderBy(m => m.Sira).ToList();
                string thumbUrl = images != null && images.Any() ? 
                    (images.FirstOrDefault(m => m.OncuResimMi) ?? images.First()).Medya.DosyaUrl : 
                    (!string.IsNullOrEmpty(h.LegacyResimAdresi) ? h.LegacyResimAdresi : null);
                
                return new HaberListDto
                {
                    Id = h.Id,
                    Baslik = h.Baslik,
                    Spot = h.Spot,
                    Tarih = h.Tarih,
                    KategoriId = h.KategoriId,
                    KategoriAd = h.Kategori?.Ad,
                    ThumbnailUrl = thumbUrl
                };
            }).ToList();

            // 2. Yazi search
            var yaziQuery = _context.Yazilar
                .Include(y => y.Yazar)
                .AsQueryable();

            if (mode == "exact" || mode == "and")
            {
                yaziQuery = yaziQuery.Where(y => EF.Functions.Contains(y.Baslik, formattedQuery) || 
                                                 EF.Functions.Contains(y.OnIzlemeMetni, formattedQuery) || 
                                                 EF.Functions.Contains(y.TamMetin, formattedQuery));
            }
            else
            {
                yaziQuery = yaziQuery.Where(y => EF.Functions.FreeText(y.Baslik, searchTerm) || 
                                                 EF.Functions.FreeText(y.OnIzlemeMetni, searchTerm) || 
                                                 EF.Functions.FreeText(y.TamMetin, searchTerm));
            }
            
            yaziQuery = yaziQuery.OrderByDescending(y => y.Tarih);

            var yaziTotal = await yaziQuery.CountAsync();
            var yaziItems = await yaziQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var yaziListDtos = yaziItems.Select(y => new SliderItemDto
            {
                Id = y.YaziId,
                Tip = "Yazi",
                Baslik = y.Baslik,
                Spot = y.OnIzlemeMetni,
                Tarih = y.Tarih ?? DateTime.Now,
                ThumbnailUrl = "",
                BadgeLabel = y.Yazar?.YazarAdi
            }).ToList();

            return new AramaSonucDto
            {
                Haberler = new PagedResult<HaberListDto> { Items = haberListDtos, Page = page, PageSize = pageSize, TotalCount = haberTotal },
                Yazilar = new PagedResult<SliderItemDto> { Items = yaziListDtos, Page = page, PageSize = pageSize, TotalCount = yaziTotal }
            };
        }
    }
}
