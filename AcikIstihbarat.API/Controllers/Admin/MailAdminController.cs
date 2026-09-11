using AcikIstihbarat.API.Data;
using AcikIstihbarat.API.Helpers;
using AcikIstihbarat.API.Models.DTOs;
using AcikIstihbarat.API.Models.Entities;
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
                    Id = s.Id,
                    Email = s.Email,
                    NewsletterDisplayName = NewsletterDisplayNames.Resolve(s.TemplateBaseName),
                    SubscriptionDate = s.LastConfirmedAt,
                    UnsubscriptionDate = s.UnsubscribedAt,
                    IsActive = s.IsActive,
                })
                .ToListAsync(ct);

            return Ok(rows);
        }

        [HttpPut("subscribers/{id:int}/deactivate")]
        public async Task<IActionResult> DeactivateSubscriber(int id, CancellationToken ct)
        {
            var subscriber = await _db.MailSubscribers.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (subscriber is null)
            {
                return NotFound();
            }

            if (subscriber.IsActive)
            {
                subscriber.IsActive = false;
                subscriber.UnsubscribedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(ToAdminItem(subscriber));
        }

        private static MailSubscriberAdminItem ToAdminItem(MailSubscriber s) => new()
        {
            Id = s.Id,
            Email = s.Email,
            NewsletterDisplayName = NewsletterDisplayNames.Resolve(s.TemplateBaseName),
            SubscriptionDate = s.LastConfirmedAt,
            UnsubscriptionDate = s.UnsubscribedAt,
            IsActive = s.IsActive,
        };
    }
}
