using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcikIstihbarat.API.Data;
using AcikIstihbarat.API.Models.DTOs;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AcikIstihbarat.API.Controllers.Public
{
    [Route("api/public/[controller]")]
    [ApiController]
    public class YazarlarController : ControllerBase
    {
        private readonly AppDbContext _context;

        public YazarlarController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<YazarDto>>> GetYazarlar()
        {
            var yazarlar = await _context.Yazarlar
                .Select(y => new YazarDto
                {
                    Id = y.YazarID,
                    Ad = y.YazarAdi
                })
                .ToListAsync();

            return Ok(yazarlar);
        }

        [HttpGet("{id}/yazilar")]
        public async Task<ActionResult<IEnumerable<YaziDto>>> GetYazarYazilari(int id)
        {
            var yazilar = await _context.Yazilar
                .Include(y => y.Yazar)
                .Where(y => y.YazarId == id && y.Onay)
                .OrderByDescending(y => y.YaziId)
                .Select(y => new YaziDto
                {
                    Id = y.YaziId,
                    YazarId = y.YazarId,
                    YazarAd = y.Yazar.YazarAdi,
                    Baslik = y.Baslik,
                    OnIzlemeMetni = y.OnIzlemeMetni,
                    Tarih = y.Tarih
                })
                .ToListAsync();

            return Ok(yazilar);
        }
    }
}
