using AcikIstihbarat.API.Models.DTOs;
using AcikIstihbarat.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AcikIstihbarat.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/haberler")]
    [Authorize]
    public class HaberlerAdminController : ControllerBase
    {
        private readonly IHaberService _haberService;

        public HaberlerAdminController(IHaberService haberService)
        {
            _haberService = haberService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var result = await _haberService.GetHaberlerAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _haberService.GetHaberByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HaberCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _haberService.CreateHaberAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] HaberCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _haberService.UpdateHaberAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _haberService.DeleteHaberAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPost("{id}/medyalar")]
        public async Task<IActionResult> SetMedyalar(int id, [FromBody] List<HaberMedyaDto> medyalar)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var success = await _haberService.SetMedyalarAsync(id, medyalar);
            if (!success) return NotFound();
            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("cleanup-html")]
        public async Task<IActionResult> CleanupHtmlInSpot()
        {
            var processedCount = await _haberService.CleanupHtmlInSpotAsync();
            return Ok(new { Message = "Cleanup completed successfully", ProcessedRecords = processedCount });
        }

        [AllowAnonymous]
        [HttpPost("cleanup-yazi-html")]
        public async Task<IActionResult> CleanupHtmlInYaziSpot([FromServices] AcikIstihbarat.API.Data.AppDbContext dbContext)
        {
            int totalProcessed = 0;
            int batchSize = 500;
            bool hasMore = true;
            int lastId = 0;
            
            while (hasMore)
            {
                var recordsToClean = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                    System.Linq.Queryable.Take(
                        System.Linq.Queryable.OrderBy(
                            System.Linq.Queryable.Where(dbContext.Yazilar, y => y.YaziId > lastId && y.OnIzlemeMetni != null && y.OnIzlemeMetni.Contains("<")),
                            y => y.YaziId
                        ), 
                        batchSize
                    )
                );
                    
                if (!recordsToClean.Any())
                {
                    hasMore = false;
                    break;
                }
                
                foreach (var yazi in recordsToClean)
                {
                    yazi.OnIzlemeMetni = AcikIstihbarat.API.Helpers.HtmlCleanupHelper.StripHtml(yazi.OnIzlemeMetni);
                }
                
                await dbContext.SaveChangesAsync();
                totalProcessed += recordsToClean.Count;
                lastId = recordsToClean.Last().YaziId;
            }
            
            return Ok(new { Message = "Yazi cleanup completed successfully", ProcessedRecords = totalProcessed });
        }
    }
}
