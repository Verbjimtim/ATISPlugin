using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ATISPlugin
{
    // Wind as written in the ATIS WIND field: DDD/SS, DDD/SS-GG, DDD-DDD/SS or DDD-DDD/SS-GG,
    // optionally followed by the ", MX XW nnKTS [RWY nn]" and ", MX TW nnKTS [RWY nn]" components this plugin adds.
    public class Wind
    {
        public int From { get; private set; }
        public int To { get; private set; }
        public int Speed { get; private set; }
        public int MaxSpeed { get; private set; }
        public bool IsVariable => From != To;
        public bool IsGusting => MaxSpeed != Speed;

        // Also matches the earlier "XW MAX nnKTS" form so ATIS saved by older builds are cleaned up.
        private static readonly Regex SuffixRegex = new Regex(",?\\s*(?:MX (?:XW|TW)|(?:XW|TW) MAX) \\d+\\s?KTS?(?: RWY \\d{2}[LRC]?)?", RegexOptions.IgnoreCase);

        private static readonly Regex WindRegex = new Regex("^(\\d{3})(?:-(\\d{3}))?/(\\d{1,3})(?:-(\\d{1,3}))?$");

        public static string StripComponents(string wind) => SuffixRegex.Replace(wind ?? string.Empty, string.Empty).Trim();

        // Returns null for CALM, VRB or anything else that has no usable direction.
        public static Wind Parse(string wind)
        {
            var match = WindRegex.Match(StripComponents(wind));

            if (!match.Success) return null;

            var from = int.Parse(match.Groups[1].Value);
            var to = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : from;
            var speed = int.Parse(match.Groups[3].Value);
            var maxSpeed = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : speed;

            return new Wind { From = from, To = to, Speed = speed, MaxSpeed = Math.Max(speed, maxSpeed) };
        }

        // A METAR wind (true) as an ATIS wind (magnetic). CALM, VRB and anything else without a direction are unchanged.
        public static string ToMagnetic(string wind, double? variation)
        {
            if (variation == null) return wind;

            var match = WindRegex.Match(wind ?? string.Empty);

            if (!match.Success) return wind;

            var direction = match.Groups[2].Success ? match.Groups[2] : match.Groups[1];

            var output = Magnetic.ToMagnetic(int.Parse(match.Groups[1].Value), variation.Value).ToString("D3");

            if (match.Groups[2].Success) output += "-" + Magnetic.ToMagnetic(int.Parse(match.Groups[2].Value), variation.Value).ToString("D3");

            return output + wind.Substring(direction.Index + direction.Length);
        }

        // A runway designator only where RWY wording puts one: at the start of the field or after "RWY"/"RWYS".
        // Never next to a digit or a decimal point, so other numbers in the field (frequencies such as
        // "FREQ 120.15", distances, times) aren't read as runways.
        private static readonly Regex RunwayInUseRegex = new Regex(
            "(?<=^\\s*|\\bRWYS?\\s+)(0[1-9]|[12]\\d|3[0-6])([LRC]?)(?![\\dLRC]|\\.\\d)((?:\\s+AND\\s+[LRC]\\b)*)");

        // Every runway in the ATIS RWY field, expanding "34L AND R" to 34L and 34R.
        public static List<string> RunwaysInUse(string text)
        {
            var output = new List<string>();

            foreach (Match match in RunwayInUseRegex.Matches(text ?? string.Empty))
            {
                var number = match.Groups[1].Value;

                output.Add(number + match.Groups[2].Value);

                foreach (Match side in Regex.Matches(match.Groups[3].Value, "[LRC]\\b"))
                {
                    output.Add(number + side.Value);
                }
            }

            return output.Distinct().ToList();
        }

        // Runway direction (e.g. 34 for 34L and 34R).
        public static string Direction(string runway) => runway.Substring(0, 2);

        // Every direction from From clockwise to To, in whole degrees.
        private IEnumerable<int> Directions()
        {
            var span = (To - From + 360) % 360;

            for (int offset = 0; offset <= span; offset++)
            {
                yield return (From + offset) % 360;
            }
        }

        // Worst case over the whole direction range, crosswind and tailwind at gust strength,
        // headwind at mean strength. Crosswind is positive from the right, negative from the left.
        public WindComponents Worst(double runwayHeading)
        {
            var output = new WindComponents { Headwind = double.MaxValue };

            foreach (var direction in Directions())
            {
                var angle = (direction - runwayHeading) * Math.PI / 180.0;

                var crosswind = MaxSpeed * Math.Sin(angle);

                if (Math.Abs(crosswind) > Math.Abs(output.Crosswind)) output.Crosswind = crosswind;

                output.Tailwind = Math.Max(output.Tailwind, -MaxSpeed * Math.Cos(angle));

                output.Headwind = Math.Min(output.Headwind, Speed * Math.Cos(angle));
            }

            return output;
        }
    }

    public class WindComponents
    {
        public double Crosswind { get; set; }
        public double Tailwind { get; set; }
        public double Headwind { get; set; }
    }
}
