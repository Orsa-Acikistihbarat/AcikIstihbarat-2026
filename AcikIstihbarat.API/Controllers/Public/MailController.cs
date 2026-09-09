using AcikIstihbarat.API.Data;
using AcikIstihbarat.API.Helpers;
using AcikIstihbarat.API.Models.DTOs;
using AcikIstihbarat.API.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AcikIstihbarat.API.Controllers.Public
{
    [ApiController]
    [Route("api/public/mail")]
    public class MailController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly MailOptions _mailOptions;
        private readonly ILogger<MailController> _logger;

        public MailController(AppDbContext db, IOptions<MailOptions> mailOptions, ILogger<MailController> logger)
        {
            _db = db;
            _mailOptions = mailOptions.Value;
            _logger = logger;
        }

        private const string ConfirmationPageStyle =
            "body{font-family:sans-serif;max-width:480px;margin:80px auto;text-align:center;color:#333}" +
            "button{padding:10px 24px;font-size:16px;cursor:pointer}";

        [HttpGet("unsubscribe")]
        public IActionResult UnsubscribeConfirm([FromQuery] Guid token)
        {
            // Does NOT mutate state - always renders the same neutral confirmation page regardless
            // of whether the token is valid, both to avoid leaking token validity (minor
            // enumeration hardening) and so automated mail-security link-prefetchers (which GET
            // every link in an inbound email before a human opens it) can't silently trigger an
            // unsubscribe just by fetching this page.
            var html = $"""
                <!DOCTYPE html>
                <html lang="tr">
                <head><meta charset="utf-8"><title>Bültenden çık</title><style>{ConfirmationPageStyle}</style></head>
                <body>
                    <h2>Bültenden çıkmak istediğinize emin misiniz?</h2>
                    <form method="post" action="/api/public/mail/unsubscribe">
                        <input type="hidden" name="token" value="{token}" />
                        <button type="submit">Bültenden çık</button>
                    </form>
                </body>
                </html>
                """;
            return Content(html, "text/html");
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> UnsubscribeConfirmed([FromForm] Guid token, CancellationToken ct)
        {
            // Used both by the confirmation page's button POST and by mail clients' native
            // one-click unsubscribe (List-Unsubscribe-Post per RFC 8058). Always returns a generic
            // confirmation regardless of whether a matching token was found - same
            // enumeration-hardening rationale as the GET action above.
            var subscriber = await _db.MailSubscribers.FirstOrDefaultAsync(s => s.UnsubscribeToken == token, ct);
            if (subscriber is not null)
            {
                subscriber.IsActive = false;
                await _db.SaveChangesAsync(ct);
            }

            var html = $"""
                <!DOCTYPE html>
                <html lang="tr">
                <head><meta charset="utf-8"><title>Bültenden çıkıldı</title><style>{ConfirmationPageStyle}</style></head>
                <body>
                    <h2>Bülten aboneliğiniz iptal edildi.</h2>
                </body>
                </html>
                """;
            return Content(html, "text/html");
        }

        [HttpPost("subscribe")]
        public async Task<ActionResult<SubscribeResponse>> Subscribe(
            [FromBody] SubscribeRequest request, CancellationToken ct)
        {
            if (!System.Net.Mail.MailAddress.TryCreate(request.Email, out _))
            {
                return BadRequest("Invalid email address.");
            }

            if (request.TemplateBaseNames is null || request.TemplateBaseNames.Count == 0)
            {
                return BadRequest("No newsletters selected.");
            }

            var allowList = await _db.MailSchedules
                .Select(s => s.TemplateBaseName)
                .Distinct()
                .ToListAsync(ct);

            // Silently drop unknown entries (defensive against stale frontend caches sending a
            // since-removed folder name) rather than erroring the whole request.
            var validTemplateNames = request.TemplateBaseNames
                .Where(n => allowList.Contains(n))
                .Distinct()
                .ToList();

            if (validTemplateNames.Count == 0)
            {
                return BadRequest("No valid newsletters selected.");
            }

            var confirmToken = Guid.NewGuid();
            var expiresAt = DateTime.UtcNow.AddDays(3);

            var results = new List<SubscribeResultItem>();
            var confirmedBatchNames = new List<string>();

            foreach (var templateBaseName in validTemplateNames)
            {
                var existing = await _db.MailSubscribers.FirstOrDefaultAsync(
                    s => s.Email == request.Email && s.TemplateBaseName == templateBaseName, ct);

                if (existing is not null && existing.IsActive)
                {
                    results.Add(new SubscribeResultItem
                    {
                        TemplateBaseName = templateBaseName,
                        Success = false,
                        ErrorMessage = "Bu bültene zaten abonesiniz.",
                    });
                }
                else if (existing is not null && !existing.IsActive && existing.ConfirmTokenExpiresAt > DateTime.UtcNow)
                {
                    results.Add(new SubscribeResultItem
                    {
                        TemplateBaseName = templateBaseName,
                        Success = false,
                        ErrorMessage = "Bu bülten için onay bekleniyor, e-postanızı kontrol edin.",
                    });
                }
                else if (existing is not null)
                {
                    // Not active, and previous confirm token already expired - reuse the row with a
                    // fresh token.
                    existing.ConfirmToken = confirmToken;
                    existing.ConfirmTokenExpiresAt = expiresAt;
                    results.Add(new SubscribeResultItem { TemplateBaseName = templateBaseName, Success = true });
                    confirmedBatchNames.Add(templateBaseName);
                }
                else
                {
                    _db.MailSubscribers.Add(new MailSubscriber
                    {
                        Email = request.Email,
                        TemplateBaseName = templateBaseName,
                        IsActive = false,
                        ConfirmToken = confirmToken,
                        ConfirmTokenExpiresAt = expiresAt,
                        UnsubscribeToken = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow,
                    });
                    results.Add(new SubscribeResultItem { TemplateBaseName = templateBaseName, Success = true });
                    confirmedBatchNames.Add(templateBaseName);
                }
            }

            if (confirmedBatchNames.Count > 0)
            {
                _db.PendingConfirmationEmails.Add(new PendingConfirmationEmail
                {
                    Email = request.Email,
                    ConfirmToken = confirmToken,
                    TemplateDisplayNamesCsv = string.Join(",", confirmedBatchNames.Select(NewsletterDisplayNames.Resolve)),
                    CreatedAt = DateTime.UtcNow,
                });
            }

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // TOCTOU race: a concurrent duplicate submission for the same (Email,
                // TemplateBaseName) pair can violate the unique index between our SELECT check
                // above and this SaveChangesAsync. Re-query current state and re-classify any
                // affected items as "already pending"/"already subscribed" instead of surfacing an
                // unhandled 500 for the whole batch.
                _logger.LogWarning(ex, "Concurrent subscribe conflict for {Email}, re-classifying affected items.", request.Email);

                results.Clear();

                var current = await _db.MailSubscribers
                    .Where(s => s.Email == request.Email && validTemplateNames.Contains(s.TemplateBaseName))
                    .AsNoTracking()
                    .ToListAsync(ct);

                foreach (var templateBaseName in validTemplateNames)
                {
                    var row = current.FirstOrDefault(s => s.TemplateBaseName == templateBaseName);
                    if (row is null)
                    {
                        results.Add(new SubscribeResultItem
                        {
                            TemplateBaseName = templateBaseName,
                            Success = false,
                            ErrorMessage = "Bu bülten için kayıt oluşturulamadı, lütfen tekrar deneyin.",
                        });
                    }
                    else if (row.IsActive)
                    {
                        results.Add(new SubscribeResultItem
                        {
                            TemplateBaseName = templateBaseName,
                            Success = false,
                            ErrorMessage = "Bu bültene zaten abonesiniz.",
                        });
                    }
                    else
                    {
                        results.Add(new SubscribeResultItem
                        {
                            TemplateBaseName = templateBaseName,
                            Success = false,
                            ErrorMessage = "Bu bülten için onay bekleniyor, e-postanızı kontrol edin.",
                        });
                    }
                }
            }

            return Ok(new SubscribeResponse { Results = results });
        }

        [HttpGet("confirm")]
        public async Task<IActionResult> ConfirmPrompt([FromQuery] Guid token, CancellationToken ct)
        {
            // Does NOT mutate state - mirrors UnsubscribeConfirm's GET/POST split above, so
            // automated mail-security link-prefetchers can't silently activate a subscription just
            // by fetching this page.
            var matches = await _db.MailSubscribers.Where(s => s.ConfirmToken == token).ToListAsync(ct);
            if (matches.Count == 0)
            {
                return Content(BuildConfirmationLandingHtml(LandingCase.Invalid, new(), new()), "text/html");
            }

            var now = DateTime.UtcNow;
            var pending = matches.Where(s => !s.IsActive && s.ConfirmTokenExpiresAt >= now).ToList();
            var expired = matches.Where(s => !s.IsActive && s.ConfirmTokenExpiresAt < now).ToList();

            if (pending.Count == 0)
            {
                return Content(
                    BuildConfirmationLandingHtml(LandingCase.AllExpired, new(), expired.Select(s => s.TemplateBaseName).Select(NewsletterDisplayNames.Resolve).ToList()),
                    "text/html");
            }

            var pendingNames = pending.Select(s => s.TemplateBaseName).Select(NewsletterDisplayNames.Resolve).ToList();
            return Content(BuildConfirmationLandingHtml(LandingCase.PendingPrompt, pendingNames, new(), token), "text/html");
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmSubmit([FromForm] Guid token, CancellationToken ct)
        {
            var matches = await _db.MailSubscribers.Where(s => s.ConfirmToken == token).ToListAsync(ct);
            if (matches.Count == 0)
            {
                return Content(BuildConfirmationLandingHtml(LandingCase.Invalid, new(), new()), "text/html");
            }

            var confirmedNames = new List<string>();
            var expiredNames = new List<string>();
            var now = DateTime.UtcNow;
            foreach (var s in matches)
            {
                if (!s.IsActive && s.ConfirmTokenExpiresAt < now)
                {
                    expiredNames.Add(s.TemplateBaseName);
                    continue;
                }

                s.IsActive = true;
                confirmedNames.Add(s.TemplateBaseName);
            }

            await _db.SaveChangesAsync(ct);

            var landingCase = confirmedNames.Count > 0 ? LandingCase.Success : LandingCase.AllExpired;
            return Content(
                BuildConfirmationLandingHtml(
                    landingCase,
                    confirmedNames.Select(NewsletterDisplayNames.Resolve).ToList(),
                    expiredNames.Select(NewsletterDisplayNames.Resolve).ToList()),
                "text/html");
        }

        private enum LandingCase
        {
            Invalid,
            AllExpired,
            Success,
            PendingPrompt,
        }

        private string BuildConfirmationLandingHtml(
            LandingCase landingCase,
            List<string> confirmedDisplayNames,
            List<string> expiredDisplayNames,
            Guid? promptToken = null)
        {
            var siteUrl = _mailOptions.PublicSiteBaseUrl;
            string heading, body;
            switch (landingCase)
            {
                case LandingCase.PendingPrompt:
                    heading = "Aboneliğinizi onaylayın";
                    body = "<p>Aşağıdaki bültenlere aboneliğinizi onaylamak için butona tıklayın:</p>"
                        + BuildList(confirmedDisplayNames)
                        + $"""
                        <form method="post" action="/api/public/mail/confirm">
                            <input type="hidden" name="token" value="{promptToken}" />
                            <button type="submit">Aboneliği Onayla</button>
                        </form>
                        """;
                    break;
                case LandingCase.Invalid:
                    heading = "Geçersiz bağlantı";
                    body = "<p>Bu onay bağlantısı geçerli değil. Aboneliğinizi tekrar başlatmak için bültenler sayfasına dönebilirsiniz.</p>";
                    break;
                case LandingCase.AllExpired:
                    heading = "Onay bağlantısının süresi doldu";
                    body = "<p>Bu bağlantının süresi dolmuş. Lütfen bültenler sayfasından tekrar abone olun:</p>"
                        + BuildList(expiredDisplayNames);
                    break;
                default: // Success
                    heading = "Aboneliğiniz onaylandı!";
                    body = "<p>Aşağıdaki bültenlere aboneliğiniz başarıyla onaylandı:</p>" + BuildList(confirmedDisplayNames);
                    if (expiredDisplayNames.Count > 0)
                    {
                        body += "<p>Şu bültenler için onay süresi dolmuş, tekrar abone olmanız gerekiyor:</p>" + BuildList(expiredDisplayNames);
                    }
                    break;
            }

            return $"""
                <!DOCTYPE html>
                <html lang="tr">
                <head><meta charset="utf-8"><title>{heading}</title><style>{ConfirmationPageStyle}</style></head>
                <body>
                    <h2>{heading}</h2>
                    {body}
                    <p><a href="{siteUrl}/acikmedya/AcikGazete">Bültenlere dön</a></p>
                </body>
                </html>
                """;
        }

        private static string BuildList(List<string> names) =>
            names.Count == 0 ? "" : "<ul>" + string.Join("", names.Select(n => $"<li>{System.Net.WebUtility.HtmlEncode(n)}</li>")) + "</ul>";
    }
}
