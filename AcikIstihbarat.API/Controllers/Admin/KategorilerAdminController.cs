using AcikIstihbarat.API.Models.DTOs;
using AcikIstihbarat.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/kategoriler")]
    [Authorize]
    public class KategorilerAdminController : ControllerBase
    {
        private readonly IKategoriService _kategoriService;

        public KategorilerAdminController(IKategoriService kategoriService)
        {
            _kategoriService = kategoriService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _kategoriService.GetAllKategorilerAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _kategoriService.GetKategoriByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] KategoriCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _kategoriService.CreateKategoriAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] KategoriCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _kategoriService.UpdateKategoriAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _kategoriService.DeleteKategoriAsync(id);
                if (!success) return NotFound();
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
