using System;
using System.Linq;

namespace ATISPlugin
{
    // Runway surface condition (SFC COND) rules.
    public static class SurfaceCondition
    {
        public const string Field = "SFC COND";

        public const string Dry = "DRY";

        // Most to least significant. Anything else typed into SFC COND is left alone.
        public static readonly string[] Conditions = { "SNOW", "STANDING WATER", "WET", "DAMP" };

        public static int Rank(string condition)
        {
            var value = (condition ?? string.Empty).Trim();

            if (value.Length == 0 || value == Dry) return 0;

            var index = Array.IndexOf(Conditions, value);

            return index < 0 ? int.MaxValue : Conditions.Length - index;
        }

        // How long after the precipitation stops before the runway is reported DRY.
        public static TimeSpan? ClearAfter(string condition)
        {
            switch ((condition ?? string.Empty).Trim())
            {
                case "SNOW": return TimeSpan.FromHours(2);
                case "STANDING WATER": return TimeSpan.FromHours(1);
                case "WET": return TimeSpan.FromHours(1);
                case "DAMP": return TimeSpan.FromMinutes(30);
                default: return null;
            }
        }

        // The runway condition implied by precipitation in the WX field, or null if there's none.
        public static string FromWeather(string weather)
        {
            string output = null;

            foreach (var token in (weather ?? string.Empty).Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                // In the vicinity, not at the aerodrome.
                if (token.StartsWith("VC")) continue;

                var light = token.StartsWith("-");

                var codes = token.TrimStart('+', '-');

                if (codes.Length % 2 != 0) continue;

                var groups = Enumerable.Range(0, codes.Length / 2).Select(x => codes.Substring(x * 2, 2)).ToList();

                string condition = null;

                if (groups.Contains("SN") || groups.Contains("SG"))
                {
                    condition = "SNOW";
                }
                else if (groups.Any(x => x == "GR" || x == "GS" || x == "PL"))
                {
                    condition = "WET";
                }
                else if (groups.Any(x => x == "RA" || x == "DZ" || x == "UP"))
                {
                    condition = light ? "DAMP" : "WET";
                }

                if (condition != null && Rank(condition) > Rank(output)) output = condition;
            }

            return output;
        }
    }
}
