using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ATISPlugin
{
    // Windshear reports in the SIGWX field, which end "AT TIME hhmm".
    public static class Windshear
    {
        public const string Field = "SIGWX";

        public static readonly TimeSpan ExpireAfter = TimeSpan.FromHours(1);

        private static readonly Regex ReportRegex = new Regex("WIND ?SHEAR.*\\bAT TIME (\\d{2})(\\d{2})\\b");

        // Entries are joined with ". ", but one windshear format has ". AT TIME" inside it.
        private static List<string> Entries(string text)
        {
            var output = new List<string>();

            foreach (var segment in Regex.Split(text ?? string.Empty, "\\.\\s+"))
            {
                if (string.IsNullOrWhiteSpace(segment)) continue;

                if (segment.StartsWith("AT TIME") && output.Any())
                {
                    output[output.Count - 1] += ". " + segment;
                }
                else
                {
                    output.Add(segment.Trim());
                }
            }

            return output;
        }

        // The field with any windshear reported more than an hour ago removed, keeping other entries,
        // or null when nothing has expired.
        public static string RemoveExpired(string text, DateTime utcNow)
        {
            var entries = Entries(text);

            var kept = entries.Where(x =>
            {
                var match = ReportRegex.Match(x);

                if (!match.Success) return true;

                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);

                if (hours > 23 || minutes > 59) return true;

                var reported = utcNow.Date.AddHours(hours).AddMinutes(minutes);

                // A time later than now was reported yesterday.
                if (reported > utcNow) reported = reported.AddDays(-1);

                return utcNow - reported <= ExpireAfter;
            }).ToList();

            return kept.Count == entries.Count ? null : string.Join(". ", kept);
        }
    }
}
