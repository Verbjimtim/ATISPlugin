using System.Collections.Generic;
using System.Linq;
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
                Modes = matches.SelectMany(x => x.Modes).ToList(),
                Fields = matches.SelectMany(x => x.Fields).ToList(),
                Defaults = matches.SelectMany(x => x.Defaults).GroupBy(x => x.Name).Select(x => x.First()).ToList()
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

        [XmlElement("Mode")]
        public List<PresetMode> Modes { get; set; } = new List<PresetMode>();

        [XmlElement("Presets")]
        public List<PresetField> Fields { get; set; } = new List<PresetField>();

        // Starting values for template blanks at this airport, by blank name.
        [XmlElement("Default")]
        public List<PresetDefault> Defaults { get; set; } = new List<PresetDefault>();

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

        // False for modes that are never picked automatically (curfew, RRO).
        [XmlAttribute]
        public bool AutoSelect { get; set; } = true;

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
