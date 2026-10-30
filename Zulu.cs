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

            foreach (var zulu in zuluInfos)
            {
                if (zulu.StartUtc.Length != 4 || zulu.EndUtc.Length != 4) continue;

                var startHour = zulu.StartUtc.Substring(0, 2);
                var startMinute = zulu.StartUtc.Substring(2, 2);
                var endHour = zulu.EndUtc.Substring(0, 2);
                var endMinute = zulu.EndUtc.Substring(2, 2);

                var startOk = DateTime.TryParseExact($"{DateTime.UtcNow:yyyy-MM-dd} {startHour}:{startMinute}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startUtc);
                var endOk = DateTime.TryParseExact($"{DateTime.UtcNow:yyyy-MM-dd} {endHour}:{endMinute}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endUtc);

                if (!startOk || !endOk) continue;

                if (DateTime.UtcNow >= startUtc && DateTime.UtcNow <= endUtc)
                {
                    return zulu;
                }
            }

            return null;
        }
    }
}