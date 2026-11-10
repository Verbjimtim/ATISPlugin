using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using vatsys;

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

        // Where a Zulu ATIS tells pilots who to contact.
        public const string ContactPlaceholder = "{STATION} {FREQ}";

        // "STATION FREQ" for the first of the airport's contacts that's online (including this controller),
        // or null when none are, or the airport lists none.
        public static string FindContact(ZuluInfo info)
        {
            if (info?.Contacts == null || !info.Contacts.Any() || !Network.IsConnected) return null;

            var online = (Network.GetOnlineATCs ?? new List<NetworkATC>()).ToList();

            if (Network.Me != null) online.Add(Network.Me);

            foreach (var contact in info.Contacts)
            {
                if (string.IsNullOrWhiteSpace(contact?.Callsign) || string.IsNullOrWhiteSpace(contact.Station)) continue;

                var pattern = "^" + Regex.Escape(contact.Callsign.Trim()).Replace("\\*", ".*") + "$";

                foreach (var atc in online)
                {
                    if (atc?.Callsign == null || !Regex.IsMatch(atc.Callsign, pattern, RegexOptions.IgnoreCase)) continue;

                    var frequency = (atc.Frequencies ?? new int[0]).Where(x => x > 0)
                        .Select(FrequencyToString)
                        .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith(ObserverFrequency));

                    if (frequency == null) continue;

                    return $"{contact.Station.Trim().ToUpper()} {frequency}";
                }
            }

            return null;
        }

        // Network frequencies are normally FSD style (24500 for 124.5); a full kHz value (124500) is handled too.
        private static string FrequencyToString(int frequency) => frequency >= 100000
            ? (frequency / 1000.0).ToString("0.0##", CultureInfo.InvariantCulture)
            : Conversions.FSDFrequencyToString(frequency);

        // Used by observers and controllers not yet on a frequency.
        private const string ObserverFrequency = "199.998";

        // The Zulu text with the contact filled in, or the placeholders left for the controller when there's none.
        public static string Fill(string text, string contact) =>
            (text ?? string.Empty).Replace(ContactPlaceholder, contact ?? ContactPlaceholder);

        private static bool TryParseTime(string hhmm, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(hhmm ?? string.Empty, "hhmm", CultureInfo.InvariantCulture, out time);
        }
    }
}