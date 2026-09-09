namespace AcikIstihbarat.API.Services
{
    public interface IMailSenderService
    {
        Task SendAsync(MailKit.Net.Smtp.SmtpClient client, MailSendRequest request, CancellationToken ct = default);

        Task SendConfirmationAsync(
            MailKit.Net.Smtp.SmtpClient client,
            string toEmail,
            Guid confirmToken,
            IReadOnlyList<string> templateDisplayNames,
            CancellationToken ct = default);
    }
}
