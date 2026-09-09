# Architecture and Implementation Plan: AcikMedya Newsletter Subscription UX

**Status:** Reviewed — incorporates Evaluator findings (see "Evaluation Findings Incorporated" section at the end).

## Summary
Add an accented active-tab state to the `/acikmedya` newsletter tabs, and replace the
non-functional email input in `AcikMedyaLayout` with a working subscribe flow: clicking
"Abone Ol" opens a Preline modal listing newsletters (checkboxes), submitting creates
inactive `MailSubscriber` rows per selected newsletter, queues ONE confirmation email with
a single shared-token link (sent asynchronously, not on the request thread), and shows a
summary modal telling the user to check their email. Clicking the confirmation link lands
on a friendly HTML page that first asks the user to confirm (GET, non-mutating — mirrors
the existing unsubscribe anti-prefetch pattern), and only on that follow-up POST flips all
rows tied to that token to `IsActive = true`, at which point the existing
`MailingOrchestrator` (unchanged) will include them in real sends.

## Decisions (confirmed across session)
- Active tab: styled like `Header.tsx`'s `Ana Sayfa` link (bottom border + accent color +
  `aria-current="page"`), driven by passing the current folder into `AcikMedyaLayout`.
- Popup opens only on "Abone Ol" click (not on typing/blur). Email format validated at
  final submit inside the popup, not before opening it.
- Display names: `AcikGazete` -> "Açık Gazete Özetleri", `AcikKose` -> "Açık Köşe
  Yazarları" — hardcoded label map, duplicated on both frontend and backend (see Phase 4.1
  and Phase 2.0). **Known drift risk, tracked as a TODO** (see `TODO-AcikIstihbarat.md`,
  "Unify newsletter display name source of truth").
- Checkboxes unchecked by default (opt-in).
- Duplicate/pending email handling: process each selected newsletter independently,
  partial success with itemized per-item errors (HTTP 200 always, not all-or-nothing).
- Confirmation token: shared across all rows created in one submission (one link
  confirms the whole batch); `ConfirmToken` is NOT globally unique — non-unique index
  only. Expires after 3 days. Expired rows are NOT deleted; the confirm endpoint just
  refuses to activate them; user must resubmit the popup for a fresh token.
- Confirmation email reuses the existing Gmail OAuth2 SMTP pipeline (same
  connect/authenticate pattern as `MailingOrchestrator.RunScheduleAsync`), but is queued
  and sent by the existing background poller rather than inline on the HTTP request
  (see Evaluation Finding #1 below).
- Popup UI built with Preline's modal (`data-hs-overlay`).
- Confirmation landing page uses a GET-render / POST-mutate split (mirrors the existing
  `UnsubscribeConfirm`/`UnsubscribeConfirmed` pattern in `MailController.cs`), with 3
  possible outcome cases after the POST: invalid token / all-expired / success-with-
  greeting-and-list, mirroring the existing `ConfirmationPageStyle` pattern in
  `MailController.cs`.

## Confirmed existing conventions to reuse (verified in this session, not assumptions)
- Frontend API calls go through `lib/api.ts`'s `fetchApi<T>(endpoint, options)`, which
  prefixes `endpoint` with `NEXT_PUBLIC_API_URL` (client-side) or `INTERNAL_API_URL`
  (server-side), both of which already resolve to `.../api/public` — so calling
  `fetchApi('/mail/subscribe', { method: 'POST', body: JSON.stringify(...) })` correctly
  hits `MailController`'s `api/public/mail/subscribe` route with zero new env vars.
- `appsettings.json`'s `Mail` section currently has: `TemplatesDataDir`,
  `PublicApiBaseUrl` (= `https://api.acikistihbarat.com`, the API's own origin — NOT the
  public Next.js site), `PollIntervalSeconds`, `DryRun`, `GmailOAuth.*`, `Guardrails.*`.
  There is NO existing key for the public Next.js site's origin — Phase 1.0 adds one.
- `docker-compose.yml`'s `api` service already injects `Mail__GmailOAuth__ClientId/
  ClientSecret/RefreshToken/SenderAddress` from `.env`-backed variables (lines ~38-41).
  The `public` service gets `NEXT_PUBLIC_API_URL=https://api.acikistihbarat.com/api/public`
  (build arg + runtime env, lines ~55/61) and `INTERNAL_API_URL=http://api:8080/api/public`
  (line 63) — confirms the public site's real external origin is
  `https://acikistihbarat.com` (same domain family, `api.` subdomain is the API).
- `MailingOrchestrator`/`MailSchedulerBackgroundService` already implements a polling
  background loop that picks up due work and sends via SMTP — this feature's async email
  queue (Finding #1's fix) rides on the same poller rather than introducing a second
  background mechanism.
- `MailController.UnsubscribeConfirm` (GET, renders a form) / `UnsubscribeConfirmed`
  (POST, mutates) is the established two-step pattern this feature's confirm endpoint
  now follows (Finding #3's fix), specifically to defend against automated mail-security
  link-prefetchers.

---

## Phase 0 — Backend data model & migration *(foundation, do first, ~30 min)*

0.1. Open `AcikIstihbarat.API/Models/Entities/MailSubscriber.cs`. Add exactly these two
     properties after `UnsubscribeToken`:
     ```csharp
     public Guid ConfirmToken { get; set; }
     public DateTime ConfirmTokenExpiresAt { get; set; }
     ```
     Do NOT add `[Required]`/default-value attributes here — values are always set
     explicitly at row-creation time in the controller (Phase 2), so entity-level
     defaults would just mask bugs if the controller ever forgets to set them.

0.2. Open `AcikIstihbarat.API/Data/AppDbContext.cs`. In `OnModelCreating`, immediately
     after the existing `MailSubscriber` index configuration block, add:
     ```csharp
     builder.Entity<MailSubscriber>().HasIndex(s => s.ConfirmToken);
     ```
     (Deliberately non-unique — do not chain `.IsUnique()`.) Leave the 3 existing
     `MailSubscriber` indexes (`Email+TemplateBaseName` unique, `UnsubscribeToken`
     unique, `TemplateBaseName+IsActive`) untouched.

0.3. Terminal, from `AcikIstihbarat.API/`:
     ```
     dotnet ef migrations add AddSubscriberConfirmation
     ```
     Open the generated `Migrations/<timestamp>_AddSubscriberConfirmation.cs` and verify
     `Up()` contains exactly:
     - `AddColumn<Guid>(name: "ConfirmToken", table: "MailSubscribers", nullable: false, defaultValue: Guid.Empty)`
       — EF will emit a `defaultValue`, not `defaultValueSql`, since no default was set
       on the CLR property; this is fine as a one-time backfill value for any pre-existing
       rows (there should be none in dev, see next bullet), but MUST be reviewed — if it
       emits `Guid.Empty` in the migration, that's acceptable since it's a one-time
       migration-time backfill, not a runtime default.
     - `AddColumn<DateTime>(name: "ConfirmTokenExpiresAt", table: "MailSubscribers", nullable: false, defaultValue: <some literal>)`
       — same reasoning.
     - `CreateIndex(name: "IX_MailSubscribers_ConfirmToken", table: "MailSubscribers", column: "ConfirmToken")`
       (no `unique: true` argument).
     - No other tables/columns touched (if drift appears, something else in
       `OnModelCreating` changed unintentionally — stop and investigate before applying).

0.4. Before applying, run against the dev DB:
     ```sql
     SELECT COUNT(*) FROM MailSubscribers;
     ```
     If 0 rows (expected — no subscribe endpoint has ever existed to populate this table
     until this feature), the backfill default value is irrelevant. If non-zero, decide
     whether those pre-existing rows should be treated as already-confirmed
     (`IsActive` presumably already `true` for them) — in that case their
     `ConfirmTokenExpiresAt` value doesn't matter since they'll never hit the confirm
     endpoint again; no special handling needed either way.

0.5. Apply: `dotnet ef database update`. Verify:
     ```sql
     SELECT TOP 5 Email, TemplateBaseName, IsActive, ConfirmToken, ConfirmTokenExpiresAt
     FROM MailSubscribers;
     ```
     runs without error (column-exists check), independent of row count.

0.6. **New (from Finding #1 — async send)**: Add a small outbox table to support queued
     confirmation emails without a second background mechanism. New entity
     `AcikIstihbarat.API/Models/Entities/PendingConfirmationEmail.cs`:
     ```csharp
     public class PendingConfirmationEmail
     {
         public int Id { get; set; }
         public string Email { get; set; } = string.Empty;
         public Guid ConfirmToken { get; set; }
         public string TemplateDisplayNamesCsv { get; set; } = string.Empty; // comma-joined, already display-mapped
         public DateTime CreatedAt { get; set; }
         public DateTime? SentAt { get; set; }
         public int AttemptCount { get; set; }
         public string? LastError { get; set; }
     }
     ```
     Register `DbSet<PendingConfirmationEmail> PendingConfirmationEmails` in
     `AppDbContext`, with a non-unique index on `SentAt` (to efficiently query
     "unsent rows") added in `OnModelCreating`. This becomes part of the same
     `AddSubscriberConfirmation` migration (regenerate/extend it before applying in 0.3-0.5
     rather than creating a second migration).

---

## Phase 1 — Backend config: PublicSiteBaseUrl *(independent, do alongside Phase 0)*

1.1. Open `AcikIstihbarat.API/Models/DTOs/MailOptions.cs`. Add one property to the
     `MailOptions` class (not `GmailOAuthOptions`):
     ```csharp
     public string PublicSiteBaseUrl { get; set; } = string.Empty;
     ```
     Place it directly under `PublicApiBaseUrl` for readability (they're the two
     different origins used by this feature: API origin for the confirm link itself,
     site origin for the "back to site" links on the landing page).

1.2. Open `AcikIstihbarat.API/appsettings.json`, inside the `"Mail"` object, add:
     ```json
     "PublicSiteBaseUrl": "https://acikistihbarat.com",
     ```
     (placed next to the existing `"PublicApiBaseUrl": "https://api.acikistihbarat.com"`).

1.3. Open `AcikIstihbarat.API/appsettings.Development.json` and add the same key with a
     local dev value, e.g. `"PublicSiteBaseUrl": "http://localhost:3000"` (confirm the
     Next.js dev port matches `acik-istihbarat-public/package.json`'s `dev` script /
     `next.config.ts` — check before finalizing; default Next.js dev port is 3000 unless
     overridden).

1.4. `docker-compose.yml` does not need a new env var for this — `appsettings.json`'s
     hardcoded production value is sufficient since this isn't a secret (matches how
     `PublicApiBaseUrl` itself is hardcoded in `appsettings.json` rather than injected via
     compose). No `docker-compose.yml` change needed for this key.

---

## Phase 2 — Backend: DTOs, display-name map, subscribe endpoint *(depends on Phase 0-1)*

2.0. Add a small server-side display-name lookup, new file
     `AcikIstihbarat.API/Helpers/NewsletterDisplayNames.cs`:
     ```csharp
     // Keep in lockstep with acik-istihbarat-public/lib/newsletterLabels.ts
     // Known drift risk — tracked in ProjectImplementationDocs/TODO-AcikIstihbarat.md
     // ("Unify newsletter display name source of truth").
     namespace AcikIstihbarat.API.Helpers
     {
         public static class NewsletterDisplayNames
         {
             private static readonly Dictionary<string, string> Labels = new()
             {
                 ["AcikGazete"] = "Açık Gazete Özetleri",
                 ["AcikKose"] = "Açık Köşe Yazarları",
             };

             public static string Resolve(string templateBaseName) =>
                 Labels.TryGetValue(templateBaseName, out var label) ? label : templateBaseName;
         }
     }
     ```
     This is the deliberate backend duplicate of the frontend's
     `lib/newsletterLabels.ts` map (Phase 4.1) — both must be updated in lockstep
     whenever a newsletter folder is added/renamed; flag this in a code comment in both
     files pointing at each other by relative path.

2.1. Add DTOs, new file `AcikIstihbarat.API/Models/DTOs/NewsletterSubscribeDtos.cs`:
     ```csharp
     namespace AcikIstihbarat.API.Models.DTOs
     {
         public class SubscribeRequest
         {
             public string Email { get; set; } = string.Empty;
             public List<string> TemplateBaseNames { get; set; } = new();
         }

         public class SubscribeResultItem
         {
             public string TemplateBaseName { get; set; } = string.Empty;
             public bool Success { get; set; }
             public string? ErrorMessage { get; set; }
         }

         public class SubscribeResponse
         {
             public List<SubscribeResultItem> Results { get; set; } = new();
         }
     }
     ```

2.2. Determine the trusted template-name allow-list (resolves Further Consideration #3
     from prior iteration): query `_db.MailSchedules.Select(s => s.TemplateBaseName).Distinct()`
     at request time inside the controller action (simplest — no new config/file needed,
     always in sync with whatever schedules actually exist; a template with no
     `MailSchedule` row can't be meaningfully subscribed to anyway since it'll never be
     sent). If this returns empty in a fresh/dev DB before any `MailSchedule` rows exist,
     document that as a known limitation for local testing — seed at least one
     `MailSchedule` row per `acikmedya-newsletters.json` folder before testing (this
     should already exist per the mailing-engine feature's own setup, not new work here).

2.3. In `AcikIstihbarat.API/Controllers/Public/MailController.cs`:
     - Add constructor dependencies: `IOptions<MailOptions> mailOptions`,
       `ILogger<MailController> logger` (in addition to the existing `AppDbContext db`).
       **Note (Finding #1 fix): `IGmailOAuthTokenProvider`/`IMailSenderService` are NOT
       needed in this controller anymore** — the confirmation email is no longer sent
       synchronously from the request path; it's queued as a `PendingConfirmationEmail`
       row and picked up by the existing background poller (Phase 2.4). Store as
       `_mailOptions`, `_logger` (matches the naming convention already used in
       `MailingOrchestrator.cs`).
     - Add `[HttpPost("subscribe")]` action, signature:
       ```csharp
       public async Task<ActionResult<SubscribeResponse>> Subscribe(
           [FromBody] SubscribeRequest request, CancellationToken ct)
       ```
     - **Input validation** (return `400 BadRequest` with a plain string message for
       these — they're client bugs, not user-facing form errors):
       - `request.Email` non-empty and `System.Net.Mail.MailAddress` can parse it:
         ```csharp
         if (!System.Net.Mail.MailAddress.TryCreate(request.Email, out _))
             return BadRequest("Invalid email address.");
         ```
       - `request.TemplateBaseNames` non-empty list.
       - Every entry in `request.TemplateBaseNames` exists in the Phase 2.2 allow-list;
         silently drop unknown entries rather than erroring the whole request (defensive
         against stale frontend caches sending a since-removed folder name) — but if
         ALL entries get dropped this way, return `400 BadRequest("No valid newsletters selected.")`.
     - **Batch token generation**: `var confirmToken = Guid.NewGuid(); var expiresAt = DateTime.UtcNow.AddDays(3);`
       generated ONCE per request, before the per-template loop.
     - **Per-template loop** — for each valid `templateBaseName`:
       ```csharp
       var existing = await _db.MailSubscribers.FirstOrDefaultAsync(
           s => s.Email == request.Email && s.TemplateBaseName == templateBaseName, ct);
       ```
       - `existing is not null && existing.IsActive` -> `results.Add(new SubscribeResultItem { TemplateBaseName = templateBaseName, Success = false, ErrorMessage = "Bu bültene zaten abonesiniz." })`.
       - `existing is not null && !existing.IsActive && existing.ConfirmTokenExpiresAt > DateTime.UtcNow` -> `Success = false, ErrorMessage = "Bu bülten için onay bekleniyor, e-postanızı kontrol edin."`.
       - `existing is not null && !existing.IsActive && existing.ConfirmTokenExpiresAt <= DateTime.UtcNow` -> reuse row: set `existing.ConfirmToken = confirmToken; existing.ConfirmTokenExpiresAt = expiresAt;` -> `Success = true`, add `templateBaseName` to a local `confirmedBatchNames` list (used for the email body).
       - `existing is null` -> `_db.MailSubscribers.Add(new MailSubscriber { Email = request.Email, TemplateBaseName = templateBaseName, IsActive = false, ConfirmToken = confirmToken, ConfirmTokenExpiresAt = expiresAt, UnsubscribeToken = Guid.NewGuid(), CreatedAt = DateTime.UtcNow });` -> `Success = true`, add to `confirmedBatchNames`.
     - **Race-condition-safe save (Finding #2 fix)**: wrap `await _db.SaveChangesAsync(ct);`
       in a `try/catch (DbUpdateException)`. On catch:
       1. Re-query the current state of every row this request attempted to touch
          (`Where(s => s.Email == request.Email && templateBaseNames.Contains(s.TemplateBaseName))`).
       2. For each `templateBaseName` whose row now exists but wasn't part of what THIS
          request actually persisted (i.e. a concurrent request won the race), re-classify
          its `SubscribeResultItem` as `Success = false` with the same "already
          pending"/"already subscribed" messages used in the normal per-item branches
          above (reuse the same classification logic as a small local helper function so
          it isn't duplicated).
       3. Retry `SaveChangesAsync` once for any rows that are still genuinely new/owned by
          this request after re-classification; if it fails again, log via `_logger` and
          treat remaining unresolved items as failed with a generic
          "Bu bülten için kayıt oluşturulamadı, lütfen tekrar deneyin." message — do NOT
          let a second exception propagate to a 500 for the whole batch.
       This guarantees `Subscribe` never returns an unhandled 500 due to the unique
       index on `(Email, TemplateBaseName)`, and preserves per-item partial success even
       under concurrent duplicate submissions.
     - If `confirmedBatchNames.Count > 0`, **queue** the confirmation email instead of
       sending it inline (Finding #1 fix): insert one
       `PendingConfirmationEmail { Email = request.Email, ConfirmToken = confirmToken,
       TemplateDisplayNamesCsv = string.Join(",", confirmedBatchNames.Select(NewsletterDisplayNames.Resolve)),
       CreatedAt = DateTime.UtcNow }` row as part of the SAME `SaveChangesAsync` call
       used for the `MailSubscriber` rows (single DB round-trip, and guarantees the
       outbox row and subscriber rows are transactionally consistent — both committed or
       neither).
     - Return `Ok(new SubscribeResponse { Results = results })` on the normal path. There
       is no more inline-SMTP-failure 500 case (Finding #1 removes that failure mode from
       the request path entirely — send failures are now handled by the background
       poller's own retry/guardrail logic, matching how `MailingOrchestrator` already
       handles transient SMTP failures for scheduled sends).

2.4. **Queued confirmation-email sending (replaces prior inline-send design per Finding #1)**:
     - Extend the existing `MailSchedulerBackgroundService` poll loop (or add a small
       sibling `BackgroundService`, `ConfirmationEmailDispatcherBackgroundService`, if
       keeping concerns cleanly separated is preferred — either is acceptable, but do NOT
       introduce a second polling *mechanism*; reuse the same interval/cancellation-token
       plumbing already established) to, each cycle:
       1. Query up to N (e.g. 20) `PendingConfirmationEmails` where `SentAt == null`,
          oldest `CreatedAt` first.
       2. For each, open the SMTP client using the identical pattern already present in
          `MailingOrchestrator.RunScheduleAsync` (copy the 4 lines: `new SmtpClient()`,
          `ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, ct)`,
          `_tokenProvider.GetAccessTokenAsync(ct)`,
          `AuthenticateAsync(new SaslMechanismOAuth2(_mailOptions.GmailOAuth.SenderAddress, accessToken), ct)`),
          call `_mailSender.SendConfirmationAsync(client, row.Email, row.ConfirmToken, row.TemplateDisplayNamesCsv.Split(','), ct)`,
          then `await client.DisconnectAsync(true, ct)` in a `finally`.
       3. On success: set `row.SentAt = DateTime.UtcNow`. On failure: increment
          `row.AttemptCount`, set `row.LastError = ex.Message`; if `AttemptCount` exceeds
          a small cap (e.g. 5), stop retrying that row and log a warning (the
          `MailSubscriber` rows themselves stay pending — the user can always resubmit
          the popup for a fresh token/email if their original confirmation email never
          arrives).
       4. `SaveChangesAsync` after each row (or batch at end of cycle — matches whatever
          granularity `MailingOrchestrator` itself already uses for its own per-subscriber
          loop, for consistency).
     - `IMailSenderService` (interface file `Services/IMailSenderService.cs`): add
       ```csharp
       Task SendConfirmationAsync(MailKit.Net.Smtp.SmtpClient client, string toEmail,
           Guid confirmToken, IReadOnlyList<string> templateDisplayNames, CancellationToken ct = default);
       ```
     - `MailSenderService.cs`: implement it, building the confirm URL as
       `$"{_mailOptions.PublicApiBaseUrl}/api/public/mail/confirm?token={confirmToken}"`
       (uses `PublicApiBaseUrl`, NOT the new `PublicSiteBaseUrl` — this link must hit the
       API, not the Next.js site). Compose a minimal Turkish HTML body (subject e.g.
       "Bülten aboneliğinizi onaylayın") listing `templateDisplayNames` as a `<ul>`, with
       an `<a href="{confirmUrl}">Aboneliği Onayla</a>` button-style link. No
       `List-Unsubscribe` header needed here (this isn't a bulk/marketing send in the
       CAN-SPAM sense — it's a single transactional double-opt-in email), and no
       unsubscribe footer injection (reuse `PreMailer.Net` inlining only if the template
       has inline-unfriendly CSS — for this simple hardcoded HTML it's optional/skippable).
     - **Respect `_mailOptions.DryRun`**: if `true`, skip the real SMTP connect/send
       entirely and instead `_logger.LogInformation("[DRYRUN] would send confirmation to {Email} for {Names}", row.Email, row.TemplateDisplayNamesCsv)`, and still mark `SentAt` so
       dry-run testing doesn't loop forever re-processing the same row.
       This lets Phase 6 verification run without real Gmail credentials configured
       locally. Note the dry-run flow now has an extra hop (poller must run at least once
       after `Subscribe` returns) compared to the original inline-send design — call this
       out explicitly during Phase 6 manual testing so it isn't mistaken for a bug.

---

## Phase 3 — Backend: confirm endpoint & landing page *(depends on Phase 0-1, parallel with Phase 2)*

3.1. **GET/POST split (Finding #3 fix)** — mirrors `UnsubscribeConfirm`/`UnsubscribeConfirmed`
     exactly. Add TWO actions to `MailController.cs`:

     - `[HttpGet("confirm")]` — **non-mutating**, renders a confirmation form:
       ```csharp
       public async Task<IActionResult> ConfirmPrompt([FromQuery] Guid token, CancellationToken ct)
       ```
       - `var matches = await _db.MailSubscribers.Where(s => s.ConfirmToken == token).ToListAsync(ct);`
       - `if (matches.Count == 0) return Content(BuildConfirmationLandingHtml(LandingCase.Invalid, [], []), "text/html");`
       - Split into `pending` (not yet active, not expired) and `expired` lists based on
         `ConfirmTokenExpiresAt` vs `DateTime.UtcNow` — do NOT mutate `IsActive` here.
       - If `pending.Count == 0` (all expired or all already active), render
         `LandingCase.AllExpired` (or a dedicated "already confirmed" case if all matches
         are already `IsActive == true` — treat that as informational, not an error).
       - Otherwise render a page with a `<form method="post" action="{PublicApiBaseUrl}/api/public/mail/confirm?token={token}">`
         containing a single "Aboneliği Onayla" submit button, listing the pending
         display names for the user to review before clicking (same anti-prefetch
         rationale as the existing unsubscribe flow: automated link-scanners GET this
         page harmlessly; only a real submitted POST activates anything).

     - `[HttpPost("confirm")]` — **mutating**, actual activation:
       ```csharp
       public async Task<IActionResult> ConfirmSubmit([FromQuery] Guid token, CancellationToken ct)
       ```
       - Same query/split logic as above, but this time actually mutate:
         ```csharp
         var confirmedNames = new List<string>();
         var expiredNames = new List<string>();
         var now = DateTime.UtcNow;
         foreach (var s in matches)
         {
             if (s.ConfirmTokenExpiresAt < now) { expiredNames.Add(s.TemplateBaseName); continue; }
             s.IsActive = true;
             confirmedNames.Add(s.TemplateBaseName);
         }
         await _db.SaveChangesAsync(ct);
         ```
       - `var landingCase = confirmedNames.Count > 0 ? LandingCase.Success : LandingCase.AllExpired;`
       - `return Content(BuildConfirmationLandingHtml(landingCase, confirmedNames.Select(NewsletterDisplayNames.Resolve).ToList(), expiredNames.Select(NewsletterDisplayNames.Resolve).ToList()), "text/html");`

3.2. Add a private enum + static HTML-builder method in `MailController.cs` (mirrors the
     existing `ConfirmationPageStyle` constant already in the file):
     ```csharp
     private enum LandingCase { Invalid, AllExpired, Success, PendingPrompt }

     private string BuildConfirmationLandingHtml(
         LandingCase landingCase, List<string> confirmedDisplayNames, List<string> expiredDisplayNames,
         string? formActionUrl = null)
     {
         var siteUrl = _mailOptions.PublicSiteBaseUrl;
         string heading, body;
         switch (landingCase)
         {
             case LandingCase.PendingPrompt:
                 heading = "Aboneliğinizi onaylayın";
                 body = "<p>Aşağıdaki bültenlere aboneliğinizi onaylamak için butona tıklayın:</p>"
                     + BuildList(confirmedDisplayNames)
                     + $"""<form method="post" action="{formActionUrl}"><button type="submit">Aboneliği Onayla</button></form>""";
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
     ```
     Note the `HtmlEncode` on list items — display names are currently hardcoded/trusted,
     but encode anyway as defense-in-depth since this renders untrusted-adjacent data
     (`TemplateBaseName` ultimately traces back through user-submitted request data even
     though it's allow-list-validated in 2.2).
     Route target `/acikmedya/AcikGazete` is a pragmatic default (no true "all
     newsletters" index route exists in the frontend today) — a bare `/acikmedya` would
     also 404 today unless a redirect is added; keep this hardcoded link but flag it for
     a follow-up if a dedicated `/acikmedya` index page is ever added.

---

## Phase 4 — Frontend: display-name map & tab accent *(independent, parallel with Phase 2-3)*

4.1. New file `acik-istihbarat-public/lib/newsletterLabels.ts`:
     ```ts
     // Keep in lockstep with AcikIstihbarat.API/Helpers/NewsletterDisplayNames.cs
     // Known drift risk — tracked in ProjectImplementationDocs/TODO-AcikIstihbarat.md
     // ("Unify newsletter display name source of truth").
     export const NEWSLETTER_LABELS: Record<string, string> = {
       AcikGazete: 'Açık Gazete Özetleri',
       AcikKose: 'Açık Köşe Yazarları',
     };

     export function resolveNewsletterLabel(folder: string): string {
       return NEWSLETTER_LABELS[folder] ?? folder;
     }
     ```

4.2. `acik-istihbarat-public/app/acikmedya/[folder]/page.tsx`: change
     ```tsx
     <AcikMedyaLayout>
     ```
     to
     ```tsx
     <AcikMedyaLayout activeFolder={folder}>
     ```

4.3. `acik-istihbarat-public/components/AcikMedyaLayout.tsx`:
     - Change signature to `export default async function AcikMedyaLayout({ children, activeFolder }: { children: React.ReactNode; activeFolder: string })`.
     - Import `resolveNewsletterLabel` from `@/lib/newsletterLabels`.
     - Replace the folders `.map` block:
       ```tsx
       {folders.map((folder) => {
         const isActive = folder === activeFolder;
         return (
           <Link
             key={folder}
             href={`/acikmedya/${folder}`}
             aria-current={isActive ? 'page' : undefined}
             className={
               isActive
                 ? 'font-heading font-semibold text-turquoise-600 dark:text-turquoise-400 py-1 border-b-2 border-turquoise-600 dark:border-turquoise-400 transition-colors duration-300'
                 : 'font-heading font-medium text-slate-600 hover:text-turquoise-600 dark:text-slate-400 dark:hover:text-turquoise-400 transition-colors duration-300'
             }
           >
             {resolveNewsletterLabel(folder)}
           </Link>
         );
       })}
       ```

---

## Phase 5 — Frontend: subscribe popup + summary popup *(depends on Phase 2-4; needs Preline)*

5.1. New file `acik-istihbarat-public/components/NewsletterSubscribeForm.tsx`,
     `'use client'`. Props: `{ folders: string[] }`.

5.2. Local types (co-located or imported from a shared `types/index.ts` addition):
     ```ts
     type SubscribeResultItem = { templateBaseName: string; success: boolean; errorMessage: string | null };
     type SubscribeResponse = { results: SubscribeResultItem[] };
     ```

5.3. State:
     ```ts
     const [email, setEmail] = useState('');
     const [emailError, setEmailError] = useState<string | null>(null);
     const [selected, setSelected] = useState<Set<string>>(new Set());
     const [selectionError, setSelectionError] = useState<string | null>(null);
     const [submitError, setSubmitError] = useState<string | null>(null);
     const [submitting, setSubmitting] = useState(false);
     const [itemErrors, setItemErrors] = useState<SubscribeResultItem[]>([]);
     const [successNames, setSuccessNames] = useState<string[]>([]);
     ```
     Two Preline modals rendered unconditionally in JSX (Preline shows/hides via its own
     `data-hs-overlay` toggle attributes/classes, not React conditional rendering) with
     fixed DOM ids: `#newsletter-select-modal`, `#newsletter-summary-modal`.

5.4. Header "Abone Ol" button (`type="button"`, not `submit` — no native form submit
     needed since this is fully client-driven):
     ```ts
     function handleOpenClick() {
       const isValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
       if (!isValid) { setEmailError('Geçerli bir e-posta adresi girin.'); return; }
       setEmailError(null);
       // trigger Preline's overlay open programmatically via HSOverlay.open(...)
       // or simply toggle a `data-hs-overlay` attribute click on a hidden trigger —
       // confirm exact Preline API call against the version already vendored in this
       // project (check node_modules/preline or the existing usage in any other
       // component using `data-hs-overlay` for the exact open/close call convention
       // before writing this, since PrelineScript.tsx only auto-inits on route change).
     }
     ```
     **Open item to verify during implementation**: this codebase's only existing
     Preline usage found so far is `hs-collapse-toggle` (mobile nav, `Header.tsx`), not a
     modal/overlay — there is no existing `data-hs-overlay` example to copy from in this
     repo. Confirm Preline's JS build here actually includes the Overlay plugin (check
     `PrelineScript.tsx`'s import and whatever `preline` package version is in
     `package.json`) before committing to this approach; if the overlay plugin isn't
     bundled, fall back to a plain React-state-controlled modal (fixed-position div +
     backdrop) instead — same visual Tailwind classes, just conditionally rendered.

5.5. Select-newsletters modal body: for each folder, a checkbox bound to `selected`:
     ```tsx
     <label key={folder} className="flex items-center gap-2">
       <input
         type="checkbox"
         checked={selected.has(folder)}
         onChange={(e) => setSelected((prev) => {
           const next = new Set(prev);
           e.target.checked ? next.add(folder) : next.delete(folder);
           return next;
         })}
       />
       {resolveNewsletterLabel(folder)}
     </label>
     ```
     Modal's own "Abone Ol" submit button calls `handleSubscribeSubmit`:
     ```ts
     async function handleSubscribeSubmit() {
       if (selected.size === 0) { setSelectionError('En az bir bülten seçin.'); return; }
       setSelectionError(null);
       setSubmitError(null);
       setSubmitting(true);
       try {
         const res = await fetchApi<SubscribeResponse>('/mail/subscribe', {
           method: 'POST',
           body: JSON.stringify({ email, templateBaseNames: [...selected] }),
         });
         const succeeded = res.results.filter((r) => r.success).map((r) => r.templateBaseName);
         const failed = res.results.filter((r) => !r.success);
         setSuccessNames(succeeded.map(resolveNewsletterLabel));
         setItemErrors(failed);
         // close select modal, open summary modal (via same Preline/plain-modal mechanism as 5.4)
       } catch (e) {
         setSubmitError('Bir hata oluştu, lütfen tekrar deneyin.');
       } finally {
         setSubmitting(false);
       }
     }
     ```
     Note: `fetchApi` throws on non-2xx (per `lib/api.ts`'s existing behavior). With the
     Finding #1 fix, `Subscribe` no longer has an inline-SMTP-failure 500 path — the only
     non-2xx cases left are the `400` validation errors above, which is a simpler error
     surface for the frontend to handle than before.

5.6. Summary modal body: if `successNames.length > 0`, "E-postanıza gönderilen onay
     bağlantısına tıklayın." + `<ul>` of `successNames`; if `itemErrors.length > 0`,
     additionally list each as "{display name}: {errorMessage}". Single "Tamam" button
     closes the modal (resets `selected`/`email` state for a clean next-open).

5.7. `AcikMedyaLayout.tsx`: replace the existing inline `<form>` block (email input +
     submit button) with `<NewsletterSubscribeForm folders={folders} />`.

---

## Phase 6 — Verification

6.1. Backend — `dotnet build` in `AcikIstihbarat.API/` must be clean. Then, with
     `Mail:DryRun` forced `true` in `appsettings.Development.json` (temporarily, if not
     already), run the API and use `AcikIstihbarat.API.http` (add new request blocks
     there matching its existing style) or Swagger:
     - `POST /api/public/mail/subscribe` with
       `{ "email": "test@example.com", "templateBaseNames": ["AcikGazete", "AcikKose"] }`
       -> `200`, both `results[].success == true`;
       `SELECT * FROM MailSubscribers WHERE Email = 'test@example.com'` shows 2 rows,
       `IsActive = 0`, same `ConfirmToken` value on both rows; also
       `SELECT * FROM PendingConfirmationEmails WHERE Email = 'test@example.com'` shows
       1 unsent row.
     - Wait for (or manually trigger) one poller cycle -> server log shows the
       `[DRYRUN]` line; `PendingConfirmationEmails.SentAt` is now non-null for that row.
     - Repeat the exact same `POST /subscribe` request again -> both items now come back
       `success: false, errorMessage: "Bu bülten için onay bekleniyor..."` (still pending,
       not yet expired).
     - `GET /api/public/mail/confirm?token=<the ConfirmToken from the DB>` -> browser
       shows a non-mutating "confirm your subscription" prompt page with a submit button;
       re-run the `SELECT` on `MailSubscribers` -> still `IsActive = 0` (confirms GET does
       not mutate).
     - `POST /api/public/mail/confirm?token=<same token>` (simulating the prompt page's
       form submit) -> Case-Success landing page listing both display names; re-run the
       `SELECT` -> both rows now `IsActive = 1`.
     - `GET /api/public/mail/confirm?token=00000000-0000-0000-0000-000000000000` (never
       issued) -> Case-Invalid landing page, no 500.
     - Concurrency check (Finding #2): fire two near-simultaneous
       `POST /mail/subscribe` requests for the same new email + same template (e.g. via
       two terminal `curl`/Postman "Send" clicks in quick succession) -> confirm neither
       returns a 500; exactly one `MailSubscriber` row exists afterward for that
       `(Email, TemplateBaseName)` pair, and the "loser" request's result item is
       classified as "already pending"/"already subscribed" rather than raising an
       unhandled exception.
     - `UPDATE MailSubscribers SET ConfirmTokenExpiresAt = '2020-01-01' WHERE Email = 'test@example.com'`
       then re-subscribe with a fresh submission for the same email/templates -> new
       rows reuse-path triggers (2.3's "expired -> reuse row" branch), confirm again ->
       success page.

6.2. Frontend — `npm run dev` in `acik-istihbarat-public/`, visit
     `/acikmedya/AcikGazete` and `/acikmedya/AcikKose`:
     - Active tab accent switches correctly between the two routes.
     - Type invalid email (e.g. `"nope"`), click "Abone Ol" -> inline error shown, modal
       does NOT open.
     - Type valid email, click "Abone Ol" -> select modal opens, both checkboxes
       unchecked; click modal's "Abone Ol" with nothing checked -> inline "select at
       least one" error, no network call fires (verify via DevTools Network tab).
     - Check one box, submit -> Network tab shows the `POST .../mail/subscribe` call and
       its response; summary modal appears with the confirmed newsletter name; DB has
       the new row (cross-check against 6.1's SQL).
     - Re-submit the exact same email+newsletter -> summary modal shows the "already
       pending" itemized message instead of a fresh confirmation, no duplicate DB row
       (`SELECT COUNT(*) ... WHERE Email = ... AND TemplateBaseName = ...` still 1).

## Further Considerations (carried over / newly surfaced)
1. **Preline modal API uncertainty (Phase 5.4)** — this repo has no existing
   `data-hs-overlay` modal usage to copy from (only `hs-collapse-toggle`). Confirm the
   Preline package version/plugins actually bundled via `PrelineScript.tsx` and
   `package.json` before implementation; have a plain-React-state modal fallback ready
   if the Overlay plugin isn't available, to avoid getting stuck mid-implementation.
2. **`/acikmedya/AcikGazete` as the hardcoded "back to site" link target** (Phase 3.2) —
   no neutral `/acikmedya` index route exists today; using the first folder as a
   pragmatic default. Flag whether a true bullet-proof "newsletters index" page is
   wanted as a separate follow-up (out of scope here).
3. **Confirmation email delivery latency** (Phase 2.4) — since sending is now queued
   (Finding #1's fix) rather than inline, there is a small delay between `Subscribe`
   returning `200` and the actual email hitting the user's inbox, bounded by the
   background poller's interval (`PollIntervalSeconds`). This is an acceptable and
   expected trade-off for removing the request-blocking/500-on-send-failure behavior;
   call this out in the summary modal copy only if `PollIntervalSeconds` is large enough
   (e.g. minutes) to be user-noticeable — otherwise no UI change needed.
4. **`MailSchedules`-driven allow-list (Phase 2.2) requires at least one `MailSchedule`
   row per newsletter folder to exist already** — if the mailing-engine feature's own
   setup/seeding hasn't been done in a given environment, the subscribe endpoint will
   reject ALL template names as invalid. Confirm `MailSchedules` has rows for both
   `AcikGazete` and `AcikKose` in whatever environment this is tested/deployed to,
   before assuming a bug in this feature's code if subscribe requests get rejected.

---

## Evaluation Findings Incorporated

This plan was reviewed by an independent Evaluator pass; all four surfaced findings were
approved and folded directly into the phases above:

1. **Performance/High — Synchronous SMTP send on the request path.** Fixed by
   introducing a `PendingConfirmationEmails` outbox table (Phase 0.6) and having the
   existing background poller dispatch queued confirmation emails (Phase 2.4) instead of
   `MailController.Subscribe` connecting to SMTP inline.
2. **Functionality/Critical — TOCTOU race on the `(Email, TemplateBaseName)` unique
   index.** Fixed by wrapping `SaveChangesAsync` in a `try/catch (DbUpdateException)` with
   re-classification and a single bounded retry (Phase 2.3), so concurrent duplicate
   submissions never surface as an unhandled 500.
3. **Safety/High — GET-based confirm endpoint mutated state directly.** Fixed by
   splitting into `[HttpGet("confirm")]` (non-mutating prompt page) and
   `[HttpPost("confirm")]` (actual activation), mirroring the existing
   `UnsubscribeConfirm`/`UnsubscribeConfirmed` pattern already in `MailController.cs`
   (Phase 3.1).
4. **Maintainability/Low — Dual source-of-truth for newsletter display names.** Not
   fixed in this plan (deliberately deferred as low-priority/non-blocking); tracked as a
   follow-up TODO item in `ProjectImplementationDocs/TODO-AcikIstihbarat.md`
   ("Unify newsletter display name source of truth").
