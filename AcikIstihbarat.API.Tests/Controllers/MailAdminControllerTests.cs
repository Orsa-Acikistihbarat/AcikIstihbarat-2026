using AcikIstihbarat.API.Controllers.Admin;
using AcikIstihbarat.API.Data;
using AcikIstihbarat.API.Models.DTOs;
using AcikIstihbarat.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AcikIstihbarat.API.Tests.Controllers
{
    public class MailAdminControllerTests
    {
        private class DummyHostLifetime : IHostApplicationLifetime
        {
            public CancellationToken ApplicationStarted => CancellationToken.None;
            public CancellationToken ApplicationStopping => CancellationToken.None;
            public CancellationToken ApplicationStopped => CancellationToken.None;
            public void StopApplication() { }
        }

        private static MailAdminController CreateController(IMailRunTracker tracker)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=(localdb)\\dummy;Database=dummy;")
                .Options;

            var db = new AppDbContext(options);
            var lifetime = new DummyHostLifetime();
            var services = new ServiceCollection().BuildServiceProvider();
            var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();

            return new MailAdminController(db, tracker, lifetime, scopeFactory);
        }

        [Fact]
        public async Task TriggerManualSend_WithEmptyKeys_ReturnsBadRequest()
        {
            var tracker = new MailRunTracker();
            var controller = CreateController(tracker);

            var result = await controller.TriggerManualSend(new TriggerMailRequest
            {
                NewsletterKeys = new List<string>()
            }, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task TriggerManualSend_WithInvalidKey_ReturnsBadRequest()
        {
            var tracker = new MailRunTracker();
            var controller = CreateController(tracker);

            var result = await controller.TriggerManualSend(new TriggerMailRequest
            {
                NewsletterKeys = new List<string> { "NonExistentNewsletter" }
            }, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task TriggerManualSend_WhenRunAlreadyActive_ReturnsConflict()
        {
            var tracker = new MailRunTracker();
            tracker.TryStartBatchRun(new List<string> { "AcikGazete" }, 50, out _);

            var controller = CreateController(tracker);

            var result = await controller.TriggerManualSend(new TriggerMailRequest
            {
                NewsletterKeys = new List<string> { "AcikGazete" }
            }, CancellationToken.None);

            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.NotNull(conflict.Value);
        }

        [Fact]
        public void GetStatus_ReturnsCurrentStatusFromTracker()
        {
            var tracker = new MailRunTracker();
            tracker.TryStartBatchRun(new List<string> { "AcikGazete" }, 75, out _);
            tracker.UpdateProgress(success: true);

            var controller = CreateController(tracker);

            var result = controller.GetStatus();

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<MailRunStatusDto>(ok.Value);
            Assert.True(dto.IsRunning);
            Assert.Equal(75, dto.TotalRecipients);
            Assert.Equal(1, dto.SentCount);
            Assert.Equal(1, dto.SuccessCount);
        }
    }
}
