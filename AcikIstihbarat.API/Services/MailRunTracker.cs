using AcikIstihbarat.API.Models.DTOs;

namespace AcikIstihbarat.API.Services
{
    public class MailRunTracker : IMailRunTracker
    {
        private readonly object _lock = new();
        private MailRunStatus _currentStatus = new();

        public bool TryStartBatchRun(List<string> newsletterKeys, int totalRecipients, out string batchId)
        {
            lock (_lock)
            {
                if (_currentStatus.IsRunning)
                {
                    batchId = string.Empty;
                    return false;
                }

                batchId = Guid.NewGuid().ToString("N");
                _currentStatus = new MailRunStatus
                {
                    BatchId = batchId,
                    ActiveNewsletterKeys = new List<string>(newsletterKeys ?? new List<string>()),
                    CurrentNewsletterKey = newsletterKeys?.FirstOrDefault(),
                    IsRunning = true,
                    TotalRecipients = totalRecipients,
                    SentCount = 0,
                    SuccessCount = 0,
                    FailureCount = 0,
                    StatusMessage = totalRecipients == 0
                        ? "Gönderilecek yeni abone bulunamadı (Tüm abonelere bugün zaten gönderilmiş)."
                        : "Gönderim başlatıldı.",
                    StartedAtUtc = DateTime.UtcNow,
                    FinishedAtUtc = null,
                    LastError = null
                };
                return true;
            }
        }

        public void SetCurrentNewsletter(string key)
        {
            lock (_lock)
            {
                _currentStatus.CurrentNewsletterKey = key;
            }
        }

        public void UpdateProgress(string? newsletterKey, bool success, string? error = null)
        {
            lock (_lock)
            {
                if (!_currentStatus.IsRunning) return;

                if (!string.IsNullOrEmpty(newsletterKey) &&
                    _currentStatus.ActiveNewsletterKeys.Count > 0 &&
                    !_currentStatus.ActiveNewsletterKeys.Contains(newsletterKey))
                {
                    return;
                }

                _currentStatus.SentCount++;
                if (success)
                {
                    _currentStatus.SuccessCount++;
                }
                else
                {
                    _currentStatus.FailureCount++;
                    if (!string.IsNullOrEmpty(error))
                    {
                        _currentStatus.LastError = error;
                    }
                }
            }
        }

        public void UpdateProgress(bool success, string? error = null)
            => UpdateProgress(null, success, error);

        public void SetStatusMessage(string message)
        {
            lock (_lock)
            {
                _currentStatus.StatusMessage = message;
            }
        }

        public void CompleteBatchRun(string? finalMessage = null)
        {
            lock (_lock)
            {
                _currentStatus.IsRunning = false;
                _currentStatus.FinishedAtUtc = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(finalMessage))
                {
                    _currentStatus.StatusMessage = finalMessage;
                }
                else if (_currentStatus.TotalRecipients == 0)
                {
                    _currentStatus.StatusMessage ??= "Gönderilecek yeni abone bulunamadı (Tüm abonelere bugün zaten gönderilmiş).";
                }
                else
                {
                    _currentStatus.StatusMessage = $"Tamamlandı ({_currentStatus.SuccessCount} başarılı, {_currentStatus.FailureCount} hatalı).";
                }
            }
        }

        public void FailBatchRun(string error)
        {
            lock (_lock)
            {
                _currentStatus.IsRunning = false;
                _currentStatus.FinishedAtUtc = DateTime.UtcNow;
                _currentStatus.LastError = error;
                _currentStatus.StatusMessage = $"Gönderim hata ile durduruldu: {error}";
            }
        }

        public MailRunStatus GetCurrentStatus()
        {
            lock (_lock)
            {
                return new MailRunStatus
                {
                    BatchId = _currentStatus.BatchId,
                    ActiveNewsletterKeys = new List<string>(_currentStatus.ActiveNewsletterKeys),
                    CurrentNewsletterKey = _currentStatus.CurrentNewsletterKey,
                    IsRunning = _currentStatus.IsRunning,
                    TotalRecipients = _currentStatus.TotalRecipients,
                    SentCount = _currentStatus.SentCount,
                    SuccessCount = _currentStatus.SuccessCount,
                    FailureCount = _currentStatus.FailureCount,
                    StatusMessage = _currentStatus.StatusMessage,
                    StartedAtUtc = _currentStatus.StartedAtUtc,
                    FinishedAtUtc = _currentStatus.FinishedAtUtc,
                    LastError = _currentStatus.LastError
                };
            }
        }

        public bool IsAnyRunActive()
        {
            lock (_lock)
            {
                return _currentStatus.IsRunning;
            }
        }
    }
}
