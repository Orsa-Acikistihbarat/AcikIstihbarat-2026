using AcikIstihbarat.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Controllers.Public
{
    [ApiController]
    [Route("api/public/[controller]")]
    public class KategorilerController : ControllerBase
    {
        private readonly IKategoriService _kategoriService;

        public KategorilerController(IKategoriService kategoriService)
        {
            _kategoriService = kategoriService;
        }

        [HttpGet]
        public async Task<IActionResult> GetKategorilerTree()
        {
            var tree = await _kategoriService.GetKategorilerTreeAsync();
            return Ok(tree);
        }
    }
}
