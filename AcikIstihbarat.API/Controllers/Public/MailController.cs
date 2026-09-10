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

        // Matches the look & feel of the /acikmedya bulletin pages (same brand header, fonts,
        // and slate/turquoise/bordeaux palette from app/globals.css) so the landing pages a user
        // is redirected to from an email button don't feel like a different, unstyled site.
        private const string ConfirmationPageStyle = """
            *{box-sizing:border-box}
            body{
                font-family:'Inter',sans-serif;
                margin:0;
                min-height:100vh;
                background:#f8fafc;
                color:#334155;
            }
            .brand-header{
                padding:28px 24px 20px;
                border-bottom:1px solid #e2e8f0;
                text-align:center;
            }
            .brand-header a{
                font-family:'Outfit',sans-serif;
                font-weight:900;
                font-size:1.5rem;
                letter-spacing:-0.03em;
                color:#0f172a;
                text-decoration:none;
            }
            .brand-header a span{ color:#0891b2 }
            .page-content{
                max-width:480px;
                margin:56px auto;
                padding:0 20px;
                text-align:center;
            }
            .card{
                background:#fff;
                border:1px solid #e2e8f0;
                border-radius:16px;
                padding:32px 28px;
                box-shadow:0 4px 16px -4px rgba(15,23,42,0.06);
            }
            h2{
                font-family:'Outfit',sans-serif;
                font-weight:800;
                font-size:1.375rem;
                color:#0f172a;
                margin:0 0 12px;
            }
            p{ line-height:1.6; margin:0 0 12px; color:#475569 }
            a.back-link{
                display:inline-block;
                margin-top:8px;
                color:#0891b2;
                font-weight:600;
                text-decoration:none;
            }
            a.back-link:hover{ color:#be1c3a }
            button{
                font-family:'Outfit',sans-serif;
                padding:10px 24px;
                font-size:15px;
                font-weight:700;
                color:#fff;
                background:#0891b2;
                border:none;
                border-radius:10px;
                cursor:pointer;
                margin-top:8px;
            }
            button:hover{ background:#0e7490 }
            ul.newsletter-list{
                list-style:none;
                margin:16px 0;
                padding:0;
                display:flex;
                flex-direction:column;
                gap:6px;
                text-align:left;
            }
            ul.newsletter-list li{
                display:flex;
                align-items:center;
                gap:10px;
                padding:10px 14px;
                background:#f8fafc;
                border:1px solid #e2e8f0;
                border-radius:10px;
                font-weight:600;
                font-size:0.9rem;
                color:#0f172a;
            }
            ul.newsletter-list li::before{
                content:'';
                width:6px;
                height:6px;
                min-width:6px;
                border-radius:50%;
                background:#0891b2;
            }
            """;

        private const string GoogleFontsLink =
            """<link rel="preconnect" href="https://fonts.googleapis.com"><link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700&family=Outfit:wght@700;800;900&display=swap" rel="stylesheet">""";

        private static string PageShell(string title, string bodyHtml) => $"""
            <!DOCTYPE html>
            <html lang="tr">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
                <title>{title}</title>
                {GoogleFontsLink}
                <style>{ConfirmationPageStyle}</style>
            </head>
            <body>
                <header class="brand-header">
                    <a href="/">AÇIK<span>İSTİHBARAT</span></a>
                </header>
                <div class="page-content">
                    <div class="card">
                        {bodyHtml}
                    </div>
                </div>
            </body>
            </html>
            """;

        [HttpGet("unsubscribe")]
        public IActionResult UnsubscribeConfirm([FromQuery] Guid token)
        {
            // Does NOT mutate state - always renders the same neutral confirmation page regardless
            // of whether the token is valid, both to avoid leaking token validity (minor
            // enumeration hardening) and so automated mail-security link-prefetchers (which GET
            // every link in an inbound email before a human opens it) can't silently trigger an
            // unsubscribe just by fetching this page.
            var body = $"""
                <h2>Bültenden çıkmak istediğinize emin misiniz?</h2>
                <form method="post" action="/api/public/mail/unsubscribe">
                    <input type="hidden" name="token" value="{token}" />
                    <button type="submit">Bültenden çık</button>
                </form>
                """;
            return Content(PageShell("Bültenden çık", body), "text/html");
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

            var body = "<h2>Aboneliğiniz İptal Edildi</h2>";
            return Content(PageShell("Aboneliğiniz İptal Edildi", body), "text/html");
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
            // Confirms immediately on GET (single click from the email button, no second
            // "Aboneliği Onayla" button on a landing page) rather than requiring a follow-up POST.
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
        }

        private string BuildConfirmationLandingHtml(
            LandingCase landingCase,
            List<string> confirmedDisplayNames,
            List<string> expiredDisplayNames)
        {
            var siteUrl = _mailOptions.PublicSiteBaseUrl;
            string heading, body;
            switch (landingCase)
            {
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
                    heading = "Aboneliğiniz Başlatıldı";
                    body = "<p>Yarın sabahtan itibaren her sabah bültenlerinizi posta kutunuza yolluyor olacağız.</p>"
                        + BuildList(confirmedDisplayNames);
                    if (expiredDisplayNames.Count > 0)
                    {
                        body += "<p>Şu bültenler için onay süresi dolmuş, tekrar abone olmanız gerekiyor:</p>" + BuildList(expiredDisplayNames);
                    }
                    break;
            }

            var innerHtml = $"""
                <h2>{heading}</h2>
                {body}
                <p><a class="back-link" href="{siteUrl}/acikmedya/AcikGazete">Bültenlere dön</a></p>
                """;
            return PageShell(heading, innerHtml);
        }

        private static string BuildList(List<string> names) =>
            names.Count == 0 ? "" : "<ul class=\"newsletter-list\">" + string.Join("", names.Select(n => $"<li>{System.Net.WebUtility.HtmlEncode(n)}</li>")) + "</ul>";
    }
}
