using System;
using System.Text.RegularExpressions;

namespace ATISPlugin
{
    // Runway changes in the OPR INFO field, written "FM TIME hhmm, EXP RWY ...".
    public static class RunwayChange
    {
        public const string Field = "OPR INFO";

        private static readonly Regex TimeRegex = new Regex("\\bFM TIME (\\d{2})(\\d{2})\\b");

        // The first FM time in the text that has been reached, as hhmm, or null when there's none.
        // A time is taken as the nearest one to now, so 0005 is 15 minutes away at 2350, and 2355 has passed at 0010.
        public static string Reached(string text, DateTime utcNow)
        {
            foreach (Match match in TimeRegex.Matches(text ?? string.Empty))
            {
                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);

                if (hours > 23 || minutes > 59) continue;

                var time = utcNow.Date.AddHours(hours).AddMinutes(minutes);

                if (time - utcNow > TimeSpan.FromHours(12)) time = time.AddDays(-1);
                else if (utcNow - time > TimeSpan.FromHours(12)) time = time.AddDays(1);

                if (time <= utcNow) return match.Groups[1].Value + match.Groups[2].Value;
            }

            return null;
        }
    }
}
