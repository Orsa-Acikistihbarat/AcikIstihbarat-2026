namespace AcikIstihbarat.API.Services
{
    public interface IMailingOrchestrator
    {
        Task RunScheduleAsync(int scheduleId, CancellationToken ct = default);

        Task DispatchPendingConfirmationEmailsAsync(CancellationToken ct = default);
    }
}
