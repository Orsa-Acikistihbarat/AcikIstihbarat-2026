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

            var itemsHtml = string.Join("", templateDisplayNames.Select(n =>
                $"<li style=\"padding:4px 0;\">{WebUtility.HtmlEncode(n)}</li>"));
            var html = $"""
                <!DOCTYPE html>
                <html lang="tr">
                <body style="margin:0;padding:0;background-color:#f4f4f4;">
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f4f4;padding:24px 0;">
                        <tr>
                            <td align="center">
                                <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background-color:#ffffff;border:1px solid #0891b2;border-radius:8px;overflow:hidden;">
                                    <tr>
                                        <td style="background-color:#06b6d4;padding:16px 24px;">
                                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                                                <tr>
                                                    <td align="left" width="33%" style="font-size:16px;font-weight:bold;color:#ffffff;font-family:Arial,Helvetica,sans-serif;">Açık İstihbarat</td>
                                                    <td align="center" width="34%" style="font-size:16px;font-weight:bold;color:#ffffff;font-family:Arial,Helvetica,sans-serif;">Bültenler</td>
                                                    <td width="33%">&nbsp;</td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="padding:32px 24px;text-align:center;font-family:Arial,Helvetica,sans-serif;color:#333333;">
                                            <p style="margin:0 0 16px;font-size:15px;line-height:1.5;">Aşağıdaki Açık İstihbarat bültenlerine abonelik başvurunuzu lütfen teyit edin.</p>
                                            <ul style="list-style:none;margin:0 0 24px;padding:0;font-size:15px;font-weight:bold;color:#0e7490;">{itemsHtml}</ul>
                                            <a href="{confirmUrl}" style="display:inline-block;background-color:#0891b2;color:#ffffff;text-decoration:none;font-family:Arial,Helvetica,sans-serif;font-size:15px;font-weight:bold;padding:12px 28px;border-radius:6px;">Aboneliği Onayla</a>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
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
