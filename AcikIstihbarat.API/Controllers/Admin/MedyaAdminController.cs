using AcikIstihbarat.API.Models.DTOs;
using AcikIstihbarat.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/medya")]
    [Authorize]
    public class MedyaAdminController : ControllerBase
    {
        private readonly IMedyaService _medyaService;

        public MedyaAdminController(IMedyaService medyaService)
        {
            _medyaService = medyaService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _medyaService.GetAllMedyaAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _medyaService.GetMedyaByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] MedyaUploadDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            try
            {
                var result = await _medyaService.UploadMedyaAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _medyaService.DeleteMedyaAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
