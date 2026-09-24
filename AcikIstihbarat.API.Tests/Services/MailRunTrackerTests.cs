using AcikIstihbarat.API.Services;
using Xunit;

namespace AcikIstihbarat.API.Tests.Services
{
    public class MailRunTrackerTests
    {
        [Fact]
        public void TryStartBatchRun_WhenNotRunning_AcquiresLockAndInitializesState()
        {
            var tracker = new MailRunTracker();
            var keys = new List<string> { "AcikGazete", "AcikKose" };

            var acquired = tracker.TryStartBatchRun(keys, 100, out var batchId);

            Assert.True(acquired);
            Assert.False(string.IsNullOrWhiteSpace(batchId));
            Assert.True(tracker.IsAnyRunActive());

            var status = tracker.GetCurrentStatus();
            Assert.True(status.IsRunning);
            Assert.Equal(100, status.TotalRecipients);
            Assert.Equal(0, status.SentCount);
            Assert.Equal("AcikGazete", status.CurrentNewsletterKey);
            Assert.Equal(keys, status.ActiveNewsletterKeys);
        }

        [Fact]
        public void TryStartBatchRun_WhenAlreadyRunning_ReturnsFalse()
        {
            var tracker = new MailRunTracker();
            tracker.TryStartBatchRun(new List<string> { "AcikGazete" }, 50, out string _);

            var secondAttempt = tracker.TryStartBatchRun(new List<string> { "AcikKose" }, 20, out var secondBatchId);

            Assert.False(secondAttempt);
            Assert.Equal(string.Empty, secondBatchId);
        }

        [Fact]
        public async Task ConcurrentCallers_OnlySingleThreadAcquiresLock()
        {
            var tracker = new MailRunTracker();
            const int threadCount = 20;
            var successCount = 0;

            var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
            {
                if (tracker.TryStartBatchRun(new List<string> { "AcikGazete" }, 10, out string _))
                {
                    Interlocked.Increment(ref successCount);
                }
            }));

            await Task.WhenAll(tasks);

            Assert.Equal(1, successCount);
            Assert.True(tracker.IsAnyRunActive());
        }

        [Fact]
        public void UpdateProgress_CorrectlyTracksSentSuccessAndFailures()
        {
            var tracker = new MailRunTracker();
            tracker.TryStartBatchRun(new List<string> { "AcikGazete" }, 10, out string _);

            tracker.UpdateProgress(success: true);
            tracker.UpdateProgress(success: true);
            tracker.UpdateProgress(success: false, error: "SMTP timeout");

            var status = tracker.GetCurrentStatus();
            Assert.Equal(3, status.SentCount);
            Assert.Equal(2, status.SuccessCount);
            Assert.Equal(1, status.FailureCount);
            Assert.Equal("SMTP timeout", status.LastError);
            Assert.Equal(30.0, status.ProgressPercentage);
        }

        [Fact]
        public void CompleteBatchRun_ReleasesLockAndSetsFinishedAt()
        {
            var tracker = new MailRunTracker();
            tracker.TryStartBatchRun(new List<string> { "AcikGazete" }, 5, out string _);
            Assert.True(tracker.IsAnyRunActive());

            tracker.CompleteBatchRun();

            Assert.False(tracker.IsAnyRunActive());
            var status = tracker.GetCurrentStatus();
            Assert.False(status.IsRunning);
            Assert.NotNull(status.FinishedAtUtc);

            // Now another run should be able to start cleanly
            var secondStart = tracker.TryStartBatchRun(new List<string> { "AcikKose" }, 15, out string _);
            Assert.True(secondStart);
        }

        [Fact]
        public void UpdateProgress_WhenNewsletterKeyNotMatchingBatch_IgnoresIncrement()
        {
            var tracker = new MailRunTracker();
            tracker.TryStartBatchRun(new List<string> { "AcikGazete" }, 19, out string _);

            // Send for AcikGazete (in batch) -> increments
            tracker.UpdateProgress("AcikGazete", success: true);
            Assert.Equal(1, tracker.GetCurrentStatus().SentCount);

            // Send for AcikKose (not in batch) -> ignored
            tracker.UpdateProgress("AcikKose", success: true);
            Assert.Equal(1, tracker.GetCurrentStatus().SentCount);
            Assert.Equal(1, tracker.GetCurrentStatus().SuccessCount);
        }
    }
}
