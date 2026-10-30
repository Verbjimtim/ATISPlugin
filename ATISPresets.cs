using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace ATISPlugin
{
    [XmlRoot("ATISPresets")]
    public class ATISPresets
    {
        [XmlElement("Airport")]
        public List<PresetAirport> Airports { get; set; } = new List<PresetAirport>();

        // An Airport entry can list several ICAO codes (e.g. shared templates), so every entry that
        // covers the airport is combined.
        public PresetAirport GetAirport(string icao)
        {
            var matches = Airports.Where(x => x.Covers(icao)).ToList();

            if (!matches.Any()) return null;

            if (matches.Count == 1) return matches[0];

            return new PresetAirport
            {
                ICAO = icao,
                SingleRunwayFormat = matches.Select(x => x.SingleRunwayFormat).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                ReportCrosswindFrom = matches.Select(x => x.ReportCrosswindFrom).Where(x => x != PresetAirport.DefaultReportCrosswindFrom).DefaultIfEmpty(PresetAirport.DefaultReportCrosswindFrom).First(),
                TimeZone = matches.Select(x => x.TimeZone).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                Modes = matches.SelectMany(x => x.Modes).ToList(),
                Fields = matches.SelectMany(x => x.Fields).ToList(),
                Defaults = matches.SelectMany(x => x.Defaults).GroupBy(x => x.Name).Select(x => x.First()).ToList(),
                AutoAdds = matches.SelectMany(x => x.AutoAdds).ToList()
            };
        }
    }

    public class PresetAirport
    {
        // One ICAO code, or several separated by commas.
        [XmlAttribute]
        public string ICAO { get; set; }

        public bool Covers(string icao) => (ICAO ?? string.Empty).Split(',').Any(x => x.Trim() == icao);

        // Crosswind (kt) from which MX XW is added to WIND. Above 10kt by default; metro class D report from 5kt.
        public const int DefaultReportCrosswindFrom = 11;

        [XmlAttribute]
        public int ReportCrosswindFrom { get; set; } = DefaultReportCrosswindFrom;

        // When set, adds a "RWY xx" mode for each runway, setting RWY to this text with {RWY} replaced.
        [XmlAttribute]
        public string SingleRunwayFormat { get; set; }

        // Windows time zone ID (e.g. "AUS Eastern Standard Time") for modes with local Hours.
        [XmlAttribute]
        public string TimeZone { get; set; }

        public TimeZoneInfo GetTimeZone()
        {
            if (string.IsNullOrWhiteSpace(TimeZone)) return null;

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
            }
            catch
            {
                return null;
            }
        }

        [XmlElement("Mode")]
        public List<PresetMode> Modes { get; set; } = new List<PresetMode>();

        [XmlElement("Presets")]
        public List<PresetField> Fields { get; set; } = new List<PresetField>();

        // Starting values for template blanks at this airport, by blank name.
        [XmlElement("Default")]
        public List<PresetDefault> Defaults { get; set; } = new List<PresetDefault>();

        // Text added to a field while its conditions are met, and removed once they aren't.
        [XmlElement("AutoAdd")]
        public List<PresetAutoAdd> AutoAdds { get; set; } = new List<PresetAutoAdd>();

        public List<PresetMode> GetModes(IEnumerable<string> runways)
        {
            var output = Modes.ToList();

            if (string.IsNullOrWhiteSpace(SingleRunwayFormat) || runways == null) return output;

            foreach (var runway in runways)
            {
                var name = $"RWY {runway}";

                if (output.Any(x => x.Name == name)) continue;

                var mode = new PresetMode { Name = name, Generated = true };

                mode.Fields.Add(new PresetModeField { Name = "RWY", Value = SingleRunwayFormat.Replace("{RWY}", runway) });

                output.Add(mode);
            }

            return output;
        }

        // With a mode selected: that mode's value, then presets for that mode or for all modes.
        // With no mode selected: every preset, then every value used by a mode.
        public List<string> GetPresets(string fieldName, IEnumerable<PresetMode> modes, string selectedMode)
        {
            var output = new List<string>();

            var selected = selectedMode == null ? null : modes.FirstOrDefault(x => x.Name == selectedMode);

            void Add(string preset)
            {
                var value = Clean(preset);
                if (!output.Contains(value)) output.Add(value);
            }

            if (selected != null)
            {
                foreach (var field in selected.Fields.Where(x => x.Name == fieldName)) Add(field.Value);
            }

            foreach (var field in Fields.Where(x => x.Field == fieldName && x.AppliesTo(selected?.Name)))
            {
                foreach (var preset in field.Presets) Add(preset);
            }

            if (selected == null)
            {
                foreach (var mode in modes)
                {
                    foreach (var field in mode.Fields.Where(x => x.Name == fieldName)) Add(field.Value);
                }
            }

            return output;
        }

        public bool IsMultiple(string fieldName) => Fields.Any(x => x.Field == fieldName && x.Multiple);

        // Whether the airport lists any values for the field, as presets or in a mode (an empty value counts).
        public bool DefinesField(string fieldName) =>
            Fields.Any(x => x.Field == fieldName) || Modes.Any(x => x.Fields.Any(f => f.Name == fieldName));

        // APCH options for airports that don't define their own.
        public const string ApproachField = "APCH";

        public static readonly string[] DefaultApproaches = { "EXP INST APCH", "EXP ILS APCH", "EXP GLS APCH", "EXP RNP APCH" };

        public static string Clean(string value) => (value ?? string.Empty).Trim().TrimEnd('.').Trim().ToUpper();
    }

    public class PresetMode
    {
        [XmlAttribute]
        public string Name { get; set; }

        // SOP preference, 1 being most preferred. 0 means not ranked.
        [XmlAttribute]
        public int Priority { get; set; }

        // Weather the mode needs: lowest cloud base (BKN or more, ft) and visibility (m). 0 means none.
        [XmlAttribute]
        public int MinCeiling { get; set; }

        [XmlAttribute]
        public int MinVisibility { get; set; }

        // Only usable on a dry runway (e.g. LAHSO).
        [XmlAttribute]
        public bool DryOnly { get; set; }

        // False for modes that are never picked automatically.
        [XmlAttribute]
        public bool AutoSelect { get; set; } = true;

        // Local hours the mode is for, e.g. "2300-0600" (curfew, RRO). Within them it's preferred over modes
        // without hours, and suggested as they start; outside them it's never picked automatically.
        [XmlAttribute]
        public string Hours { get; set; }

        // "true" or "false" to limit the hours to daylight saving time or standard time (e.g. AEDT and AEST wording).
        [XmlAttribute]
        public string DaylightSaving { get; set; }

        public bool IsTimed => !string.IsNullOrWhiteSpace(Hours);

        private static readonly Regex HoursRegex = new Regex("^(\\d{2})(\\d{2})-(\\d{2})(\\d{2})$");

        public bool InPeriod(DateTime utcNow, TimeZoneInfo timeZone)
        {
            if (!IsTimed || timeZone == null) return false;

            var match = HoursRegex.Match(Hours.Trim());

            if (!match.Success) return false;

            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), timeZone);

            if (bool.TryParse(DaylightSaving, out var daylightSaving) && timeZone.IsDaylightSavingTime(local) != daylightSaving) return false;

            var now = local.Hour * 60 + local.Minute;
            var start = int.Parse(match.Groups[1].Value) * 60 + int.Parse(match.Groups[2].Value);
            var end = int.Parse(match.Groups[3].Value) * 60 + int.Parse(match.Groups[4].Value);

            // Hours past midnight, e.g. 2300-0600.
            return start <= end ? now >= start && now < end : now >= start || now < end;
        }

        // Whether the mode can be picked automatically now: timed modes only within their hours.
        public bool IsAutomatic(DateTime utcNow, TimeZoneInfo timeZone) => IsTimed ? InPeriod(utcNow, timeZone) : AutoSelect;

        // Runways to assess against the wind, when the RWY text doesn't say the direction (e.g. "10L/28R").
        // Alternative directions are separated by "|", e.g. "10L,10R|28L,28R". Defaults to the runways in RWY.
        [XmlAttribute]
        public string Runways { get; set; }

        // Added from SingleRunwayFormat rather than listed in ATISPresets.xml.
        [XmlIgnore]
        public bool Generated { get; set; }

        [XmlElement("Field")]
        public List<PresetModeField> Fields { get; set; } = new List<PresetModeField>();
    }

    // e.g. <AutoAdd Field="SIGWX" Runways="34R" Wind="020-070" AboveSpeed="14">CAUTION ...</AutoAdd>
    public class PresetAutoAdd
    {
        [XmlAttribute]
        public string Field { get; set; }

        // Comma separated modes, one of which must be selected. Empty means any.
        [XmlAttribute]
        public string Modes { get; set; }

        // Comma separated runways that must all be in RWY. Empty means any.
        [XmlAttribute]
        public string Runways { get; set; }

        // Magnetic wind directions, e.g. "020-070". Met when any of the wind's direction range is within them.
        [XmlAttribute]
        public string Wind { get; set; }

        // Mean wind speed (kt) to be exceeded.
        [XmlAttribute]
        public int AboveSpeed { get; set; }

        // Crosswind (kt) to be exceeded: the worst case, as reported in WIND as MX XW, on the rule's Runways when
        // given, otherwise on any runway in RWY.
        [XmlAttribute]
        public int AboveCrosswind { get; set; }

        [XmlText]
        public string Text { get; set; }

        private static readonly Regex DirectionsRegex = new Regex("^(\\d{3})-(\\d{3})$");

        private static List<string> List(string value) =>
            (value ?? string.Empty).Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

        public bool Applies(string mode, ICollection<string> runwaysInUse, ATISPlugin.Wind wind, Func<string, double> heading)
        {
            var modes = List(Modes);

            if (modes.Any() && !modes.Contains(mode)) return false;

            if (!List(Runways).All(runwaysInUse.Contains)) return false;

            if (!string.IsNullOrWhiteSpace(Wind) || AboveSpeed > 0)
            {
                if (wind == null || wind.Speed <= AboveSpeed) return false;

                var directions = DirectionsRegex.Match((Wind ?? string.Empty).Trim());

                if (directions.Success && !wind.Overlaps(int.Parse(directions.Groups[1].Value), int.Parse(directions.Groups[2].Value))) return false;
            }

            if (AboveCrosswind > 0)
            {
                // On the rule's own runways when it lists them (e.g. 35 but not a crossing 30), otherwise any in RWY.
                var runways = List(Runways).Any() ? List(Runways) : runwaysInUse.ToList();

                if (wind == null || !runways.Any()) return false;

                var crosswind = runways.Max(x => Math.Round(Math.Abs(wind.Worst(heading(x)).Crosswind)));

                if (crosswind <= AboveCrosswind) return false;
            }

            return true;
        }
    }

    public class PresetDefault
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlText]
        public string Value { get; set; }
    }

    public class PresetModeField
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlText]
        public string Value { get; set; }
    }

    public class PresetField
    {
        [XmlAttribute]
        public string Field { get; set; }

        // When true, presets are toggled in and out of the field instead of replacing it.
        [XmlAttribute]
        public bool Multiple { get; set; }

        // Comma separated mode names these presets are limited to. Empty means all modes.
        [XmlAttribute]
        public string Modes { get; set; }

        [XmlElement("Preset")]
        public List<string> Presets { get; set; } = new List<string>();

        public bool AppliesTo(string mode)
        {
            if (string.IsNullOrWhiteSpace(Modes) || mode == null) return true;

            return Modes.Split(',').Any(x => x.Trim() == mode);
        }
    }
}
