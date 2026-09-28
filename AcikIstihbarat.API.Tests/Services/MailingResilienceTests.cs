using System.IO;
using System.Net.Sockets;
using AcikIstihbarat.API.Services;
using MailKit;
using MailKit.Net.Smtp;
using Xunit;

namespace AcikIstihbarat.API.Tests.Services
{
    public class MailingResilienceTests
    {
        [Fact]
        public void ServiceNotConnectedException_IsClassifiedAsTransient_AndNotPermanent()
        {
            var ex = new ServiceNotConnectedException("The SmtpClient is not connected.");

            var isTransient = MailingOrchestrator.IsTransientOrConnectionError(ex, out var is421);
            var isPermanent = MailingOrchestrator.IsPermanentRecipientFailure(ex);

            Assert.True(isTransient);
            Assert.False(is421);
            Assert.False(isPermanent);
        }

        [Fact]
        public void SocketException_IsClassifiedAsTransient_AndNotPermanent()
        {
            var ex = new SocketException(10054); // Connection reset by peer

            var isTransient = MailingOrchestrator.IsTransientOrConnectionError(ex, out var is421);
            var isPermanent = MailingOrchestrator.IsPermanentRecipientFailure(ex);

            Assert.True(isTransient);
            Assert.False(is421);
            Assert.False(isPermanent);
        }

        [Fact]
        public void IOException_IsClassifiedAsTransient_AndNotPermanent()
        {
            var ex = new IOException("Unable to read data from the transport connection.");

            var isTransient = MailingOrchestrator.IsTransientOrConnectionError(ex, out var is421);
            var isPermanent = MailingOrchestrator.IsPermanentRecipientFailure(ex);

            Assert.True(isTransient);
            Assert.False(is421);
            Assert.False(isPermanent);
        }

        [Fact]
        public void SmtpProtocolException_IsClassifiedAsTransient_AndNotPermanent()
        {
            var ex = new SmtpProtocolException("The SMTP server has unexpectedly disconnected.");

            var isTransient = MailingOrchestrator.IsTransientOrConnectionError(ex, out var is421);
            var isPermanent = MailingOrchestrator.IsPermanentRecipientFailure(ex);

            Assert.True(isTransient);
            Assert.False(is421);
            Assert.False(isPermanent);
        }

        [Fact]
        public void SmtpCommandException_421_IsClassifiedAsTransient_AndFlagsRateLimit()
        {
            var ex = new SmtpCommandException(SmtpErrorCode.UnexpectedStatusCode, SmtpStatusCode.ServiceNotAvailable, "4.7.0 Try again later, closing transmission channel");

            var isTransient = MailingOrchestrator.IsTransientOrConnectionError(ex, out var is421);
            var isPermanent = MailingOrchestrator.IsPermanentRecipientFailure(ex);

            Assert.True(isTransient);
            Assert.True(is421);
            Assert.False(isPermanent);
        }

        [Theory]
        [InlineData(550)]
        [InlineData(551)]
        [InlineData(552)]
        [InlineData(553)]
        [InlineData(501)]
        public void SmtpCommandException_PermanentErrors_AreClassifiedAsPermanent_AndNotTransient(int statusCode)
        {
            var ex = new SmtpCommandException(
                SmtpErrorCode.RecipientNotAccepted,
                (SmtpStatusCode)statusCode,
                "Recipient mailbox rejected"
            );

            var isTransient = MailingOrchestrator.IsTransientOrConnectionError(ex, out var is421);
            var isPermanent = MailingOrchestrator.IsPermanentRecipientFailure(ex);

            Assert.False(isTransient);
            Assert.False(is421);
            Assert.True(isPermanent);
        }

        [Fact]
        public void GenericException_IsNeitherTransientNorPermanentRecipientFailure()
        {
            var ex = new InvalidOperationException("Something unexpected happened.");

            var isTransient = MailingOrchestrator.IsTransientOrConnectionError(ex, out var is421);
            var isPermanent = MailingOrchestrator.IsPermanentRecipientFailure(ex);

            Assert.False(isTransient);
            Assert.False(is421);
            Assert.False(isPermanent);
        }
    }
}
