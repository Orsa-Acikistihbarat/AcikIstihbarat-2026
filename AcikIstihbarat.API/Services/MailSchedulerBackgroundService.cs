using AcikIstihbarat.API.Data;
using AcikIstihbarat.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AcikIstihbarat.API.Services
{
    public class MailSchedulerBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly MailOptions _mailOptions;
        private readonly ILogger<MailSchedulerBackgroundService> _logger;

        public MailSchedulerBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<MailOptions> mailOptions,
            ILogger<MailSchedulerBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _mailOptions = mailOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollOnceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled error in mail scheduler poll tick.");
                }

                await Task.Delay(TimeSpan.FromSeconds(_mailOptions.PollIntervalSeconds), stoppingToken);
            }
        }

        private async Task PollOnceAsync(CancellationToken stoppingToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var orchestrator = scope.ServiceProvider.GetRequiredService<IMailingOrchestrator>();
                try
                {
                    await orchestrator.DispatchPendingConfirmationEmailsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to dispatch pending confirmation emails.");
                }
            }

            List<int> dueScheduleIds;
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dueScheduleIds = await db.MailSchedules
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.NextRunAtUtc <= DateTime.UtcNow)
                    .Select(s => s.Id)
                    .ToListAsync(stoppingToken);
            }

            foreach (var scheduleId in dueScheduleIds)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var orchestrator = scope.ServiceProvider.GetRequiredService<IMailingOrchestrator>();
                    await orchestrator.RunScheduleAsync(scheduleId, stoppingToken);
                }
                catch (Exception ex)
                {
                    // NextRunAtUtc/LastRunAtUtc are deliberately left unchanged here so the schedule
                    // is retried on the next poll tick rather than skipped for a full cycle -
                    // ownership of that recompute belongs entirely to the orchestrator itself.
                    _logger.LogError(ex, "Failed to run mail schedule {ScheduleId}.", scheduleId);
                }
            }
        }
    }
}
