namespace AcikIstihbarat.API.Services
{
    /// <summary>
    /// Small shared helper for converting UTC time to Istanbul local time, used by both the
    /// template resolver (date-suffix matching) and the mailing orchestrator (idempotency /
    /// "already sent today" checks), so the conversion logic isn't duplicated.
    /// </summary>
    public static class IstanbulClock
    {
        private static readonly TimeZoneInfo IstanbulTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

        public static DateTime NowLocal() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstanbulTimeZone);

        public static DateTime ToLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), IstanbulTimeZone);

        public static DateTime ToUtc(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), IstanbulTimeZone);
    }
}
