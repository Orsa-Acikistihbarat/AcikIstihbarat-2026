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
        private readonly MailOptions _mailOptions;
        private readonly MailGuardrailOptions _guardrails;
        private readonly ILogger<MailingOrchestrator> _logger;

        public MailingOrchestrator(
            AppDbContext db,
            IMailTemplateResolver templateResolver,
            IMailSenderService mailSender,
            IGmailOAuthTokenProvider tokenProvider,
            IOptions<MailOptions> mailOptions,
            ILogger<MailingOrchestrator> logger)
        {
            _db = db;
            _templateResolver = templateResolver;
            _mailSender = mailSender;
            _tokenProvider = tokenProvider;
            _mailOptions = mailOptions.Value;
            _guardrails = mailOptions.Value.Guardrails;
            _logger = logger;
        }

        public async Task RunScheduleAsync(int scheduleId, CancellationToken ct = default)
        {
            var schedule = await _db.MailSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId, ct);
            if (schedule is null || !schedule.IsActive)
            {
                return;
            }

            var resolved = _templateResolver.ResolveLatest(schedule.TemplateBaseName);
            if (resolved is null)
            {
                _logger.LogWarning(
                    "No template available for {TemplateBaseName}, skipping this run.",
                    schedule.TemplateBaseName);
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
                .Where(s => s.LastSentAt is null || IstanbulClock.ToLocal(s.LastSentAt.Value).Date != today)
                .ToList();

            if (toSend.Count == 0)
            {
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
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, ct);
                var accessToken = await _tokenProvider.GetAccessTokenAsync(ct);
                await client.AuthenticateAsync(new SaslMechanismOAuth2(_mailOptions.GmailOAuth.SenderAddress, accessToken), ct);

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

                        await _mailSender.SendAsync(client, request, ct);

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
                        subscriber.ConsecutiveFailureCount++;

                        if (subscriber.ConsecutiveFailureCount > _guardrails.MaxConsecutiveFailures)
                        {
                            subscriber.IsActive = false;
                            _logger.LogWarning(
                                "Auto-deactivated subscriber {SubscriberId} ({Email}) after {Count} consecutive failures.",
                                subscriber.Id, subscriber.Email, subscriber.ConsecutiveFailureCount);
                        }

                        if (ex is SmtpCommandException or SmtpProtocolException)
                        {
                            consecutiveSmtpFailures++;
                            if (consecutiveSmtpFailures >= 5)
                            {
                                _logger.LogError(ex,
                                    "Aborting batch for {TemplateBaseName}: {Count} consecutive SMTP-level errors.",
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
                        await client.DisconnectAsync(true, ct);
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
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, ct);
                var accessToken = await _tokenProvider.GetAccessTokenAsync(ct);
                await client.AuthenticateAsync(new SaslMechanismOAuth2(_mailOptions.GmailOAuth.SenderAddress, accessToken), ct);

                foreach (var row in pending)
                {
                    try
                    {
                        var names = row.TemplateDisplayNamesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        await _mailSender.SendConfirmationAsync(client, row.Email, row.ConfirmToken, names, ct);
                        row.SentAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        row.AttemptCount++;
                        row.LastError = ex.Message;
                        _logger.LogError(
                            ex, "Failed to send confirmation email to {Email} (attempt {Attempt}).", row.Email, row.AttemptCount);
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
                        await client.DisconnectAsync(true, ct);
                    }
                    client.Dispose();
                }
            }
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
