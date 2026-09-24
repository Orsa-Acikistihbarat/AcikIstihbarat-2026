using AcikIstihbarat.API.Data;
using AcikIstihbarat.API.Helpers;
using AcikIstihbarat.API.Models.DTOs;
using AcikIstihbarat.API.Models.Entities;
using AcikIstihbarat.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace AcikIstihbarat.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/mail")]
    [Authorize]
    public class MailAdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMailRunTracker _tracker;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IServiceScopeFactory _scopeFactory;

        public MailAdminController(
            AppDbContext db,
            IMailRunTracker tracker,
            IHostApplicationLifetime lifetime,
            IServiceScopeFactory scopeFactory)
        {
            _db = db;
            _tracker = tracker;
            _lifetime = lifetime;
            _scopeFactory = scopeFactory;
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

        [HttpGet("subscribers/summary")]
        public async Task<IActionResult> GetSubscriberSummary(CancellationToken ct)
        {
            var grouped = await _db.MailSubscribers
                .Where(s => s.ConfirmedAt != null)
                .GroupBy(s => s.TemplateBaseName)
                .Select(g => new
                {
                    TemplateBaseName = g.Key,
                    Subscribed = g.Count(s => s.IsActive),
                    Unsubscribed = g.Count(s => !s.IsActive),
                })
                .ToListAsync(ct);

            // 0-fill every known newsletter so a card is never silently missing.
            var result = NewsletterDisplayNames.Keys
                .Select(key =>
                {
                    var match = grouped.FirstOrDefault(g => g.TemplateBaseName == key);
                    return new MailSubscriberSummaryItem
                    {
                        Key = key,
                        Title = NewsletterDisplayNames.Resolve(key),
                        SubscribedCount = match?.Subscribed ?? 0,
                        UnsubscribedCount = match?.Unsubscribed ?? 0,
                    };
                })
                .ToList();

            return Ok(result);
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

        [HttpPost("trigger")]
        public async Task<IActionResult> TriggerManualSend(
            [FromBody] TriggerMailRequest request,
            CancellationToken ct)
        {
            if (request.NewsletterKeys == null || request.NewsletterKeys.Count == 0)
            {
                return BadRequest(new { message = "En az bir bülten seçilmelidir." });
            }

            var invalidKeys = request.NewsletterKeys
                .Where(k => !NewsletterDisplayNames.Keys.Contains(k))
                .ToList();

            if (invalidKeys.Count > 0)
            {
                return BadRequest(new { message = $"Geçersiz bülten anahtarları: {string.Join(", ", invalidKeys)}" });
            }

            if (_tracker.IsAnyRunActive())
            {
                return Conflict(new { message = "Şu anda devam eden bir e-posta gönderimi var. Lütfen tamamlanmasını bekleyin." });
            }

            var schedules = await _db.MailSchedules
                .Where(s => s.IsActive && request.NewsletterKeys.Contains(s.TemplateBaseName))
                .ToListAsync(ct);

            if (schedules.Count == 0)
            {
                return BadRequest(new { message = "Seçilen bültenler için aktif bir gönderim programı bulunamadı." });
            }

            var today = IstanbulClock.NowLocal().Date;
            var activeSubscribers = await _db.MailSubscribers
                .Where(s => s.IsActive && request.NewsletterKeys.Contains(s.TemplateBaseName))
                .ToListAsync(ct);

            var totalRecipients = activeSubscribers
                .Count(s => request.ForceResend || s.LastSentAt is null || IstanbulClock.ToLocal(s.LastSentAt.Value).Date != today);

            if (!_tracker.TryStartBatchRun(request.NewsletterKeys, totalRecipients, out var batchId))
            {
                return Conflict(new { message = "Eşzamanlı başka bir gönderim işlemi başlatıldı." });
            }

            var appStoppingToken = _lifetime.ApplicationStopping;

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var orchestrator = scope.ServiceProvider.GetRequiredService<IMailingOrchestrator>();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    foreach (var key in request.NewsletterKeys)
                    {
                        if (appStoppingToken.IsCancellationRequested)
                        {
                            _tracker.FailBatchRun("Sunucu kapatıldığı için gönderim durduruldu.");
                            return;
                        }

                        var schedule = await db.MailSchedules
                            .FirstOrDefaultAsync(s => s.TemplateBaseName == key && s.IsActive, appStoppingToken);

                        if (schedule is not null)
                        {
                            await orchestrator.RunScheduleAsync(schedule.Id, request.ForceResend, appStoppingToken);
                        }
                    }

                    _tracker.CompleteBatchRun();
                }
                catch (OperationCanceledException)
                {
                    _tracker.FailBatchRun("Sunucu kapanma sürecine girdiği için işlem iptal edildi.");
                }
                catch (Exception ex)
                {
                    _tracker.FailBatchRun($"Beklenmeyen hata: {ex.Message}");
                }
            }, appStoppingToken);

            return Accepted(new
            {
                message = "Gönderim süreci arka planda başlatıldı.",
                batchId,
                totalRecipients
            });
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var status = _tracker.GetCurrentStatus();
            return Ok(MailRunStatusDto.FromStatus(status));
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
