using AcikIstihbarat.API.Models.DTOs;

namespace AcikIstihbarat.API.Services
{
    public interface IMailRunTracker
    {
        /// <summary>
        /// Attempts to acquire the global batch lock and initialize a new manual mail run.
        /// Returns false if another run is already active.
        /// </summary>
        bool TryStartBatchRun(List<string> newsletterKeys, int totalRecipients, out string batchId);

        /// <summary>
        /// Updates the currently active newsletter key being processed within the batch.
        /// </summary>
        void SetCurrentNewsletter(string key);

        /// <summary>
        /// Thread-safely increments SentCount and either SuccessCount or FailureCount
        /// for the specified newsletter key. If a newsletterKey is provided and does not
        /// match the active batch, the update is ignored.
        /// </summary>
        void UpdateProgress(string? newsletterKey, bool success, string? error = null);

        /// <summary>
        /// Thread-safely increments SentCount and either SuccessCount or FailureCount.
        /// </summary>
        void UpdateProgress(bool success, string? error = null);

        /// <summary>
        /// Sets an informational status message (e.g. for zero-recipient runs or phase transitions).
        /// </summary>
        void SetStatusMessage(string message);

        /// <summary>
        /// Releases the batch lock and marks the execution as finished.
        /// </summary>
        void CompleteBatchRun(string? finalMessage = null);

        /// <summary>
        /// Marks the batch run as failed and records the error.
        /// </summary>
        void FailBatchRun(string error);

        /// <summary>
        /// Returns an atomic snapshot of current run metrics for UI polling.
        /// </summary>
        MailRunStatus GetCurrentStatus();

        /// <summary>
        /// Checks whether any mail batch is currently in progress.
        /// </summary>
        bool IsAnyRunActive();
    }
}
