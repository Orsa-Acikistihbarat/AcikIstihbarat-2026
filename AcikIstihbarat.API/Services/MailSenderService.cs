using System.Net;
using System.Text.RegularExpressions;
using AcikIstihbarat.API.Models.DTOs;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AcikIstihbarat.API.Services
{
    public class MailSenderService : IMailSenderService
    {
        private static readonly Regex TagStripRegex = new("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

        private readonly MailOptions _mailOptions;
        private readonly ILogger<MailSenderService> _logger;

        public MailSenderService(IOptions<MailOptions> mailOptions, ILogger<MailSenderService> logger)
        {
            _mailOptions = mailOptions.Value;
            _logger = logger;
        }

        public async Task SendAsync(SmtpClient client, MailSendRequest request, CancellationToken ct = default)
        {
            var html = PreMailer.Net.PreMailer.MoveCssInline(request.Html).Html;

            var unsubscribeUrl = $"{_mailOptions.PublicApiBaseUrl}/api/public/mail/unsubscribe?token={request.UnsubscribeToken}";

            html = InjectUnsubscribeFooter(html, unsubscribeUrl);

            var textBody = BuildPlainTextAlternative(html, unsubscribeUrl);

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_mailOptions.GmailOAuth.SenderAddress));
            message.To.Add(MailboxAddress.Parse(request.ToEmail));
            message.Subject = request.Subject;
            message.Body = new BodyBuilder
            {
                HtmlBody = html,
                TextBody = textBody,
            }.ToMessageBody();

            message.Headers.Add("List-Unsubscribe", $"<{unsubscribeUrl}>");
            message.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");

            await client.SendAsync(message, ct);
        }

        public async Task SendConfirmationAsync(
            SmtpClient client,
            string toEmail,
            Guid confirmToken,
            IReadOnlyList<string> templateDisplayNames,
            CancellationToken ct = default)
        {
            var confirmUrl = $"{_mailOptions.PublicApiBaseUrl}/api/public/mail/confirm?token={confirmToken}";

            var itemsHtml = string.Join("", templateDisplayNames.Select(n => $"<li>{WebUtility.HtmlEncode(n)}</li>"));
            var html = $"""
                <!DOCTYPE html>
                <html lang="tr">
                <body>
                    <p>Aşağıdaki bültenlere aboneliğinizi onaylamak için bağlantıya tıklayın:</p>
                    <ul>{itemsHtml}</ul>
                    <p><a href="{confirmUrl}">Aboneliği Onayla</a></p>
                </body>
                </html>
                """;

            var textBody = "Aşağıdaki bültenlere aboneliğinizi onaylamak için bağlantıya tıklayın: "
                + string.Join(", ", templateDisplayNames) + "\n\n" + confirmUrl;

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_mailOptions.GmailOAuth.SenderAddress));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Bülten aboneliğinizi onaylayın";
            message.Body = new BodyBuilder
            {
                HtmlBody = html,
                TextBody = textBody,
            }.ToMessageBody();

            await client.SendAsync(message, ct);
        }

        private static string InjectUnsubscribeFooter(string html, string unsubscribeUrl)
        {
            const string footerTemplate =
                "<div style=\"margin-top:24px;padding-top:16px;border-top:1px solid #ddd;font-size:12px;color:#888;text-align:center;\">" +
                "<a href=\"{0}\">Bu bültenden çıkmak için tıklayın</a></div>";
            var footer = string.Format(footerTemplate, unsubscribeUrl);

            var bodyCloseIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (bodyCloseIndex < 0)
            {
                return html + footer;
            }

            return html.Insert(bodyCloseIndex, footer);
        }

        private static string BuildPlainTextAlternative(string html, string unsubscribeUrl)
        {
            var stripped = TagStripRegex.Replace(html, " ");
            stripped = WebUtility.HtmlDecode(stripped);
            stripped = WhitespaceRegex.Replace(stripped, " ").Trim();
            return stripped + "\n\nBu bültenden çıkmak için: " + unsubscribeUrl;
        }
    }
}
