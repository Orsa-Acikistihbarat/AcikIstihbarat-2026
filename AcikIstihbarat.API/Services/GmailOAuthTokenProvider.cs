using System.Text.Json.Serialization;
using AcikIstihbarat.API.Models.DTOs;
using Microsoft.Extensions.Options;

namespace AcikIstihbarat.API.Services
{
    /// <summary>
    /// Singleton so the cached access token survives across scoped requests / background service
    /// poll ticks. Guards concurrent refreshes with a semaphore to avoid refresh-storm requests
    /// against Google's token endpoint when a batch send is in progress.
    /// </summary>
    public class GmailOAuthTokenProvider : IGmailOAuthTokenProvider
    {
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GmailOAuthOptions _options;
        private readonly ILogger<GmailOAuthTokenProvider> _logger;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        private string? _accessToken;
        private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

        public GmailOAuthTokenProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<MailOptions> mailOptions,
            ILogger<GmailOAuthTokenProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = mailOptions.Value.GmailOAuth;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
        {
            if (_accessToken is not null && _expiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                return _accessToken;
            }

            await _refreshLock.WaitAsync(ct);
            try
            {
                // Re-check after acquiring the lock in case another caller already refreshed it.
                if (_accessToken is not null && _expiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
                {
                    return _accessToken;
                }

                var client = _httpClientFactory.CreateClient("GoogleOAuth");
                var form = new Dictionary<string, string>
                {
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret,
                    ["refresh_token"] = _options.RefreshToken,
                    ["grant_type"] = "refresh_token",
                };

                using var response = await client.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form), ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode || body.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError(
                        "Gmail OAuth token refresh failed (status {StatusCode}): {Body}. The refresh token likely " +
                        "needs to be re-issued via the one-time bootstrap tool.",
                        response.StatusCode, body);
                    throw new GmailReauthRequiredException(
                        "Gmail OAuth refresh token was rejected; re-run the OAuth bootstrap tool to obtain a new one.");
                }

                var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(body)
                    ?? throw new GmailReauthRequiredException("Gmail OAuth token endpoint returned an empty/unparseable response.");

                _accessToken = tokenResponse.AccessToken;
                _expiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

                return _accessToken;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }
    }
}
