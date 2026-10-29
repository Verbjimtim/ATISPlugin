using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ATISPlugin
{
    // Ranks runway modes against the current weather, following the VATPAC runway management SOP:
    // crosswind up to 20kt, tailwind up to 5kt on a dry runway and none on a wet one.
    public static class RunwaySelection
    {
        public const int MaxCrosswind = 20;
        public const int MaxTailwindDry = 5;
        public const int MaxTailwindWet = 0;

        // Modes listed without a priority come after ranked ones, and generated single runway modes after those.
        private const int Unranked = 50;
        private const int Generated = 100;
        private const int Unlimited = 99999;

        public class Conditions
        {
            public Wind Wind { get; set; }
            public int Ceiling { get; set; } = Unlimited;
            public int Visibility { get; set; } = Unlimited;
            public bool Wet { get; set; }
        }

        public class Assessment
        {
            public PresetMode Mode { get; set; }
            public bool Usable { get; set; }
            public double Tailwind { get; set; }
            public double Crosswind { get; set; }
            public double AverageCrosswind { get; set; }
        }

        public static Conditions FromMetar(string metar, bool wet)
        {
            var output = new Conditions { Wet = wet };

            if (string.IsNullOrWhiteSpace(metar)) return output;

            var processed = new METAR().Process(metar);

            output.Wind = Wind.Parse(processed?.Wind);

            if (SurfaceCondition.FromWeather(processed?.Weather) != null) output.Wet = true;

            foreach (var item in Regex.Split(metar.Trim(), "\\s+").Skip(1))
            {
                if (item == "RMK") break;

                if (item == "CAVOK") continue;

                if (Regex.IsMatch(item, "^\\d{4}$"))
                {
                    output.Visibility = Math.Min(output.Visibility, int.Parse(item) == 9999 ? 10000 : int.Parse(item));
                    continue;
                }

                // Cloud base is the lowest broken or overcast layer.
                var cloud = Regex.Match(item, "^(BKN|OVC|VV)(\\d{3})");

                if (cloud.Success) output.Ceiling = Math.Min(output.Ceiling, int.Parse(cloud.Groups[2].Value) * 100);
            }

            return output;
        }

        // Every alternative set of runways a mode could use, e.g. "10L,10R|28L,28R".
        private static List<List<string>> RunwayGroups(PresetMode mode)
        {
            if (!string.IsNullOrWhiteSpace(mode.Runways))
            {
                return mode.Runways.Split('|')
                    .Select(x => x.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList())
                    .Where(x => x.Any())
                    .ToList();
            }

            var runwayField = mode.Fields.FirstOrDefault(x => x.Name == "RWY");

            var runways = Wind.RunwaysInUse(runwayField?.Value);

            return runways.Any() ? new List<List<string>> { runways } : new List<List<string>>();
        }

        public static Assessment Assess(PresetMode mode, Conditions conditions, Func<string, double> heading)
        {
            var maxTailwind = conditions.Wet ? MaxTailwindWet : MaxTailwindDry;

            var weatherOk = conditions.Ceiling >= mode.MinCeiling
                && conditions.Visibility >= mode.MinVisibility
                && !(mode.DryOnly && conditions.Wet);

            Assessment best = null;

            foreach (var group in RunwayGroups(mode))
            {
                var assessment = new Assessment { Mode = mode };

                if (conditions.Wind != null)
                {
                    var components = group.Select(x => conditions.Wind.Worst(heading(x))).ToList();

                    assessment.Tailwind = components.Max(x => Math.Round(x.Tailwind));
                    assessment.Crosswind = components.Max(x => Math.Round(Math.Abs(x.Crosswind)));
                    assessment.AverageCrosswind = Math.Round(components.Average(x => Math.Abs(x.Crosswind)));
                }

                assessment.Usable = weatherOk && assessment.Tailwind <= maxTailwind && assessment.Crosswind <= MaxCrosswind;

                if (best == null || Compare(assessment, best) < 0) best = assessment;
            }

            // Without runways to check, the wind can't be judged, so it's never picked automatically.
            return best ?? new Assessment { Mode = mode, Usable = false, Tailwind = Unlimited, Crosswind = Unlimited, AverageCrosswind = Unlimited };
        }

        // Best first: automatic modes, usable, SOP priority, least tailwind, then least crosswind. Ties keep the
        // ATISPresets.xml order, so list approach variants best first (IVA before instrument); a variant whose
        // weather minima aren't met is unusable and drops down.
        private static int Compare(Assessment a, Assessment b)
        {
            var keys = new Func<Assessment, double>[]
            {
                x => x.Mode.AutoSelect ? 0 : 1,
                x => x.Usable ? 0 : 1,
                x => x.Mode.Priority > 0 ? x.Mode.Priority : x.Mode.Generated ? Generated : Unranked,
                x => x.Tailwind,
                x => x.Crosswind,
                x => x.AverageCrosswind
            };

            foreach (var key in keys)
            {
                var result = key(a).CompareTo(key(b));

                if (result != 0) return result;
            }

            return 0;
        }

        public static List<Assessment> Rank(IEnumerable<PresetMode> modes, Conditions conditions, Func<string, double> heading)
        {
            var assessments = modes.Select(x => Assess(x, conditions, heading)).ToList();

            // OrderBy is a stable sort, so equally ranked modes keep their ATISPresets.xml order.
            return assessments.OrderBy(x => x, Comparer<Assessment>.Create(Compare)).ToList();
        }

        // The best mode to start with, or null if none is suitable.
        public static PresetMode Best(IEnumerable<PresetMode> modes, Conditions conditions, Func<string, double> heading) =>
            Rank(modes, conditions, heading).FirstOrDefault(x => x.Mode.AutoSelect && x.Usable)?.Mode;
    }
}
