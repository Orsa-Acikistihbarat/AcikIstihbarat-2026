using AcikIstihbarat.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Controllers.Public
{
    [ApiController]
    [Route("api/public/[controller]")]
    public class AramaController : ControllerBase
    {
        private readonly IAramaService _aramaService;

        public AramaController(IAramaService aramaService)
        {
            _aramaService = aramaService;
        }

        [HttpGet]
        public async Task<IActionResult> Ara([FromQuery] string q, [FromQuery] string mode = "or", [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
        {
            var result = await _aramaService.AraAsync(q, mode, page, pageSize);
            return Ok(result);
        }
    }
}
