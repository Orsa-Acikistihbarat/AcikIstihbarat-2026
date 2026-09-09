namespace AcikIstihbarat.API.Services
{
    /// <summary>
    /// Thrown when the Gmail OAuth2 refresh token is rejected (e.g. revoked, expired due to the
    /// consent screen being stuck in "Testing" publishing status). Callers should log this as an
    /// actionable "someone needs to redo the OAuth bootstrap" message rather than a generic HTTP
    /// error.
    /// </summary>
    public class GmailReauthRequiredException : Exception
    {
        public GmailReauthRequiredException(string message) : base(message)
        {
        }

        public GmailReauthRequiredException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
