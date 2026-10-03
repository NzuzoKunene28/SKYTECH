using System;

namespace SkyTech.Services
{
    // One source of truth for "now" and "today" in South Africa, regardless of
    // the server's own clock or time zone (Azure runs in UTC).
    public static class SaTime
    {
        private static readonly TimeZoneInfo? Zone = Find();

        private static TimeZoneInfo? Find()
        {
            foreach (var id in new[] { "Africa/Johannesburg", "South Africa Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            return null;
        }

        public static DateTime Now =>
            Zone == null ? DateTime.Now : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

        public static DateTime Today => Now.Date;
    }
}