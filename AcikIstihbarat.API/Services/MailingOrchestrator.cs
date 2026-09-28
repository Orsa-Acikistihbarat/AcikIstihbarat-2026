using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using AcikIstihbarat.API.Data;
using AcikIstihbarat.API.Models.DTOs;
using AcikIstihbarat.API.Models.Entities;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AcikIstihbarat.API.Services
{
    public class MailingOrchestrator : IMailingOrchestrator
    {
        private static readonly Regex HumanizeRegex = new("(?<=[a-z0-9])(?=[A-Z])", RegexOptions.Compiled);

        private readonly AppDbContext _db;
        private readonly IMailTemplateResolver _templateResolver;
        private readonly IMailSenderService _mailSender;
        private readonly IGmailOAuthTokenProvider _tokenProvider;
        private readonly IMailRunTracker _tracker;
        private readonly MailOptions _mailOptions;
        private readonly MailGuardrailOptions _guardrails;
        private readonly ILogger<MailingOrchestrator> _logger;

        public MailingOrchestrator(
            AppDbContext db,
            IMailTemplateResolver templateResolver,
            IMailSenderService mailSender,
            IGmailOAuthTokenProvider tokenProvider,
            IMailRunTracker tracker,
            IOptions<MailOptions> mailOptions,
            ILogger<MailingOrchestrator> logger)
        {
            _db = db;
            _templateResolver = templateResolver;
            _mailSender = mailSender;
            _tokenProvider = tokenProvider;
            _tracker = tracker;
            _mailOptions = mailOptions.Value;
            _guardrails = mailOptions.Value.Guardrails;
            _logger = logger;
        }

        public Task RunScheduleAsync(int scheduleId, CancellationToken ct = default)
            => RunScheduleAsync(scheduleId, forceResend: false, ct);

        public async Task RunScheduleAsync(int scheduleId, bool forceResend, CancellationToken ct = default)
        {
            var schedule = await _db.MailSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId, ct);
            if (schedule is null || !schedule.IsActive)
            {
                return;
            }

            if (_tracker.IsAnyRunActive())
            {
                _tracker.SetCurrentNewsletter(schedule.TemplateBaseName);
            }

            var resolved = _templateResolver.ResolveLatest(schedule.TemplateBaseName);
            if (resolved is null)
            {
                _logger.LogWarning(
                    "No template available for {TemplateBaseName}, skipping this run.",
                    schedule.TemplateBaseName);
                if (_tracker.IsAnyRunActive())
                {
                    _tracker.SetStatusMessage($"{schedule.TemplateBaseName} için şablon bulunamadı.");
                }
                // Still advance NextRunAtUtc below - a missing template today isn't a scheduling bug.
                RecomputeNextRun(schedule);
                await _db.SaveChangesAsync(ct);
                return;
            }

            var (fileName, html) = resolved.Value;

            var today = IstanbulClock.NowLocal().Date;
            var subscribers = await _db.MailSubscribers
                .Where(s => s.IsActive && s.TemplateBaseName == schedule.TemplateBaseName)
                .ToListAsync(ct);

            var toSend = subscribers
                .Where(s => forceResend || s.LastSentAt is null || IstanbulClock.ToLocal(s.LastSentAt.Value).Date != today)
                .ToList();

            if (toSend.Count == 0)
            {
                if (_tracker.IsAnyRunActive())
                {
                    _tracker.SetStatusMessage($"{schedule.TemplateBaseName}: Tüm abonelere bugün zaten gönderilmiş (0 alıcı).");
                }
                RecomputeNextRun(schedule);
                await _db.SaveChangesAsync(ct);
                return;
            }

            var subject = BuildSubject(schedule.TemplateBaseName, fileName);

            if (_mailOptions.DryRun)
            {
                foreach (var subscriber in toSend)
                {
                    _logger.LogInformation("[DRYRUN] would send '{Subject}' to {Email}", subject, subscriber.Email);
                    if (_tracker.IsAnyRunActive())
                    {
                        _tracker.UpdateProgress(schedule.TemplateBaseName, success: true);
                    }
                }

                RecomputeNextRun(schedule);
                await _db.SaveChangesAsync(ct);
                return;
            }

            SmtpClient? client = null;
            var sentCount = 0;
            var consecutiveSmtpFailures = 0;

            try
            {
                client = new SmtpClient();
                await EnsureConnectedAsync(client, ct);

                for (var i = 0; i < toSend.Count; i++)
                {
                    if (_guardrails.MaxSendsPerRun.HasValue && sentCount >= _guardrails.MaxSendsPerRun.Value)
                    {
                        _logger.LogWarning(
                            "MaxSendsPerRun cap ({Cap}) reached, stopping run for {TemplateBaseName}.",
                            _guardrails.MaxSendsPerRun.Value, schedule.TemplateBaseName);
                        break;
                    }

                    var subscriber = toSend[i];
                    sentCount++;

                    try
                    {
                        var request = new MailSendRequest
                        {
                            ToEmail = subscriber.Email,
                            Subject = subject,
                            Html = html,
                            UnsubscribeToken = subscriber.UnsubscribeToken,
                        };

                        // Pre-flight check: ensure socket is connected and authenticated
                        await EnsureConnectedAsync(client, ct);

                        try
                        {
                            await _mailSender.SendAsync(client, request, ct);
                        }
                        catch (Exception sendEx) when (IsTransientOrConnectionError(sendEx, out var is421))
                        {
                            _logger.LogWarning(sendEx,
                                "Transient error sending to {Email} (is421={Is421}). Reconnecting and retrying once...",
                                subscriber.Email, is421);

                            if (is421)
                            {
                                // Backoff delay for Google Workspace rate-limit deferral
                                await Task.Delay(10000, ct);
                            }

                            if (client.IsConnected)
                            {
                                try { await client.DisconnectAsync(true, ct); } catch { /* ignore */ }
                            }

                            await EnsureConnectedAsync(client, ct);
                            await _mailSender.SendAsync(client, request, ct);
                        }

                        _db.EmailSendLogs.Add(new EmailSendLog
                        {
                            SubscriberId = subscriber.Id,
                            ScheduleId = schedule.Id,
                            TemplateFileName = fileName,
                            SentAtUtc = DateTime.UtcNow,
                            Success = true,
                        });

                        subscriber.LastSentAt = DateTime.UtcNow;
                        subscriber.LastSendStatus = "Sent";
                        subscriber.ConsecutiveFailureCount = 0;
                        consecutiveSmtpFailures = 0;

                        if (_tracker.IsAnyRunActive())
                        {
                            _tracker.UpdateProgress(schedule.TemplateBaseName, success: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _db.EmailSendLogs.Add(new EmailSendLog
                        {
                            SubscriberId = subscriber.Id,
                            ScheduleId = schedule.Id,
                            TemplateFileName = fileName,
                            SentAtUtc = DateTime.UtcNow,
                            Success = false,
                            ErrorMessage = ex.Message,
                        });

                        subscriber.LastSendStatus = "Failed";

                        if (IsPermanentRecipientFailure(ex))
                        {
                            subscriber.ConsecutiveFailureCount++;
                            if (subscriber.ConsecutiveFailureCount > _guardrails.MaxConsecutiveFailures)
                            {
                                subscriber.IsActive = false;
                                _logger.LogWarning(
                                    "Auto-deactivated subscriber {SubscriberId} ({Email}) after {Count} consecutive permanent failures.",
                                    subscriber.Id, subscriber.Email, subscriber.ConsecutiveFailureCount);
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Did not increment ConsecutiveFailureCount for {Email} because error was infrastructure/transient: {Message}",
                                subscriber.Email, ex.Message);
                        }

                        if (_tracker.IsAnyRunActive())
                        {
                            _tracker.UpdateProgress(schedule.TemplateBaseName, success: false, error: ex.Message);
                        }

                        if (IsDailyLimitExceeded(ex))
                        {
                            _logger.LogError(ex,
                                "CRITICAL: Daily sending limit / relay quota exceeded on Google Workspace for {Sender}. Aborting batch immediately to protect subscribers.",
                                _mailOptions.GmailOAuth.SenderAddress);
                            break;
                        }

                        if (IsTransientOrConnectionError(ex, out _) || ex is SmtpCommandException or SmtpProtocolException)
                        {
                            consecutiveSmtpFailures++;
                            if (consecutiveSmtpFailures >= 3)
                            {
                                _logger.LogError(ex,
                                    "Aborting batch for {TemplateBaseName}: {Count} consecutive connection/SMTP-level errors.",
                                    schedule.TemplateBaseName, consecutiveSmtpFailures);
                                break;
                            }
                        }
                        else
                        {
                            _logger.LogError(ex, "Failed to send mail to {Email}.", subscriber.Email);
                        }
                    }

                    // Per-message delay (skip after the very last message).
                    if (i < toSend.Count - 1)
                    {
                        await Task.Delay(Random.Shared.Next(_guardrails.MinDelayMs, _guardrails.MaxDelayMs + 1), ct);

                        if ((i + 1) % _guardrails.BatchSize == 0)
                        {
                            await Task.Delay(_guardrails.InterBatchDelayMs, ct);
                        }
                    }
                }
            }
            finally
            {
                if (client is not null)
                {
                    if (client.IsConnected)
                    {
                        try { await client.DisconnectAsync(true, ct); } catch { /* ignore */ }
                    }
                    client.Dispose();
                }
            }

            RecomputeNextRun(schedule);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DispatchPendingConfirmationEmailsAsync(CancellationToken ct = default)
        {
            const int MaxPerTick = 20;
            const int MaxAttempts = 5;

            var pending = await _db.PendingConfirmationEmails
                .Where(p => p.SentAt == null && p.AttemptCount < MaxAttempts)
                .OrderBy(p => p.CreatedAt)
                .Take(MaxPerTick)
                .ToListAsync(ct);

            if (pending.Count == 0)
            {
                return;
            }

            if (_mailOptions.DryRun)
            {
                foreach (var row in pending)
                {
                    _logger.LogInformation(
                        "[DRYRUN] would send confirmation to {Email} for {Names}", row.Email, row.TemplateDisplayNamesCsv);
                    row.SentAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync(ct);
                return;
            }

            SmtpClient? client = null;
            try
            {
                client = new SmtpClient();
                await EnsureConnectedAsync(client, ct);

                foreach (var row in pending)
                {
                    try
                    {
                        var names = row.TemplateDisplayNamesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        await EnsureConnectedAsync(client, ct);

                        try
                        {
                            await _mailSender.SendConfirmationAsync(client, row.Email, row.ConfirmToken, names, ct);
                        }
                        catch (Exception sendEx) when (IsTransientOrConnectionError(sendEx, out var is421))
                        {
                            _logger.LogWarning(sendEx,
                                "Transient error sending confirmation to {Email} (is421={Is421}). Reconnecting and retrying...",
                                row.Email, is421);

                            if (is421)
                            {
                                await Task.Delay(10000, ct);
                            }

                            if (client.IsConnected)
                            {
                                try { await client.DisconnectAsync(true, ct); } catch { /* ignore */ }
                            }

                            await EnsureConnectedAsync(client, ct);
                            await _mailSender.SendConfirmationAsync(client, row.Email, row.ConfirmToken, names, ct);
                        }

                        row.SentAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        row.AttemptCount++;
                        row.LastError = ex.Message;
                        _logger.LogError(
                            ex, "Failed to send confirmation email to {Email} (attempt {Attempt}).", row.Email, row.AttemptCount);

                        if (IsDailyLimitExceeded(ex))
                        {
                            _logger.LogError("CRITICAL: Daily sending limit exceeded during confirmation emails. Aborting confirmation queue.");
                            break;
                        }
                    }

                    await _db.SaveChangesAsync(ct);
                }
            }
            finally
            {
                if (client is not null)
                {
                    if (client.IsConnected)
                    {
                        try { await client.DisconnectAsync(true, ct); } catch { /* ignore */ }
                    }
                    client.Dispose();
                }
            }
        }

        private async Task EnsureConnectedAsync(SmtpClient client, CancellationToken ct)
        {
            if (!client.IsConnected)
            {
                var host = string.IsNullOrWhiteSpace(_mailOptions.SmtpHost) ? "smtp-relay.gmail.com" : _mailOptions.SmtpHost;
                var port = _mailOptions.SmtpPort > 0 ? _mailOptions.SmtpPort : 587;
                _logger.LogInformation("Connecting SmtpClient to {Host}:{Port} with STARTTLS...", host, port);
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
            }

            // Authenticate if credentials are provided and server advertises AUTH capability.
            // If the SMTP Relay is configured in IP-only mode without required AUTH, authentication can be bypassed.
            if (!client.IsAuthenticated && client.Capabilities.HasFlag(SmtpCapabilities.Authentication))
            {
                _logger.LogInformation("Authenticating SmtpClient via Google Workspace OAuth2...");
                var accessToken = await _tokenProvider.GetAccessTokenAsync(ct);
                await client.AuthenticateAsync(new SaslMechanismOAuth2(_mailOptions.GmailOAuth.SenderAddress, accessToken), ct);
            }
        }

        internal static bool IsTransientOrConnectionError(Exception ex, out bool isRateLimit421)
        {
            isRateLimit421 = false;
            if (ex is SmtpCommandException cmdEx)
            {
                if (cmdEx.StatusCode == SmtpStatusCode.ServiceNotAvailable || (int)cmdEx.StatusCode == 421)
                {
                    isRateLimit421 = true;
                    return true;
                }

                if ((int)cmdEx.StatusCode >= 400 && (int)cmdEx.StatusCode < 500)
                {
                    return true;
                }

                return false;
            }

            if (ex is ServiceNotConnectedException or SmtpProtocolException or SocketException or IOException)
            {
                return true;
            }

            return false;
        }

        internal static bool IsDailyLimitExceeded(Exception ex)
        {
            if (ex is SmtpCommandException cmdEx)
            {
                var msg = cmdEx.Message ?? string.Empty;
                if ((int)cmdEx.StatusCode is 550 or 452 or 554 or 421)
                {
                    if (msg.Contains("daily", StringComparison.OrdinalIgnoreCase) &&
                        (msg.Contains("limit", StringComparison.OrdinalIgnoreCase) ||
                         msg.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                         msg.Contains("exceeded", StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool IsPermanentRecipientFailure(Exception ex)
        {
            if (IsDailyLimitExceeded(ex))
            {
                return false;
            }

            if (ex is SmtpCommandException cmdEx)
            {
                return (int)cmdEx.StatusCode is 550 or 551 or 552 or 553 or 501;
            }

            return false;
        }

        private static void RecomputeNextRun(MailSchedule schedule)
        {
            switch (schedule.FrequencyType)
            {
                case MailFrequencyType.Daily:
                    var nowLocal = IstanbulClock.NowLocal();
                    var todayAtTime = nowLocal.Date + schedule.TimeOfDayLocal;
                    var nextLocal = todayAtTime > nowLocal ? todayAtTime : todayAtTime.AddDays(1);
                    schedule.NextRunAtUtc = IstanbulClock.ToUtc(nextLocal);
                    schedule.LastRunAtUtc = DateTime.UtcNow;
                    break;
                default:
                    throw new NotSupportedException(
                        $"MailFrequencyType.{schedule.FrequencyType} is not implemented yet.");
            }
        }

        private static string BuildSubject(string templateBaseName, string resolvedFileName)
        {
            var humanized = HumanizeRegex.Replace(templateBaseName, " ");

            var match = Regex.Match(resolvedFileName, @"(\d{2})(\d{2})(\d{2})");
            var dateSuffix = "";
            if (match.Success)
            {
                var dd = match.Groups[1].Value;
                var mm = match.Groups[2].Value;
                var yy = match.Groups[3].Value;
                dateSuffix = $" - {dd}.{mm}.20{yy}";
            }

            return humanized + dateSuffix;
        }
    }
}
