using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcikIstihbarat.API.Data;
using AcikIstihbarat.API.Models.DTOs;
using System.Linq;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Controllers.Public
{
    [Route("api/public/[controller]")]
    [ApiController]
    public class YazilarController : ControllerBase
    {
        private readonly AppDbContext _context;

        public YazilarController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<YaziDto>> GetYazi(int id)
        {
            var yazi = await _context.Yazilar
                .Include(y => y.Yazar)
                .Where(y => y.YaziId == id && y.Onay)
                .Select(y => new YaziDto
                {
                    Id = y.YaziId,
                    YazarId = y.YazarId,
                    YazarAd = y.Yazar.YazarAdi,
                    Baslik = y.Baslik,
                    OnIzlemeMetni = y.OnIzlemeMetni,
                    TamMetin = y.TamMetin,
                    Tarih = y.Tarih
                })
                .FirstOrDefaultAsync();

            if (yazi == null)
            {
                return NotFound();
            }

            // Increment read count
            var yaziEntity = await _context.Yazilar.FindAsync(id);
            if (yaziEntity != null)
            {
                yaziEntity.Okunma++;
                await _context.SaveChangesAsync();
            }

            return Ok(yazi);
        }
    }
}
