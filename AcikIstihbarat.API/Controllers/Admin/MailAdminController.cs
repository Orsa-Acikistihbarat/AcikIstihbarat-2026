using AcikIstihbarat.API.Data;
using AcikIstihbarat.API.Helpers;
using AcikIstihbarat.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcikIstihbarat.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/mail")]
    [Authorize]
    public class MailAdminController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MailAdminController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("subscribers")]
        public async Task<IActionResult> GetSubscribers(CancellationToken ct)
        {
            var rows = await _db.MailSubscribers
                .Where(s => s.ConfirmedAt != null)
                .OrderByDescending(s => s.LastConfirmedAt)
                .Select(s => new MailSubscriberAdminItem
                {
                    Email = s.Email,
                    NewsletterDisplayName = NewsletterDisplayNames.Resolve(s.TemplateBaseName),
                    SubscriptionDate = s.LastConfirmedAt,
                    UnsubscriptionDate = s.UnsubscribedAt,
                })
                .ToListAsync(ct);

            return Ok(rows);
        }
    }
}
