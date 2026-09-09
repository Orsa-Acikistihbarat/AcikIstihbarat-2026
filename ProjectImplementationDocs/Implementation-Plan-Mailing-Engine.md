# Architecture and Implementation Plan: Mailing Engine

## Summary
A lightweight periodic mailing engine added inside `AcikIstihbarat.API`, sending Claude-Code-produced
static HTML newsletter artifacts (already resolved via date-suffix convention, same as the existing
`acikmedya` web newsletters) to registered subscribers via Gmail using true OAuth2 (XOAUTH2), with a
separately configurable send-schedule table, anti-blacklisting guardrails, unsubscribe support, and
DB + log-based auditing.

## Decisions (confirmed across session)
- Lives inside `AcikIstihbarat.API` (new Services + entities + migration on existing `AppDbContext`),
  not a standalone project.
- Trigger: in-process `BackgroundService` poller (no external cron, no admin-panel manual trigger).
- Templates: reuse `acik-istihbarat-public/acikmedya-data/<Folder>` bind mount (read-only into API
  container too), same `^<name>(\d{2})(\d{2})(\d{2})\.html$` date-suffix convention as
  `acik-istihbarat-public/lib/newsletters.ts`.
- Send cadence must be DATA-driven (separate `MailSchedules` table), decoupled from the subscriber
  list — daily-only today, but weekly/interval/cron-like later without code changes.
- Auth: Gmail OAuth2 (XOAUTH2 via MailKit `SaslMechanismOAuth2`), NOT app-password. One-time manual
  consent bootstrap produces a long-lived `refresh_token`; runtime exchanges it for short-lived
  access tokens per send batch.
- Unsubscribe: engine-appended footer block + `List-Unsubscribe` header (does NOT rely on Claude Code
  templates reserving a placeholder) — more robust since template generation isn't coordinated with
  the mailer.
- Critical known gotcha: OAuth consent screen must be "In production" publishing status, or refresh
  tokens expire after 7 days (fatal for unattended daily sends). Alternative if
  `acikistihbarat.com` has Google Workspace: service account + domain-wide delegation (no user
  consent/refresh token at all) — flag as an option to evaluate before doing the verification work.

---

## Phase 0 — Data model & migration *(foundation, do first)*

0.1. Create `AcikIstihbarat.API/Models/Entities/MailSubscriber.cs`:
     - `int Id` (PK, identity)
     - `string Email` (required, max length 320)
     - `string TemplateBaseName` (required, max length 128 — e.g. "AcikGazete")
     - `bool IsActive` (default true)
     - `DateTime? LastSentAt` (UTC)
     - `string? LastSendStatus` (e.g. "Sent"/"Failed"/"Skipped", max length 32 — plain string is
       fine, no enum needed for a display/audit field)
     - `int ConsecutiveFailureCount` (default 0 — drives auto-deactivation in 2.5)
     - `Guid UnsubscribeToken` (required, default `Guid.NewGuid()` at creation)
     - `DateTime CreatedAt` (UTC, default now)

0.2. Create `AcikIstihbarat.API/Models/Entities/MailSchedule.cs`:
     - `int Id` (PK)
     - `string TemplateBaseName` (required, max length 128)
     - `MailFrequencyType FrequencyType` — new enum in same file or
       `Models/Entities/MailFrequencyType.cs`: `Daily = 0, Weekly = 1, IntervalDays = 2, Cron = 3`
       (only `Daily` gets real logic in Phase 2.6; others are reserved values, not implemented yet —
       throw `NotSupportedException` if selected for now, so misconfiguration fails loudly).
     - `int? IntervalValue` (nullable — used only by `IntervalDays`)
     - `int? DaysOfWeekMask` (nullable — bitmask Sun=1,Mon=2,...Sat=64; used only by `Weekly`)
     - `TimeSpan TimeOfDayLocal` (e.g. `08:00:00`, interpreted in `Europe/Istanbul`)
     - `bool IsActive` (default true)
     - `DateTime? LastRunAtUtc`
     - `DateTime NextRunAtUtc` (required — computed on insert/seed and after every run; this is the
       only column the poller filters on)

0.3. Create `AcikIstihbarat.API/Models/Entities/EmailSendLog.cs`:
     - `long Id` (PK — use `long` since this table grows unboundedly, unlike the others)
     - `int SubscriberId` (FK -> `MailSubscriber.Id`)
     - `int? ScheduleId` (FK -> `MailSchedule.Id`, nullable+SetNull so deleting a schedule doesn't
       break historical logs)
     - `string TemplateFileName` (the exact resolved file, e.g. `AcikGazete070926.html` — store the
       resolved filename, not just the base name, so you can audit exactly what was sent even if
       fallback-to-past-date logic kicked in)
     - `DateTime SentAtUtc`
     - `bool Success`
     - `string? ErrorMessage` (max length 2000, nullable)

0.4. Open `AcikIstihbarat.API/Data/AppDbContext.cs` and:
     a. Add three properties: `public DbSet<MailSubscriber> MailSubscribers { get; set; }`,
        `public DbSet<MailSchedule> MailSchedules { get; set; }`,
        `public DbSet<EmailSendLog> EmailSendLogs { get; set; }`.
     b. Inside `OnModelCreating`, after the existing `Haber`/`Yazi` config blocks, add:
        - `builder.Entity<MailSubscriber>().HasIndex(s => new { s.Email, s.TemplateBaseName }).IsUnique();`
          — **composite**, NOT `Email` alone: each row is one subscriber-to-one-template
          relationship, so a single email must be able to subscribe to multiple different
          templates (e.g. both `AcikGazete` and `AcikKose`) without violating uniqueness.
        - `builder.Entity<MailSubscriber>().HasIndex(s => s.UnsubscribeToken).IsUnique();`
        - `builder.Entity<MailSubscriber>().HasIndex(s => new { s.TemplateBaseName, s.IsActive });`
          — supports the per-run subscriber lookup query in 2.4 step 3, which executes on every
          scheduled send.
        - `builder.Entity<EmailSendLog>().HasOne<MailSubscriber>().WithMany().HasForeignKey(l =>
          l.SubscriberId).OnDelete(DeleteBehavior.Restrict);`
        - `builder.Entity<EmailSendLog>().HasOne<MailSchedule>().WithMany().HasForeignKey(l =>
          l.ScheduleId).OnDelete(DeleteBehavior.SetNull);`
     c. Do NOT add a navigation collection back from `MailSubscriber`/`MailSchedule` to
        `EmailSendLog` unless a concrete query need appears later — keep the entities minimal,
        matching this codebase's existing style (most entities here only have forward nav
        properties where actually consumed, e.g. `Haber.Medyalar`).

0.5. Run in a terminal from the `AcikIstihbarat.API` folder:
     `dotnet ef migrations add AddMailingEngine`
     Confirm the generated file lands in `Migrations/` with a timestamp prefix matching the existing
     naming convention (`20260801225254_InitialCreate.cs` style).

0.6. **Manually open the generated migration's `Up()` method** before applying anything — confirm:
     - All 3 `CreateTable` calls have correct column types/nullability matching 0.1-0.3 exactly.
     - The 2 unique indexes and 2 FKs from 0.4b are present with the correct `OnDelete` behavior.
     - No unrelated model snapshot drift got pulled in from other entities (if it did, something in
       `OnModelCreating` was touched unintentionally — investigate before applying).

0.7. Apply to the dev DB: `dotnet ef database update`. Verify via
     `SELECT name FROM sys.tables WHERE name IN ('MailSubscribers','MailSchedules',
     'EmailSendLogs')` against the actual dev DB (don't just trust "up to date" console output —
     see repo lesson on migrations silently no-op'ing).

---

## Phase 1 — Gmail OAuth2 one-time bootstrap *(independent, parallel with Phase 0)*

1.1. Google Cloud Console (manual, human-driven, no code):
     a. Create or select a GCP project.
     b. APIs & Services -> Library -> enable "Gmail API".
     c. APIs & Services -> OAuth consent screen: User type = External; App name = e.g.
        "AcikIstihbarat Mailer"; add scope `https://www.googleapis.com/auth/gmail.send`.
     d. Add the sending Gmail account under "Test users" for now (needed even before going to
        production, to be able to test at all).
     e. APIs & Services -> Credentials -> Create Credentials -> OAuth Client ID -> Application type
        "Desktop app". Note the generated Client ID + Client Secret.

1.2. Create a throwaway console project OUTSIDE the deployed API (e.g.
     `scratch/GmailOAuthBootstrap/GmailOAuthBootstrap.csproj`, a plain `dotnet new console`, deleted
     after use — do NOT add it to the solution file or Dockerfile):
     a. Hardcode/prompt for `clientId`, `clientSecret` at the top.
     b. Build the authorization URL:
        `https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri=http://localhost:8721/&response_type=code&scope=https://www.googleapis.com/auth/gmail.send&access_type=offline&prompt=consent`
     c. Start a bare `HttpListener` on `http://localhost:8721/`, call
        `Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true })` to open the
        default browser.
     d. On the listener receiving the redirect request, parse the `code` query param from the
        request URL, respond with a simple "you can close this tab" HTML page, then stop the
        listener.
     e. POST to `https://oauth2.googleapis.com/token` with form fields `code`, `client_id`,
        `client_secret`, `redirect_uri=http://localhost:8721/`, `grant_type=authorization_code`.
     f. Deserialize the JSON response, print `refresh_token` to console (and `access_token` for
        immediate sanity-check only — it expires in ~1hr so don't bother storing it).

1.3. Store secrets (never commit):
     a. Local dev: from `AcikIstihbarat.API/` run `dotnet user-secrets init` (if not already done)
        then `dotnet user-secrets set "Mail:GmailOAuth:ClientId" "..."` (repeat for
        `ClientSecret`, `RefreshToken`, `SenderAddress`).
     b. Production: add to `docker-compose.yml`'s `api` service `environment:` block (see Phase 3.2)
        sourced from a root `.env` file (same mechanism already used for
        `${ACIKMEDYA_WEBHOOK_TOKEN}`), NOT hardcoded into the compose file itself.

1.4. Before relying on this for daily unattended sends, confirm in Google Cloud Console that the
     OAuth consent screen is NOT stuck in "Testing" status (7-day refresh-token expiry) — either:
     - Publish to "In production" (may prompt for verification since `gmail.send` is a sensitive
       scope — follow Google's in-console prompts), OR
     - If `acikistihbarat.com` runs on Google Workspace, evaluate switching to a service account +
       domain-wide delegation instead (no user consent flow, no refresh-token expiry at all) —
       treat this as a parallel spike, don't block Phase 2 on deciding it.

---

## Phase 2 — Core services *(depends on Phase 0 tables existing; Phase 1 secrets needed only for
live-send testing, not for writing the code)*

### 2.1 Template resolver
Create `AcikIstihbarat.API/Services/IMailTemplateResolver.cs`:
```
public interface IMailTemplateResolver
{
    (string FileName, string Html)? ResolveLatest(string templateBaseName);
}
```
Create `MailTemplateResolver.cs` implementing it:
- Constructor takes `IConfiguration` (or a small `MailOptions` bound via `IOptions<MailOptions>` —
  prefer the latter, matching typical ASP.NET Core options pattern even though this codebase mostly
  reads `IConfiguration` directly today; call this out as a deliberate small improvement).
- Base directory = `config["Mail:TemplatesDataDir"]`, subfolder = `templateBaseName`.
- Filename regex (copy verbatim, do not paraphrase):
  `^{templateBaseName}(\d{2})(\d{2})(\d{2})\.html$` (escape `templateBaseName` with
  `Regex.Escape` since it's interpolated).
- List files in `{baseDir}/{templateBaseName}/`, parse dd/mm/yy from matches, compute "today" via
  `TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istanbulTz)` (register the Windows/Linux tz id
  carefully — use `"Europe/Istanbul"` with `TimeZoneInfo.FindSystemTimeZoneById` which works on
  Linux containers; this differs from the Node version's `Intl.DateTimeFormat` approach but must
  produce identical dd/mm/yy results — add a comment cross-referencing `newsletters.ts` explicitly).
- Same fallback rule as the TS version: exact date match wins outright; else pick the highest past
  date; ignore future-dated files; return `null` if directory missing or no files match.
- Read the winning file with `File.ReadAllText`, return `(fileName, html)`.

### 2.2 OAuth token provider
Create `AcikIstihbarat.API/Services/IGmailOAuthTokenProvider.cs`:
```
public interface IGmailOAuthTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);
}
```
Create `GmailOAuthTokenProvider.cs`, registered as a Singleton (token cache must survive across
scoped requests/background service iterations):
- Fields: injected `HttpClient` (via `IHttpClientFactory`, named client e.g. "GoogleOAuth"),
  `IOptions<GmailOAuthOptions>` (ClientId, ClientSecret, RefreshToken, SenderAddress).
- Private cached `string? _accessToken`, `DateTimeOffset _expiresAt`.
- `GetAccessTokenAsync`: if cached token valid for > 5 more minutes, return it; otherwise
  `SemaphoreSlim`-guard a refresh (avoid concurrent refresh storms from parallel batch sends), POST
  to `https://oauth2.googleapis.com/token` with `client_id`, `client_secret`, `refresh_token`,
  `grant_type=refresh_token`; on non-2xx or `invalid_grant` in the body, throw a specific
  `GmailReauthRequiredException` (new small exception type) so callers can log an actionable
  "someone needs to redo the OAuth bootstrap" message instead of a generic HTTP error.

### 2.3 Mail sender
Add NuGet packages to `AcikIstihbarat.API.csproj`: `MailKit` (latest 4.x) and `PreMailer.Net`
(latest stable).

Create `IMailSenderService.cs`:
```
public interface IMailSenderService
{
    Task SendAsync(MailSendRequest request, CancellationToken ct = default);
}
```
Define `MailSendRequest` (simple record/class): `string ToEmail`, `string Subject`, `string Html`,
`Guid UnsubscribeToken`.

Create `MailSenderService.cs`:
- Injected: `IGmailOAuthTokenProvider`, `IOptions<GmailOAuthOptions>`, `ILogger<MailSenderService>`.
- `SendAsync` steps, in order:
  a. Inline CSS: `html = PreMailer.Net.PreMailer.MoveCssInline(html).Html;`
  b. Build unsubscribe URL:
     `{PublicApiBaseUrl}/api/public/mail/unsubscribe?token={request.UnsubscribeToken}` (base URL
     from config, e.g. `Mail:PublicApiBaseUrl` = `https://api.acikistihbarat.com`).
  c. Inject a footer `<div>` immediately before the last `</body>` (case-insensitive
     `LastIndexOf`), containing a visible unsubscribe link; if no `</body>` tag found, append to
     end of string instead of throwing (defensive fallback for a malformed artifact).
  d. Build plain-text alternative via a small local helper: strip tags with
     `Regex.Replace(html, "<[^>]+>", " ")`, collapse whitespace, `WebUtility.HtmlDecode`; append a
     plain-text unsubscribe line pointing at the same URL.
  e. Build `MimeMessage`: `From` = `SenderAddress` from options, `To` = `request.ToEmail`,
     `Subject` = `request.Subject`, `Body` = `BodyBuilder { HtmlBody = ..., TextBody = ... }.ToMessageBody()`.
  f. Add headers: `message.Headers.Add("List-Unsubscribe", $"<{unsubscribeUrl}>");` and
     `message.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");`.
  g. This method does NOT open its own SMTP connection — it accepts an already-connected
     `MailKit.Net.Smtp.SmtpClient` as a parameter (change the interface to
     `SendAsync(SmtpClient client, MailSendRequest request, ...)`) so the orchestrator (2.4) can
     reuse one connection across an entire batch instead of reconnecting per email — call this out
     explicitly since it changes the interface shape from the naive version.

### 2.4 Orchestrator
Create `IMailingOrchestrator.cs`:
```
public interface IMailingOrchestrator
{
    Task RunScheduleAsync(int scheduleId, CancellationToken ct = default);
}
```
Create `MailingOrchestrator.cs` (Scoped — needs `AppDbContext`):
- Injected: `AppDbContext`, `IMailTemplateResolver`, `IMailSenderService`,
  `IGmailOAuthTokenProvider`, `IOptions<MailGuardrailOptions>`, `ILogger<MailingOrchestrator>`.
- `RunScheduleAsync(scheduleId)`:
  1. Load the `MailSchedule` by id; if `!IsActive`, return early.
  2. Call `_templateResolver.ResolveLatest(schedule.TemplateBaseName)`; if `null`, log a warning
     ("no template available for {TemplateBaseName}, skipping this run") and return WITHOUT
     updating `NextRunAtUtc` as a failure — still let the scheduler (2.6) advance `NextRunAtUtc`
     normally, since a missing template today isn't a scheduling bug.
  3. Query `MailSubscribers` where `IsActive && TemplateBaseName == schedule.TemplateBaseName`.
  4. Filter out subscribers whose `LastSentAt` is already "today" in Istanbul time (idempotency —
     reuse the same tz conversion helper as 2.1, factor it into a small shared
     `IstanbulClock`/static helper rather than duplicating the conversion in two services).
  5. Connect ONE `SmtpClient` (`smtp.gmail.com`, port 587, `SecureSocketOptions.StartTls`),
     authenticate once via `SaslMechanismOAuth2(senderAddress, await
     _tokenProvider.GetAccessTokenAsync())`.
  6. Iterate remaining subscribers in batches per `MailGuardrailOptions.BatchSize` (see 2.5),
     tracking a running `sentCount`:
     - At the top of each iteration: `if (sentCount >= guardrails.MaxSendsPerRun) { log a clear
       "MaxSendsPerRun cap reached, stopping run" warning; break; }` (skip this check entirely
       when `MaxSendsPerRun` is null/unset) — this is the hard safety cap of last resort, it must
       actually stop sends, not just exist as an unread config value.
     - Derive subject: humanize `TemplateBaseName` (simple `Regex.Replace` inserting spaces before
       capital letters, e.g. "AcikGazete" -> "Acik Gazete" — good enough, no i18n library needed)
       + " - " + resolved file's date formatted `dd.MM.yyyy`.
     - Call `_mailSender.SendAsync(client, request)`; increment `sentCount` regardless of
       success/failure (it counts attempts against the cap, not just successes).
     - On success: insert `EmailSendLog{Success=true}`, update subscriber
       `LastSentAt=nowUtc, LastSendStatus="Sent", ConsecutiveFailureCount=0`.
     - On exception: insert `EmailSendLog{Success=false, ErrorMessage=ex.Message}`, increment
       `ConsecutiveFailureCount`; if it now exceeds `MailGuardrailOptions.MaxConsecutiveFailures`,
       set `subscriber.IsActive=false` and log a clear "auto-deactivated" warning.
     - Apply the per-message delay and circuit-breaker rules from 2.5 between/after each send.
  7. Recompute `schedule.NextRunAtUtc`/`schedule.LastRunAtUtc` (see updated 2.6 step 3 — this is now
     done HERE, in the same unit of work, not split into the background service) and
     `await _db.SaveChangesAsync()` ONCE at the end of the run, covering the schedule update plus
     all `EmailSendLog`/`MailSubscriber` writes together (batch the DB writes; don't round-trip per
     recipient, and don't leave `NextRunAtUtc` persistence ambiguous across two DbContext scopes).
  8. Disconnect the `SmtpClient` in a `finally` block.

### 2.5 Guardrail details (implemented inside 2.4, configured via new `MailGuardrailOptions`)
- `BatchSize` (default 25).
- `MinDelayMs`/`MaxDelayMs` per message (default 1000/5000 — pick a random value in this range
  before each send via `Random.Shared.Next`).
- `InterBatchDelayMs` (default 30000) — pause this long after each full batch of `BatchSize`.
- `MaxSendsPerRun` (default null/unlimited, but expose the setting — acts as a hard daily safety
  cap independent of subscriber count, in case the list grows unexpectedly large). **Must be
  actively enforced** by the loop in 2.4 step 6 (explicit break-on-cap check), not just read into
  config and left unused.
- `MaxConsecutiveFailures` (default 3) — see 2.4 step 6.
- Circuit breaker: track a running count of failures within the CURRENT run; if it hits e.g. 5
  consecutive SMTP-level exceptions (not per-recipient business failures — specifically
  `SmtpCommandException`/`SmtpProtocolException` with 4xx/5xx codes), abort the rest of THIS run
  immediately (don't mark remaining un-attempted subscribers as failed — they simply weren't
  processed, will be retried next scheduled run) and log a high-severity "aborting batch, SMTP
  errors" message.

### 2.6 Background scheduler
Create `MailSchedulerBackgroundService.cs` : `BackgroundService`:
- Injected: `IServiceScopeFactory` (to create a scope per poll tick, since `AppDbContext` and
  `MailingOrchestrator` are Scoped), `IOptions<MailOptions>` (for `PollIntervalSeconds`),
  `ILogger<MailSchedulerBackgroundService>`.
- `ExecuteAsync`: `while (!stoppingToken.IsCancellationRequested)`:
  1. Create a scope, resolve `AppDbContext`.
  2. Query `MailSchedules` where `IsActive && NextRunAtUtc <= DateTime.UtcNow`
     (`AsNoTracking` for the initial due-check query is fine; re-fetch tracked entities as needed
     inside the orchestrator itself).
  3. For each due schedule id (sequentially, not `Task.WhenAll` — sending is I/O-bound but you
     want deterministic guardrail behavior, not N schedules hammering Gmail concurrently), resolve
     a fresh scope and call `orchestrator.RunScheduleAsync(id, stoppingToken)`, wrapped in
     try/catch that logs and continues to the next schedule rather than crashing the whole loop.
     **`NextRunAtUtc`/`LastRunAtUtc` are NOT recomputed here** — ownership belongs entirely to the
     orchestrator (2.4 step 7), which loads the tracked `MailSchedule` itself inside its own scope
     and saves the recompute in the same `SaveChangesAsync` as the send-result writes. This avoids
     the two-DbContext ambiguity of updating an entity that was only ever queried `AsNoTracking` in
     the outer due-check scope. Recompute rules (implemented inside the orchestrator, not here):
     - `Daily`: `today's TimeOfDayLocal in Istanbul, converted to UTC; if already passed today, use
       tomorrow's` — i.e. always advance to the *next* future occurrence, never re-trigger the same
       slot twice.
     - `Weekly`/`IntervalDays`/`Cron`: not implemented yet — `switch` should have a `default: throw
       new NotSupportedException(...)` so accidentally seeding one of these silently doesn't just
       no-op forever.
     - If `RunScheduleAsync` throws before reaching its own save (caught here by the background
       service's try/catch), `NextRunAtUtc` is deliberately left unchanged so the same schedule is
       retried on the next poll tick rather than being skipped for a full cycle.
  4. `await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), stoppingToken)`.

### 2.7 Unsubscribe endpoint (GET confirmation page + POST one-click, per RFC 8058)
Create `AcikIstihbarat.API/Controllers/Public/MailController.cs`:
- `[ApiController] [Route("api/public/mail")] [AllowAnonymous]` (match existing Public controllers'
  attribute style — check one, e.g. `HaberController`, for the exact base route prefix convention
  before finalizing the route string).
- `[HttpGet("unsubscribe")] public async Task<IActionResult> UnsubscribeConfirm([FromQuery] Guid
  token)`: looks up `MailSubscriber` by `UnsubscribeToken == token` — does **NOT** mutate state.
  Returns a minimal HTML confirmation page with a visible "Confirm unsubscribe" button that POSTs
  to the endpoint below (same `token`, carried as a hidden field or in the form action URL). If the
  token isn't found, render the same neutral confirmation-style page regardless (don't leak
  validity via 404 vs 200 — minor enumeration hardening); the POST action below will simply no-op
  for an invalid token. This avoids automated mail-security link-prefetchers (which commonly GET
  every link in an inbound email before a human opens it) from silently mass-unsubscribing
  recipients, since GET no longer performs the mutation.
- `[HttpPost("unsubscribe")] public async Task<IActionResult> UnsubscribeConfirmed([FromForm] Guid
  token)`: this is the actual mutating action — used both by the confirmation page's button POST
  AND by mail clients' native one-click unsubscribe (which sends `List-Unsubscribe-Post` as a POST
  per RFC 8058, matching the header added in 2.3(f)). Looks up `MailSubscriber` by
  `UnsubscribeToken == token`; if found, set `IsActive=false` and save; always return a generic 200
  confirmation regardless of whether a matching token was found (same enumeration-hardening
  rationale as the GET action).

### 2.8 DI registration
In `Program.cs`, near the existing `AddScoped<IHaberService, HaberService>()` block, add:
- `builder.Services.Configure<MailOptions>(builder.Configuration.GetSection("Mail"));`
- `builder.Services.Configure<GmailOAuthOptions>(builder.Configuration.GetSection("Mail:GmailOAuth"));`
- `builder.Services.Configure<MailGuardrailOptions>(builder.Configuration.GetSection("Mail:Guardrails"));`
- `builder.Services.AddHttpClient("GoogleOAuth");`
- `builder.Services.AddSingleton<IGmailOAuthTokenProvider, GmailOAuthTokenProvider>();`
- `builder.Services.AddScoped<IMailTemplateResolver, MailTemplateResolver>();`
- `builder.Services.AddScoped<IMailSenderService, MailSenderService>();`
- `builder.Services.AddScoped<IMailingOrchestrator, MailingOrchestrator>();`
- `builder.Services.AddHostedService<MailSchedulerBackgroundService>();`

---

## Phase 3 — Configuration & Docker wiring *(depends on Phase 0-2 existing so the settings keys
they bind to are real)*

3.1. `appsettings.json` (safe defaults, no secrets) — add:
```
"Mail": {
  "TemplatesDataDir": "/app/acikmedya-data",
  "PublicApiBaseUrl": "https://api.acikistihbarat.com",
  "PollIntervalSeconds": 120,
  "GmailOAuth": { "SenderAddress": "" },
  "Guardrails": {
    "BatchSize": 25, "MinDelayMs": 1000, "MaxDelayMs": 5000,
    "InterBatchDelayMs": 30000, "MaxConsecutiveFailures": 3, "MaxSendsPerRun": null
  }
}
```
3.2. `appsettings.Development.json` — override `TemplatesDataDir` to a local path and
     `PublicApiBaseUrl` to `http://localhost:15128` for local testing.
3.3. Secrets (`ClientId`/`ClientSecret`/`RefreshToken`) go ONLY into user-secrets (dev) or
     environment variables (prod) — never into either `appsettings*.json` file.
3.4. `docker-compose.yml`, `api` service:
     a. Add volume: `./acik-istihbarat-public/acikmedya-data:/app/acikmedya-data:ro` (append to
        existing `volumes:` list alongside the `MedyaKutuphanesi` mount).
     b. Add to `environment:`: `Mail__GmailOAuth__ClientId=${MAIL_GMAIL_CLIENT_ID}`,
        `Mail__GmailOAuth__ClientSecret=${MAIL_GMAIL_CLIENT_SECRET}`,
        `Mail__GmailOAuth__RefreshToken=${MAIL_GMAIL_REFRESH_TOKEN}`,
        `Mail__GmailOAuth__SenderAddress=${MAIL_GMAIL_SENDER_ADDRESS}`.
     c. Add these 4 vars to the root `.env` file (create if absent) that already backs
        `${ACIKMEDYA_WEBHOOK_TOKEN}`; confirm `.env` is in `.gitignore`.
3.5. No `Caddyfile` change (new route is a sub-path of the already-proxied
     `api.acikistihbarat.com`). No GitHub Actions workflow change (env vars flow through the same
     deploy secrets mechanism already used for `ACIKMEDYA_WEBHOOK_TOKEN` — confirm by checking
     `.github/workflows/deploy.yml`'s existing secret-injection step before assuming this needs no
     changes).

---

## Phase 4 — Verification

4.1. Unit tests (new test project or existing one if present — check for
     `AcikIstihbarat.API.Tests` before creating a new csproj) for `MailTemplateResolver`:
     - Exact today-date file present -> returned.
     - Only a past-dated file present -> returned (fallback).
     - Only a future-dated file present -> `null`.
     - Empty/missing folder -> `null`.
     - Malformed filename (doesn't match regex) -> ignored, doesn't crash.

4.2. Add `Mail:DryRun` (bool, default false) config flag; in `MailingOrchestrator.RunScheduleAsync`,
     if true, perform every step through subject-line derivation and log
     `"[DRYRUN] would send '{subject}' to {email}"` instead of calling `IMailSenderService`/opening
     an `SmtpClient` at all — lets you validate schedule due-checks, subscriber filtering, and
     idempotency logic with zero real sends or Gmail API calls.

4.3. Manual smoke test sequence (after Phase 1 secrets are live):
     a. Insert one `MailSchedule` row with `TimeOfDayLocal` a few minutes in the future,
        `NextRunAtUtc` computed accordingly, `IsActive=true`.
     b. Insert one `MailSubscriber` row pointed at your own inbox, `IsActive=true`,
        `TemplateBaseName` matching an existing `acikmedya-data` folder that has at least one dated
        file.
     c. Set `Mail:DryRun=false`, run the API, watch logs for the poll tick picking up the schedule.
     d. Confirm: email arrives; renders correctly in Gmail web AND at least one other client
        (Outlook desktop / Apple Mail, if available) — checks the CSS-inlining step actually
        worked; clicking the unsubscribe link opens the GET confirmation page WITHOUT mutating
        state, and only after clicking "Confirm unsubscribe" (POST) does `IsActive` flip to false
        in the DB — verifying the GET request alone is inert (anti-prefetch-bot safeguard);
        `EmailSendLog` row written with `Success=true`; `MailSubscriber.LastSentAt` updated so a
        second poll tick the same day does NOT re-send (re-activate the subscriber first to test
        this in isolation from the unsubscribe test).

4.4. Failure-path smoke test: temporarily set an invalid `RefreshToken` in config, run a schedule,
     confirm `GmailReauthRequiredException` is thrown and logged clearly (not swallowed as a generic
     error), and that the scheduler moves on / doesn't crash the whole background service.

---

## Relevant files
- `AcikIstihbarat.API/Models/Entities/MailSubscriber.cs` — NEW
- `AcikIstihbarat.API/Models/Entities/MailSchedule.cs` — NEW
- `AcikIstihbarat.API/Models/Entities/MailFrequencyType.cs` — NEW (enum)
- `AcikIstihbarat.API/Models/Entities/EmailSendLog.cs` — NEW
- `AcikIstihbarat.API/Data/AppDbContext.cs` — MODIFY
- `AcikIstihbarat.API/Migrations/*_AddMailingEngine.cs` — NEW (generated)
- `AcikIstihbarat.API/Services/IMailTemplateResolver.cs` / `MailTemplateResolver.cs` — NEW
- `AcikIstihbarat.API/Services/IGmailOAuthTokenProvider.cs` / `GmailOAuthTokenProvider.cs` — NEW
- `AcikIstihbarat.API/Services/GmailReauthRequiredException.cs` — NEW
- `AcikIstihbarat.API/Services/IMailSenderService.cs` / `MailSenderService.cs` — NEW
- `AcikIstihbarat.API/Services/MailSendRequest.cs` — NEW
- `AcikIstihbarat.API/Services/IMailingOrchestrator.cs` / `MailingOrchestrator.cs` — NEW
- `AcikIstihbarat.API/Services/MailSchedulerBackgroundService.cs` — NEW
- `AcikIstihbarat.API/Models/DTOs/MailOptions.cs` (`MailOptions`, `GmailOAuthOptions`,
  `MailGuardrailOptions`) — NEW
- `AcikIstihbarat.API/Controllers/Public/MailController.cs` — NEW
- `AcikIstihbarat.API/Program.cs` — MODIFY
- `AcikIstihbarat.API/appsettings.json`, `appsettings.Development.json` — MODIFY
- `AcikIstihbarat.API/AcikIstihbarat.API.csproj` — MODIFY (MailKit, PreMailer.Net)
- `docker-compose.yml` — MODIFY (`api` service)
- root `.env` — MODIFY (4 new secret vars)
- `.github/workflows/deploy.yml` — VERIFY (confirm existing secret-passthrough covers new vars)
- `scratch/GmailOAuthBootstrap/` — NEW, throwaway, not committed/deployed

## Further Considerations
1. Google Workspace domain-wide delegation vs. OAuth consent verification — check if
   `acikistihbarat.com` has Workspace before investing in consent-screen verification.
2. Plain-text fallback generation: naive HTML-tag-strip vs. a proper library — start naive, revisit
   only if rendering issues appear.
3. Bounce/complaint feedback: Gmail SMTP doesn't expose bounce webhooks like SendGrid/SES — the
   consecutive-failure auto-deactivation heuristic (2.4/2.5) is the practical substitute.
4. Whether an existing test project already exists in the solution (affects 4.1's "new vs existing
   test project" choice) — verify before implementation.
5. **Declined during evaluation**: validating `TemplateBaseName` against real `acikmedya-data`
   folders at insert-time/startup (would have caught typos early instead of silently sending
   nothing forever, logged only as a warning). Left as-is per user decision — worth revisiting if
   misconfiguration becomes a recurring support issue.

## Evaluation history
This plan was reviewed by the Evaluator agent; 5 of 6 findings were approved and are integrated
directly into the phase steps above (composite `Email`+`TemplateBaseName` unique index, composite
`TemplateBaseName`+`IsActive` index, GET-confirm/POST-mutate unsubscribe split per RFC 8058,
single-ownership `NextRunAtUtc` recompute inside the orchestrator, enforced `MaxSendsPerRun` cap).
The 6th (proactive `TemplateBaseName` validation) was declined — see Further Considerations #5.
