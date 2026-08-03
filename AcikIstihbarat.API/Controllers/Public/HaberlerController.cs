using AcikIstihbarat.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Controllers.Public
{
    [ApiController]
    [Route("api/public/[controller]")]
    public class HaberlerController : ControllerBase
    {
        private readonly IHaberService _haberService;

        public HaberlerController(IHaberService haberService)
        {
            _haberService = haberService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 12)
        {
            var result = await _haberService.GetHaberlerAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("manset")]
        public async Task<IActionResult> GetMansetler()
        {
            var result = await _haberService.GetMansetlerAsync();
            return Ok(result);
        }

        [HttpGet("kategori/{kategoriId}")]
        public async Task<IActionResult> GetByKategori(int kategoriId, [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
        {
            var result = await _haberService.GetHaberlerByKategoriAsync(kategoriId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _haberService.GetHaberByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
}
