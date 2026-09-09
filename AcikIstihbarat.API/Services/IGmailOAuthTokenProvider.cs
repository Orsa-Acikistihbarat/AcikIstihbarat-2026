namespace AcikIstihbarat.API.Services
{
    public interface IGmailOAuthTokenProvider
    {
        Task<string> GetAccessTokenAsync(CancellationToken ct = default);
    }
}
