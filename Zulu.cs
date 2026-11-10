using System;
using System.Globalization;
using System.Linq;

namespace ATISPlugin
{
    public class Zulu
    {
        public static ZuluInfo GetInfo(string icao)
        {
            var zuluInfos = Plugin.ZuluInfo.Where(x => x.ICAO == icao);

            if (!zuluInfos.Any()) return null;

            if (zuluInfos.Count() == 1)
            {
                return zuluInfos.First();
            }

            var now = DateTime.UtcNow.TimeOfDay;

            foreach (var zulu in zuluInfos)
            {
                if (!TryParseTime(zulu.StartUtc, out var start) || !TryParseTime(zulu.EndUtc, out var end)) continue;

                // The end time covers its whole minute, so 1259 runs until 1300 and 2359 until midnight.
                end += TimeSpan.FromMinutes(1);

                var inWindow = start <= end ? now >= start && now < end : now >= start || now < end;

                if (inWindow) return zulu;
            }

            // An entry without times applies whenever none of the timed ones do.
            return zuluInfos.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.StartUtc) && string.IsNullOrWhiteSpace(x.EndUtc));
        }

        private static bool TryParseTime(string hhmm, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(hhmm ?? string.Empty, "hhmm", CultureInfo.InvariantCulture, out time);
        }
    }
}