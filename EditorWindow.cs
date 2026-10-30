using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using vatsys;
using VATSYSControls;

namespace ATISPlugin
{
    public partial class EditorWindow : BaseForm
    {
        private ATISControl Control
        {   
            get
            {
                if (Number == 1) return Plugin.ATIS1;
                else if (Number == 2) return Plugin.ATIS2;
                else if (Number == 3) return Plugin.ATIS3;
                else return Plugin.ATIS4;
            }
        }
        private string ICAO { get; set; }
        private char ID { get; set; }
        private string DiplayName => Airport == null ? ICAO : $"{ICAO} - {Airport.FullName}";
        private Airspace2.Airport Airport => Airspace2.GetAirport(ICAO);
        public int Number { get; private set; } = 1;
        private Dictionary<string, string> Saves { get; set; } = new Dictionary<string, string>();
        private string Mode { get; set; }
        private PresetAirport PresetAirport => ICAO == null ? null : Plugin.Presets.GetAirport(ICAO);
        private Dictionary<int, PresetButton> PresetButtons { get; set; } = new Dictionary<int, PresetButton>();
        private bool Edits => Saves.Any() || 
            TimeCheck != Control.TimeCheck || 
            ID != Control.ID || 
            Mode != Control.Mode || 
            Control.PromptRate != Rate || 
            Control.VoiceName != VoiceName;
        private bool TimeCheck { get; set; }
        public EventHandler<RefreshEventArgs> RefreshEvent { get; set; }
        public string VoiceName { get; set; }
        public PromptRate Rate { get; set; } 
        private string ZuluFrequency { get; set; }
        public bool CanRecord => Edits == false && Control.Broadcasting == false ? true : false;
        private bool Initialized { get; set; }

        public EditorWindow()
        {
            InitializeComponent();

            BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);

            ComboBoxTimeCheck.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ComboBoxTimeCheck.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            ComboBoxTimeCheck.FocusColor = Colours.GetColour(Colours.Identities.HighlightedText);
            ComboBoxTimeCheck.Font = MMI.eurofont_winsml;

            ComboBoxAirport.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ComboBoxAirport.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            ComboBoxAirport.FocusColor = Colours.GetColour(Colours.Identities.HighlightedText);
            ComboBoxAirport.Font = MMI.eurofont_winsml;

            ComboBoxLetter.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ComboBoxLetter.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            ComboBoxLetter.FocusColor = Colours.GetColour(Colours.Identities.HighlightedText);
            ComboBoxLetter.Font = MMI.eurofont_winsml;

            CreatePresetButtons();

            Initialized = true;

            RefreshEvent += OnRefreshEvent;
        }

        public void Change(int number)
        {
            try
            {
                Number = number;

                ICAO = Control.ICAO;

                ID = Control.ID;

                Mode = Control.Mode;

                TimeCheck = Control.TimeCheck;

                Rate = Control.PromptRate;

                VoiceName = Control.VoiceName;

                Saves.Clear();

                LoadOptions();

                RefreshForm_ClearWindCalculator();

                RefreshForm();
            }
            catch { }
        }

        private void OnRefreshEvent(object sender, RefreshEventArgs e)
        {
            if (!IsHandleCreated)
            {
                CreateHandle();
            }

            if (e != null && e.Number != Number)
            {
                Change(Number);

                return;
            }

            RefreshForm();
        }

        private void EditorWindow_Load(object sender, EventArgs e)
        {
            Change(1);
        }

        private void LoadRunways()
        {
            ComboBoxRunway.Items.Clear();

            ComboBoxRunway.Items.Add("");

            if (ICAO == null) return;

            var airport = Airspace2.GetAirport(ICAO);

            if (airport != null)
            {
                foreach (var runway in airport.Runways.OrderBy(x => x.Name))
                {
                    ComboBoxRunway.Items.Add($"RWY {runway.Name.ToString().PadLeft(2, '0')}");
                }
            }
            else
            {
                for (int i = 1; i <= 36; i++)
                {
                    ComboBoxRunway.Items.Add($"RWY {i.ToString().PadLeft(2, '0')}");
                }
            }
        }

        private void LoadCodes()
        {
            ComboBoxLetter.Items.Clear();

            var codeBlock = Plugin.CodeBlocks.FirstOrDefault(x => x.ICAO == ICAO);

            if (codeBlock == null)
            {
                for (char c = 'A'; c <= 'Y'; c++)
                {
                    ComboBoxLetter.Items.Add(c.ToString());
                }
            }
            else
            {
                var codes = codeBlock.Codes.Split(',');
                foreach (var code in codes)
                {
                    ComboBoxLetter.Items.Add(code);
                }
            }

            if (ID == 'Z')
            {
                ComboBoxLetter.SelectedIndex = 0;

                return;
            }

            var codeValid = ComboBoxLetter.Items.IndexOf(ID.ToString()) != -1;

            if (codeValid) return;

            ComboBoxLetter.SelectedIndex = 0;
        }

        private List<PresetMode> Modes { get; set; } = new List<PresetMode>();

        private IEnumerable<string> GetRunwayNames()
        {
            if (Airport == null) return Enumerable.Empty<string>();

            return Airport.Runways
                .Select(x => x.Name.ToString().PadLeft(2, '0'))
                .Where(x => Regex.IsMatch(x, "^\\d{2}[LRC]?$"))
                .Distinct()
                .OrderBy(x => x);
        }

        // Runways named in the RWY field being edited, e.g. 34L and 34R from "34L AND R FOR ARRS AND DEPS".
        private List<string> GetRunwaysInUse()
        {
            var runwayLine = Control.Lines.FirstOrDefault(x => x.Name == "RWY");

            var runwayBox = runwayLine == null ? null : GetTextBox(runwayLine.Number);

            return runwayBox == null ? new List<string>() : Wind.RunwaysInUse(runwayBox.Text);
        }

        private void LoadModes()
        {
            Modes = PresetAirport?.GetModes(GetRunwayNames()) ?? new List<PresetMode>();

            RefreshForm_Presets();
        }

        private bool IsMultiple(string fieldName) => PresetAirport != null && PresetAirport.IsMultiple(fieldName);

        // Options for a field's arrow button: built in SFC COND options, then presets from ATISPresets.xml.
        private List<string> GetOptions(ATISLine line)
        {
            var options = new List<string>();

            if (line.Name == SurfaceCondition.Field)
            {
                var saved = PresetAirport.Clean(line.Value);

                // DRY is only reported after the runway was last reported as something else.
                if (!string.IsNullOrEmpty(saved) && saved != SurfaceCondition.Dry) options.Add(SurfaceCondition.Dry);

                options.AddRange(SurfaceCondition.Conditions.Reverse());

                if (saved == SurfaceCondition.Dry) options.Add(string.Empty);
            }

            if (PresetAirport != null)
            {
                options.AddRange(PresetAirport.GetPresets(line.Name, Modes, Mode).Where(x => !options.Contains(x)));
            }

            return options;
        }

        // Raises SFC COND to match the precipitation in WX. Never lowers it, as the runway stays wet after
        // the rain stops; ATISControl suggests DRY once the condition has had time to clear.
        private void UpdateSurfaceCondition()
        {
            if (Control.IsZulu) return;

            var weatherLine = Control.Lines.FirstOrDefault(x => x.METARField == METARField.Weather);
            var surfaceLine = Control.Lines.FirstOrDefault(x => x.Name == SurfaceCondition.Field);

            var weatherBox = weatherLine == null ? null : GetTextBox(weatherLine.Number);
            var surfaceBox = surfaceLine == null ? null : GetTextBox(surfaceLine.Number);

            if (weatherBox == null || surfaceBox == null) return;

            var condition = SurfaceCondition.FromWeather(weatherBox.Text);

            if (condition == null) return;

            if (SurfaceCondition.Rank(condition) <= SurfaceCondition.Rank(surfaceBox.Text)) return;

            surfaceBox.Text = condition;
        }

        // Shows the arrow button only on fields with presets for the selected mode, and greys it out
        // when there's nothing to choose between (a single option that replaces the field).
        private void RefreshForm_PresetButtons(bool enabled)
        {
            foreach (var presetButton in PresetButtons)
            {
                var textBox = GetTextBox(presetButton.Key);

                var line = Control.Lines.FirstOrDefault(x => x.Number == presetButton.Key);

                var presets = line == null ? new List<string>() : GetOptions(line);

                var hasPresets = presets.Any();

                presetButton.Value.Tag = hasPresets ? line.Name : null;

                presetButton.Value.Enabled = enabled && (presets.Count > 1 || (hasPresets && (IsMultiple(line.Name) || Template.IsTemplate(presets[0]))));

                var width = hasPresets ? TextBoxWidth - PresetButtonWidth - 4 : TextBoxWidth;

                if (textBox.Width != width) textBox.Width = width;
            }
        }

        private const int TextBoxWidth = 492;
        // Arrow buttons are square, as tall as the text boxes (which size themselves to the font).
        private int PresetButtonWidth => TextBox1.Height;
        private ContextMenuStrip FieldMenu;
        private PresetButton ButtonMode;

        private void CreatePresetButtons()
        {
            FieldMenu = PresetMenu.Create(ComboBoxLetter.MenuRenderer);

            TextBoxMode.Width = TextBoxWidth - PresetButtonWidth - 4;

            ButtonMode = new PresetButton
            {
                Name = "ButtonMode",
                Location = new System.Drawing.Point(TextBoxMode.Left + TextBoxWidth - PresetButtonWidth, TextBoxMode.Top),
                Size = new System.Drawing.Size(PresetButtonWidth, PresetButtonWidth)
            };

            ButtonMode.Click += (s, e) => ShowModeMenu();

            Controls.Add(ButtonMode);

            for (int number = 1; number <= 12; number++)
            {
                var textBox = GetTextBox(number);

                var button = new PresetButton
                {
                    Name = $"ButtonPreset{number}",
                    Location = new System.Drawing.Point(textBox.Left + TextBoxWidth - PresetButtonWidth, textBox.Top),
                    Size = new System.Drawing.Size(PresetButtonWidth, PresetButtonWidth),
                    Visible = false
                };

                var buttonNumber = number;

                button.Click += (s, e) => ShowPresetMenu(buttonNumber);

                Controls.Add(button);

                PresetButtons.Add(number, button);
            }
        }

        private TextField GetTextBox(int number)
        {
            switch (number)
            {
                case 1: return TextBox1;
                case 2: return TextBox2;
                case 3: return TextBox3;
                case 4: return TextBox4;
                case 5: return TextBox5;
                case 6: return TextBox6;
                case 7: return TextBox7;
                case 8: return TextBox8;
                case 9: return TextBox9;
                case 10: return TextBox10;
                case 11: return TextBox11;
                case 12: return TextBox12;
                default: return null;
            }
        }

        private void ShowPresetMenu(int number)
        {
            var textBox = GetTextBox(number);

            var line = Control.Lines.FirstOrDefault(x => x.Number == number);

            if (textBox == null || line == null || PresetButtons[number].Tag == null) return;

            var multiple = IsMultiple(line.Name);

            FieldMenu.Items.Clear();

            foreach (var preset in GetOptions(line))
            {
                if (multiple && string.IsNullOrEmpty(preset)) continue;

                var isTemplate = Template.IsTemplate(preset);

                var item = PresetMenu.Item(
                    string.IsNullOrEmpty(preset) ? "(BLANK)" : preset,
                    multiple && !isTemplate && !string.IsNullOrEmpty(preset) && ContainsPreset(textBox.Text, preset));

                item.Click += (s, e) =>
                {
                    var value = preset;

                    if (isTemplate)
                    {
                        // Windshear is reported on a runway in use; a runway change can be to any runway.
                        var runways = line.Name == Windshear.Field ? GetRunwaysInUse() : GetRunwayNames().ToList();

                        if (!runways.Any()) runways = GetRunwayNames().ToList();

                        var defaults = PresetAirport?.Defaults.ToDictionary(x => x.Name, x => x.Value) ?? new Dictionary<string, string>();

                        using (var templateWindow = new TemplateWindow(preset, runways, defaults, ComboBoxLetter.MenuRenderer))
                        {
                            if (templateWindow.ShowDialog(this) != DialogResult.OK) return;

                            value = templateWindow.Result;
                        }
                    }

                    textBox.Text = multiple ? TogglePreset(textBox.Text, value) : value;
                };

                FieldMenu.Items.Add(item);
            }

            FieldMenu.Show(textBox, new System.Drawing.Point(0, textBox.Height));
        }

        private void ShowModeMenu()
        {
            if (!ButtonMode.Enabled) return;

            FieldMenu.Items.Clear();

            // Best first for the current wind and weather, when there's a METAR to judge by.
            var ordered = string.IsNullOrWhiteSpace(Control.METARRaw)
                ? Modes
                : RunwaySelection.Rank(Modes, GetRunwayConditions(), RunwayHeading).Select(x => x.Mode).ToList();

            foreach (var name in new[] { (string)null }.Concat(ordered.Select(x => x.Name)))
            {
                var item = PresetMenu.Item(name ?? "(NONE)");

                item.Click += (s, e) => SelectMode(name);

                FieldMenu.Items.Add(item);
            }

            FieldMenu.Show(TextBoxMode, new System.Drawing.Point(0, TextBoxMode.Height));
        }

        private void TextBoxMode_MouseUp(object sender, MouseEventArgs e)
        {
            ShowModeMenu();
        }

        private static bool ContainsPreset(string text, string preset) =>
            (text ?? string.Empty).IndexOf(preset, StringComparison.OrdinalIgnoreCase) >= 0;

        // Adds the preset to the end of the field, or removes it if it's already there.
        private static string TogglePreset(string text, string preset)
        {
            var current = (text ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(preset)) return current;

            var index = current.IndexOf(preset, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
            {
                return string.IsNullOrEmpty(current) ? preset : $"{current.TrimEnd('.', ' ')}. {preset}";
            }

            current = current.Remove(index, preset.Length);
            current = Regex.Replace(current, "(\\s*\\.\\s*){2,}", ". ");
            current = Regex.Replace(current, "^[\\s\\.]+|[\\s\\.]+$", "");

            return current;
        }

        private void LoadOptions()
        {
            LoadRunways();

            ComboBoxAirport.Items.Clear();

            ComboBoxAirport.Items.Add("");

            foreach (var freq in Plugin.ATISData.Frequencies.OrderBy(x => x.Airport))
            {
                var airport = Airspace2.GetAirport(freq.Airport);

                if (airport == null)
                {
                    ComboBoxAirport.Items.Add(freq.Airport);
                }
                else
                {
                    ComboBoxAirport.Items.Add($"{freq.Airport} - {airport.FullName}");
                }
            }

            LoadCodes();

            LoadModes();

            ComboBoxVoice.Items.Clear();

            foreach (var voice in Control.SpeechSynth.GetInstalledVoices())
            {
                ComboBoxVoice.Items.Add(voice.VoiceInfo.Name);
            }

            ComboBoxVoice.Items.Add(Plugin.ManualVoiceName);

            ComboBoxZuluFrequency.Items.Clear();

            if (Plugin.Frequencies.Any())
            {
                foreach (var frequency in Plugin.Frequencies)
                {
                    ComboBoxZuluFrequency.Items.Add(string.IsNullOrWhiteSpace(frequency.FriendlyName) ? frequency.Name : frequency.FriendlyName);
                }

                ComboBoxZuluFrequency.SelectedIndex = 0;
            }

            foreach (var line in Control.Lines)
            {
                switch (line.Number)
                {
                    case 1:
                        Label1.Text = line.Name;
                        break;
                    case 2:
                        Label2.Text = line.Name;
                        break;
                    case 3:
                        Label3.Text = line.Name;
                        break;
                    case 4:
                        Label4.Text = line.Name;
                        break;
                    case 5:
                        Label5.Text = line.Name;
                        break;
                    case 6:
                        Label6.Text = line.Name;
                        break;
                    case 7:
                        Label7.Text = line.Name;
                        break;
                    case 8:
                        Label8.Text = line.Name;
                        break;
                    case 9:
                        Label9.Text = line.Name;
                        break;
                    case 10:
                        Label10.Text = line.Name;
                        break;
                    case 11:
                        Label11.Text = line.Name;
                        break;
                    case 12:
                        Label12.Text = line.Name;
                        break;
                    default:
                        break;
                }
            }
        }

        public void RefreshForm()
        {
            Invoke(new Action(() => {

                if (!Initialized) return;

                if (Plugin.ProfileName() == "New Zealand")
                {
                    ButtonZulu.Visible = false;
                }
                else
                {
                    ButtonZulu.Visible = true;
                }

                if (ICAO == null)
                {
                    ComboBoxAirport.SelectedIndex = ComboBoxAirport.Items.IndexOf("");
                }
                else
                {
                    var index = ComboBoxAirport.Items.IndexOf(DiplayName);

                    if (ComboBoxAirport.SelectedIndex != index) ComboBoxAirport.SelectedIndex = ComboBoxAirport.Items.IndexOf(DiplayName);
                }

                ComboBoxLetter.SelectedIndex = ComboBoxLetter.Items.IndexOf(ID.ToString());

                SetText(TextBoxMode, Mode);

                if (VoiceName != null)
                {
                    ComboBoxVoice.SelectedIndex = ComboBoxVoice.Items.IndexOf(VoiceName);
                }

                ComboBoxRate.SelectedIndex = ComboBoxRate.Items.IndexOf(Rate.ToString());

                ComboBoxTimeCheck.SelectedIndex = ComboBoxTimeCheck.Items.IndexOf(TimeCheck.ToString());

                if (ComboBoxZuluFrequency.Items == null) ComboBoxZuluFrequency.Items = new List<string>();

                ComboBoxZuluFrequency.Items.Clear();

                foreach (var frequency in Plugin.Frequencies)
                {
                    ComboBoxZuluFrequency.Items.Add(string.IsNullOrWhiteSpace(frequency.FriendlyName) ? frequency.Name : frequency.FriendlyName);
                }

                if (!string.IsNullOrWhiteSpace(ZuluFrequency))
                {
                    ComboBoxZuluFrequency.SelectedIndex = ComboBoxZuluFrequency.Items.IndexOf(ZuluFrequency);
                }

                RefreshForm_TopButtons();

                if (Network.IsConnected && Control?.ICAO == null)
                {
                    RefreshForm_ClearWindCalculator();
                    RefreshFrom_NoATIS();
                }
                else if (Network.IsConnected && Control?.ICAO != null)
                {
                    RefreshForm_WithATIS();
                }
                else
                {
                    RefreshForm_ClearWindCalculator();
                    RefreshForm_NotConnected();
                }

                RefreshForm_Presets();

                AutoSelectMode();

            }));
        }

        // Current weather for ranking runway modes: the METAR, plus SFC COND for a wet runway.
        private RunwaySelection.Conditions GetRunwayConditions()
        {
            var surfaceLine = Control.Lines.FirstOrDefault(x => x.Name == SurfaceCondition.Field);

            var surfaceBox = surfaceLine == null ? null : GetTextBox(surfaceLine.Number);

            var wet = surfaceBox != null && SurfaceCondition.Rank(surfaceBox.Text) > 0;

            return RunwaySelection.FromMetar(Control.METARRaw, wet);
        }

        // The profile's GroundUnknown colour (orange), as the window colours have no orange.
        private static System.Drawing.Color OutsideLimitsColour => Colours.GetColour(Colours.Identities.GroundUnknown);

        // vatSys text fields draw their own background and ignore BackColor, so the highlight is a frame drawn
        // behind the field, just larger than it.
        private const int HighlightWidth = 2;
        private readonly Dictionary<TextBox, Panel> FieldHighlights = new Dictionary<TextBox, Panel>();

        // Highlights the RWY field when its runways are outside the SOP wind limits (or a dry only mode on a wet runway),
        // and the APCH field when the weather is below that approach's minima. Only a warning: the ATIS can still be saved.
        private void RefreshForm_Limits()
        {
            var runwayLine = Control.Lines.FirstOrDefault(x => x.Name == "RWY");
            var approachLine = Control.Lines.FirstOrDefault(x => x.Name == "APCH");
            var windLine = Control.Lines.FirstOrDefault(x => x.METARField == METARField.Wind);

            var runwayBox = runwayLine == null ? null : GetTextBox(runwayLine.Number);
            var approachBox = approachLine == null ? null : GetTextBox(approachLine.Number);
            var windBox = windLine == null ? null : GetTextBox(windLine.Number);

            var active = Control.ICAO != null && !Control.IsZulu && Network.IsConnected;

            var conditions = GetRunwayConditions();

            // What's being published, rather than the METAR, for the wind.
            if (windBox != null) conditions.Wind = Wind.Parse(windBox.Text);

            var runwayOutside = false;

            if (active && runwayBox != null && !string.IsNullOrWhiteSpace(runwayBox.Text))
            {
                // The selected mode's runway list and dry only rule apply while RWY still matches it.
                var mode = Modes.FirstOrDefault(x => x.Name == Mode);
                var modeRunway = mode?.Fields.FirstOrDefault(x => x.Name == "RWY");
                var matchesMode = modeRunway != null && PresetAirport.Clean(modeRunway.Value) == PresetAirport.Clean(runwayBox.Text);

                var runwayCheck = new PresetMode
                {
                    Runways = matchesMode ? mode.Runways : null,
                    DryOnly = matchesMode && mode.DryOnly
                };

                runwayCheck.Fields.Add(new PresetModeField { Name = "RWY", Value = runwayBox.Text });

                // Without a runway number in RWY there's nothing to check.
                if (!string.IsNullOrWhiteSpace(runwayCheck.Runways) || Wind.RunwaysInUse(runwayBox.Text).Any())
                {
                    runwayOutside = !RunwaySelection.Assess(runwayCheck, conditions, RunwayHeading).Usable;
                }
            }

            var approachOutside = false;

            if (active && approachBox != null && !string.IsNullOrWhiteSpace(approachBox.Text))
            {
                // Approach minima come from the modes that use this approach wording.
                var approach = PresetAirport.Clean(approachBox.Text);

                var minima = Modes.FirstOrDefault(x => (x.MinCeiling > 0 || x.MinVisibility > 0)
                    && x.Fields.Any(f => f.Name == "APCH" && PresetAirport.Clean(f.Value) == approach));

                if (minima != null)
                {
                    approachOutside = conditions.Ceiling < minima.MinCeiling || conditions.Visibility < minima.MinVisibility;
                }
            }

            SetOutsideLimits(runwayLine, runwayOutside);
            SetOutsideLimits(approachLine, approachOutside);
        }

        private void SetOutsideLimits(ATISLine line, bool outside)
        {
            var textBox = line == null ? null : GetTextBox(line.Number);

            if (textBox == null) return;

            if (!FieldHighlights.TryGetValue(textBox, out var highlight))
            {
                highlight = new Panel { Visible = false };

                Controls.Add(highlight);

                FieldHighlights.Add(textBox, highlight);
            }

            highlight.Visible = outside && textBox.Visible;

            if (!highlight.Visible) return;

            highlight.BackColor = OutsideLimitsColour;

            // Follows the field, which moves with the layout.
            var bounds = textBox.Bounds;

            bounds.Inflate(HighlightWidth, HighlightWidth);

            if (highlight.Bounds != bounds) highlight.Bounds = bounds;

            highlight.SendToBack();
        }

        private double RunwayHeading(string runway) => GetRunwayHeading(runway) ?? int.Parse(runway.Substring(0, 2)) * 10;

        // Picks the best runway mode the first time the ATIS has a METAR, once only per ATIS.
        private void AutoSelectMode()
        {
            if (Control.ICAO == null || Control.IsZulu || Control.ModeAutoSelected) return;

            if (string.IsNullOrWhiteSpace(Control.METARRaw) || !Network.IsConnected) return;

            Control.ModeAutoSelected = true;

            if (Mode != null || Control.Mode != null || !Modes.Any()) return;

            var best = RunwaySelection.Best(Modes, GetRunwayConditions(), RunwayHeading);

            if (best != null) SelectMode(best.Name);
        }

        private void RefreshForm_Presets()
        {
            var visible = !Control.IsZulu;

            var enabled = visible && Network.IsConnected && Control.ICAO != null;

            TextBoxMode.Enabled = enabled;

            // Prompt the controller to pick a runway mode first.
            if (enabled && string.IsNullOrWhiteSpace(Mode))
            {
                LabelMode.ForeColor = Colours.GetColour(Colours.Identities.Warning);
            }
            else
            {
                LabelMode.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            }

            ButtonMode.Enabled = enabled;

            RefreshForm_PresetButtons(enabled);

            RefreshForm_Limits();

            RefreshForm_Layout();
        }

        private const int RowTop = 229;
        private const int RowHeight = 31;
        private int? WindowChrome { get; set; }

        // Stacks the rows that are in use (MODE only when the airport has modes, then one row per ATIS line)
        // and sizes the window to fit.
        private void RefreshForm_Layout()
        {
            SuspendLayout();

            var y = RowTop;

            var showMode = !Control.IsZulu && Modes.Any();

            LabelMode.Visible = showMode;
            TextBoxMode.Visible = showMode;
            ButtonMode.Visible = showMode;

            if (showMode)
            {
                LabelMode.Location = new System.Drawing.Point(8, y + 3);
                TextBoxMode.Location = new System.Drawing.Point(172, y);
                ButtonMode.Location = new System.Drawing.Point(172 + TextBoxWidth - PresetButtonWidth, y);
                ButtonMode.Size = new System.Drawing.Size(PresetButtonWidth, PresetButtonWidth);
                y += RowHeight;
            }

            var rows = Control.Lines.Count(x => x.Number <= 12 && x.Name != "ZULU");

            for (int number = 1; number <= 12; number++)
            {
                var textBox = GetTextBox(number);
                var label = GetLabel(number);
                var button = PresetButtons[number];

                var inUse = number <= rows && !Control.IsZulu;

                textBox.Visible = inUse;
                label.Visible = inUse;
                button.Visible = inUse && button.Tag != null;

                if (!inUse) continue;

                label.Location = new System.Drawing.Point(8, y + 3);
                textBox.Location = new System.Drawing.Point(172, y);
                button.Location = new System.Drawing.Point(172 + TextBoxWidth - PresetButtonWidth, y);
                button.Size = new System.Drawing.Size(PresetButtonWidth, PresetButtonWidth);

                y += RowHeight;
            }

            if (Control.IsZulu)
            {
                TextBoxZulu.Location = new System.Drawing.Point(172, y + 1);
                TextBoxZulu.Height = rows * RowHeight - 7;
                y += rows * RowHeight;
            }

            ComboBoxTimeCheck.Location = new System.Drawing.Point(172, y - 1);
            LabelTimeCheck.Location = new System.Drawing.Point(78, y + 3);
            ComboBoxZuluFrequency.Location = new System.Drawing.Point(172, y - 1);
            LabelFrequency.Location = new System.Drawing.Point(86, y + 3);

            y += RowHeight;

            ButtonSave.Location = new System.Drawing.Point(172, y - 1);
            ButtonCancel.Location = new System.Drawing.Point(423, y - 1);

            y += 50;

            ComboBoxRunway.Location = new System.Drawing.Point(172, y - 1);
            LabelWindCalculator.Location = new System.Drawing.Point(6, y + 2);
            LabelWindComponents.Location = new System.Drawing.Point(338, y + 2);

            var clientHeight = y - 1 + 41;

            ResumeLayout();

            if (WindowChrome == null)
            {
                var chrome = Height - ClientSize.Height;
                WindowChrome = chrome > 0 ? chrome : 28;
            }

            var height = clientHeight + WindowChrome.Value;

            if (Height == height) return;

            MinimumSize = System.Drawing.Size.Empty;
            MaximumSize = System.Drawing.Size.Empty;
            Height = height;
            MinimumSize = new System.Drawing.Size(Width, height);
            MaximumSize = new System.Drawing.Size(Width, height);
        }

        private Label GetLabel(int number)
        {
            switch (number)
            {
                case 1: return Label1;
                case 2: return Label2;
                case 3: return Label3;
                case 4: return Label4;
                case 5: return Label5;
                case 6: return Label6;
                case 7: return Label7;
                case 8: return Label8;
                case 9: return Label9;
                case 10: return Label10;
                case 11: return Label11;
                case 12: return Label12;
                default: return null;
            }
        }

        private void RefreshForm_TopButtons()
        {
            if (Plugin.ATIS1.ICAO != null)
            {
                ButtonATIS1.Text = Plugin.ATIS1.ICAO;
            }
            else
            {
                ButtonATIS1.Text = "ATIS #1";
            }

            if (Plugin.ATIS2.ICAO != null)
            {
                ButtonATIS2.Text = Plugin.ATIS2.ICAO;
            }
            else
            {
                ButtonATIS2.Text = "ATIS #2";
            }

            if (Plugin.ATIS3.ICAO != null)
            {
                ButtonATIS3.Text = Plugin.ATIS3.ICAO;
            }
            else
            {
                ButtonATIS3.Text = "ATIS #3";
            }

            if (Plugin.ATIS4.ICAO != null)
            {
                ButtonATIS4.Text = Plugin.ATIS4.ICAO;
            }
            else
            {
                ButtonATIS4.Text = "ATIS #4";
            }

            if (Number != 1 && Plugin.ATIS1.HasUpdates)
            {
                ButtonATIS1.ForeColor = Colours.GetColour(Colours.Identities.Warning);
            }
            else
            {
                ButtonATIS1.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            }

            if (Number != 2 && Plugin.ATIS2.HasUpdates)
            {
                ButtonATIS2.ForeColor = Colours.GetColour(Colours.Identities.Warning);
            }
            else
            {
                ButtonATIS2.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            }

            if (Number != 3 && Plugin.ATIS3.HasUpdates)
            {
                ButtonATIS3.ForeColor = Colours.GetColour(Colours.Identities.Warning);
            }
            else
            {
                ButtonATIS3.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            }

            if (Number != 4 && Plugin.ATIS4.HasUpdates)
            {
                ButtonATIS4.ForeColor = Colours.GetColour(Colours.Identities.Warning);
            }
            else
            {
                ButtonATIS4.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            }

            if (Control.Number == 1)
            {
                ButtonATIS1.Checked = true;
            }
            else
            {
                ButtonATIS1.Checked = false;
            }

            if (Control.Number == 2)
            {
                ButtonATIS2.Checked = true;
            }
            else
            {
                ButtonATIS2.Checked = false;
            }

            if (Control.Number == 3)
            {
                ButtonATIS3.Checked = true;
            }
            else
            {
                ButtonATIS3.Checked = false;
            }

            if (Control.Number == 4)
            {
                ButtonATIS4.Checked = true;
            }
            else
            {
                ButtonATIS4.Checked = false;
            }
        }

        private void RefreshForm_NotConnected()
        {
            ComboBoxAirport.Enabled = false;
            ButtonCreate.Enabled = false;
            ComboBoxRate.Visible = true;
            ButtonRecord.Visible = false;

            RefreshForm_NormalATIS();

            ButtonZulu.Enabled = false;
            ComboBoxAirport.Enabled = true;
            ButtonCreate.Visible = true;
            ButtonDelete.Visible = false;
            ButtonGetMetar.Enabled = false;
            ComboBoxVoice.Enabled = false;
            ComboBoxRate.Enabled = false;
            ComboBoxLetter.Enabled = false;
            ButtonNext.Enabled = false;
            ComboBoxTimeCheck.Enabled = false;

            TextBox1.Text = string.Empty;
            TextBox2.Text = string.Empty;
            TextBox3.Text = string.Empty;
            TextBox4.Text = string.Empty;
            TextBox5.Text = string.Empty;
            TextBox6.Text = string.Empty;
            TextBox7.Text = string.Empty;
            TextBox8.Text = string.Empty;
            TextBox9.Text = string.Empty;
            TextBox10.Text = string.Empty;
            TextBox11.Text = string.Empty;
            TextBox12.Text = string.Empty;
            TextBoxZulu.Text = string.Empty;

            LabelMETAR.Text = string.Empty;

            ButtonSave.Enabled = false;
            ButtonCancel.Enabled = false;

            ButtonListen.Enabled = false;
            ButtonBroadcast.Enabled = false;

            RefreshForm_DisableTextBoxes();

            RefreshForm_ResetColours();
        }

        private void RefreshFrom_NoATIS()
        {
            ComboBoxAirport.Enabled = true;
            ButtonCreate.Enabled = true;

            if (Control.IsZulu)
            {
                RefreshForm_ZuluATIS();
            }
            else
            {
                RefreshForm_NormalATIS();
            }

            ButtonZulu.Enabled = false;
            ComboBoxAirport.Enabled = true;
            ButtonCreate.Visible = true;
            ButtonDelete.Visible = false;
            ButtonGetMetar.Enabled = false;
            ComboBoxVoice.Enabled = false;
            ComboBoxRate.Enabled = false;
            ComboBoxLetter.Enabled = false;
            ButtonNext.Enabled = false;
            ComboBoxTimeCheck.Enabled = false;

            TextBox1.Text = string.Empty;
            TextBox2.Text = string.Empty;
            TextBox3.Text = string.Empty;
            TextBox4.Text = string.Empty;
            TextBox5.Text = string.Empty;
            TextBox6.Text = string.Empty;
            TextBox7.Text = string.Empty;
            TextBox8.Text = string.Empty;
            TextBox9.Text = string.Empty;
            TextBox10.Text = string.Empty;
            TextBox11.Text = string.Empty;
            TextBox12.Text = string.Empty;
            TextBoxZulu.Text = string.Empty;

            LabelMETAR.Text = string.Empty;

            ButtonSave.Enabled = false;
            ButtonCancel.Enabled = false;

            ButtonListen.Enabled = false;
            ButtonBroadcast.Enabled = false;

            RefreshForm_DisableTextBoxes();

            RefreshForm_ResetColours();
        }

        private void RefreshForm_EnableTextBoxes()
        {
            TextBox1.Enabled = true;
            TextBox2.Enabled = true;
            TextBox3.Enabled = true;
            TextBox4.Enabled = true;
            TextBox5.Enabled = true;
            TextBox6.Enabled = true;
            TextBox7.Enabled = true;
            TextBox8.Enabled = true;
            TextBox9.Enabled = true;
            TextBox10.Enabled = true;
            TextBox11.Enabled = true;
            TextBox12.Enabled = true;
            TextBoxZulu.Enabled = true;

            TextBox1.TextChanged -= TextBox_TextChanged;
            TextBox1.TextChanged += TextBox_TextChanged;
            TextBox2.TextChanged -= TextBox_TextChanged;
            TextBox2.TextChanged += TextBox_TextChanged;
            TextBox3.TextChanged -= TextBox_TextChanged;
            TextBox3.TextChanged += TextBox_TextChanged;
            TextBox4.TextChanged -= TextBox_TextChanged;
            TextBox4.TextChanged += TextBox_TextChanged;
            TextBox5.TextChanged -= TextBox_TextChanged;
            TextBox5.TextChanged += TextBox_TextChanged;
            TextBox6.TextChanged -= TextBox_TextChanged;
            TextBox6.TextChanged += TextBox_TextChanged;
            TextBox7.TextChanged -= TextBox_TextChanged;
            TextBox7.TextChanged += TextBox_TextChanged;
            TextBox8.TextChanged -= TextBox_TextChanged;
            TextBox8.TextChanged += TextBox_TextChanged;
            TextBox9.TextChanged -= TextBox_TextChanged;
            TextBox9.TextChanged += TextBox_TextChanged;
            TextBox10.TextChanged -= TextBox_TextChanged;
            TextBox10.TextChanged += TextBox_TextChanged;
            TextBox11.TextChanged -= TextBox_TextChanged;
            TextBox11.TextChanged += TextBox_TextChanged;
            TextBox12.TextChanged -= TextBox_TextChanged;
            TextBox12.TextChanged += TextBox_TextChanged;
            TextBoxZulu.TextChanged -= TextBox_TextChanged;
            TextBoxZulu.TextChanged += TextBox_TextChanged;
        }

        // Only touch the text when it differs, so the caret and selection aren't reset mid-edit.
        private static void SetText(TextBox textBox, string value)
        {
            if ((textBox.Text ?? string.Empty) == (value ?? string.Empty)) return;

            textBox.Text = value;
        }

        private void RefreshForm_UnhookTextBoxes()
        {
            for (int number = 1; number <= 12; number++)
            {
                GetTextBox(number).TextChanged -= TextBox_TextChanged;
            }

            TextBoxZulu.TextChanged -= TextBox_TextChanged;
        }

        private void RefreshForm_DisableTextBoxes()
        {
            TextBox1.Enabled = false;
            TextBox2.Enabled = false;
            TextBox3.Enabled = false;
            TextBox4.Enabled = false;
            TextBox5.Enabled = false;
            TextBox6.Enabled = false;
            TextBox7.Enabled = false;
            TextBox8.Enabled = false;
            TextBox9.Enabled = false;
            TextBox10.Enabled = false;
            TextBox11.Enabled = false;
            TextBox12.Enabled = false;
            TextBoxZulu.Enabled = false;

            TextBox1.TextChanged -= TextBox_TextChanged;
            TextBox2.TextChanged -= TextBox_TextChanged;
            TextBox3.TextChanged -= TextBox_TextChanged;
            TextBox4.TextChanged -= TextBox_TextChanged;
            TextBox5.TextChanged -= TextBox_TextChanged;
            TextBox6.TextChanged -= TextBox_TextChanged;
            TextBox7.TextChanged -= TextBox_TextChanged;
            TextBox8.TextChanged -= TextBox_TextChanged;
            TextBox9.TextChanged -= TextBox_TextChanged;
            TextBox10.TextChanged -= TextBox_TextChanged;
            TextBox11.TextChanged -= TextBox_TextChanged;
            TextBox12.TextChanged -= TextBox_TextChanged;
            TextBoxZulu.TextChanged -= TextBox_TextChanged;
        }

        private int AppliedSuggestionsVersion { get; set; } = -1;
        private string AppliedMetar { get; set; }

        private void RefreshForm_WithATIS()
        {
            if (Control.IsZulu)
            {
                RefreshForm_ZuluATIS();
            }
            else
            {
                RefreshForm_NormalATIS();
            }

            ButtonZulu.Enabled = true;
            ComboBoxAirport.Enabled = false;
            ButtonCreate.Visible = false;
            ButtonDelete.Visible = true;
            ButtonGetMetar.Enabled = true;
            ComboBoxVoice.Enabled = true;
            ComboBoxRate.Enabled = true;
            ComboBoxTimeCheck.Enabled = true;

            RefreshForm_UnhookTextBoxes();

            // A newer METAR replaces pending values in the METAR fields (including ones from an earlier METAR that
            // wasn't saved), and new suggestions replace pending values in the fields they cover. Edits made after
            // that stay until the next METAR or suggestion.
            var newMetar = Control.METARRaw != AppliedMetar;

            if (newMetar || Control.SuggestionsVersion != AppliedSuggestionsVersion)
            {
                var suggested = Control.SuggestedLines.ToList();

                foreach (var line in Control.Lines)
                {
                    if ((newMetar && line.METARField != METARField.None) || suggested.Any(x => x.Name == line.Name))
                    {
                        Saves.Remove(line.Name);
                    }
                }

                AppliedMetar = Control.METARRaw;
                AppliedSuggestionsVersion = Control.SuggestionsVersion;
            }

            foreach (var line in Control.Lines)
            {
                var textBox = line.Number == 13 ? TextBoxZulu : GetTextBox(line.Number);

                if (textBox == null) continue;

                var saveLine = Saves.FirstOrDefault(x => x.Key == line.Name);

                SetText(textBox, saveLine.Key != null ? saveLine.Value : line.Value);
            }

            RefreshForm_EnableTextBoxes();

            LabelMETAR.Text = Control.METARRaw;

            if (!Control.IsZulu && Control.HasUpdates && ID != Control.ID)
            {
                LabelATIS.ForeColor = Colours.GetColour(Colours.Identities.Warning);
            }
            else
            {
                LabelATIS.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            }

            foreach (var line in Control.Lines)
            {
                var suggestedLine = Control.SuggestedLines.FirstOrDefault(x => x.Name == line.Name);

                var saveLine = Saves.FirstOrDefault(x => x.Key == line.Name);

                switch (line.Number)
                {
                    case 1:
                        if (saveLine.Key != null) SetText(TextBox1, saveLine.Value);
                        else if (suggestedLine != null)
                        {
                            SetText(TextBox1, suggestedLine.Value);
                            Label1.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label1.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText); 
                        }
                        break;
                    case 2:
                        if (saveLine.Key != null) SetText(TextBox2, saveLine.Value);
                        else if (suggestedLine != null)
                        {
                            SetText(TextBox2, suggestedLine.Value);
                            Label2.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label2.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 3:
                        if (saveLine.Key != null) SetText(TextBox3, saveLine.Value);
                        else if (suggestedLine != null)
                        {
                            SetText(TextBox3, suggestedLine.Value);
                            Label3.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label3.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 4:
                        if (saveLine.Key != null) SetText(TextBox4, saveLine.Value);
                        else if (suggestedLine != null)
                        {
                            SetText(TextBox4, suggestedLine.Value);
                            Label4.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label4.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 5:
                        if (saveLine.Key != null) SetText(TextBox5, saveLine.Value);
                        else if (suggestedLine != null)
                        {
                            SetText(TextBox5, suggestedLine.Value);
                            Label5.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label5.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 6:
                        if (saveLine.Key != null) SetText(TextBox6, saveLine.Value);
                        else if (suggestedLine != null)
                        {
                            SetText(TextBox6, suggestedLine.Value);
                            Label6.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label6.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 7:
                        if (saveLine.Key != null) SetText(TextBox7, saveLine.Value);
                        else if (suggestedLine != null)
                        {
                            SetText(TextBox7, suggestedLine.Value);
                            Label7.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label7.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 8:
                        if (saveLine.Key != null) SetText(TextBox8, saveLine.Value);
                        else if (suggestedLine != null)
                        {
                            SetText(TextBox8, suggestedLine.Value);
                            Label8.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label8.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 9:
                        if (saveLine.Key != null) SetText(TextBox9, saveLine.Value);
                        else if (suggestedLine != null)
                        {
                            SetText(TextBox9, suggestedLine.Value);
                            Label9.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label9.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 10:
                        if (saveLine.Key != null) SetText(TextBox10, saveLine.Value);
                        else if (suggestedLine != null)
                        {
                            SetText(TextBox10, suggestedLine.Value);
                            Label10.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label10.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 11:
                        if (saveLine.Key != null) SetText(TextBox11, saveLine.Value);
                        else if (suggestedLine != null)
                        {
                            SetText(TextBox11, suggestedLine.Value);
                            Label11.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label11.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 12:
                        if (saveLine.Key != null) SetText(TextBox12, saveLine.Value);
                        else if (suggestedLine != null)
                        {
                            SetText(TextBox12, suggestedLine.Value);
                            Label12.ForeColor = Colours.GetColour(Colours.Identities.Warning);
                        }
                        else
                        {
                            Label12.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                        }
                        break;
                    case 13:
                        if (saveLine.Key != null) SetText(TextBoxZulu, saveLine.Value);
                        break;
                    default:
                        break;
                }
            }

            if (Edits)
            {
                ButtonSave.Enabled = true;
                ButtonCancel.Enabled = true;
            }
            else
            {
                ButtonSave.Enabled = false;
                ButtonCancel.Enabled = false;
            }

            if (Control.CanListen)
            {
                ButtonListen.Enabled = true;
                ButtonBroadcast.Enabled = true;
            }
            else
            {
                ButtonListen.Enabled = false;
                ButtonBroadcast.Enabled = false;
            }

            if (CanRecord)
            {
                ButtonRecord.Enabled = true;
            }
            else
            {
                ButtonRecord.Enabled = false;
            }

            if (Control.Recording)
            {
                ButtonRecord.Checked = true;
            }
            else
            {
                ButtonRecord.Checked = false;
            }

            if (Control.Listening)
            {
                ButtonListen.Checked = true;
            }
            else
            {
                ButtonListen.Checked = false;
            }

            if (Control.Broadcasting)
            {
                ButtonBroadcast.Checked = true;
            }
            else
            {
                ButtonBroadcast.Checked = false;
            }
        }

        private void RefreshForm_NormalATIS()
        {
            TextBoxZulu.Visible = false;
            ComboBoxZuluFrequency.Visible = false;
            LabelFrequency.Visible = false;
            ComboBoxLetter.Enabled = true;
            ButtonNext.Enabled = true;
            ButtonZulu.Checked = false;

            TextBox1.Visible = true;
            TextBox2.Visible = true;
            TextBox3.Visible = true;
            TextBox4.Visible = true;
            TextBox5.Visible = true;
            TextBox6.Visible = true;
            TextBox7.Visible = true;
            TextBox8.Visible = true;
            TextBox9.Visible = true;
            TextBox10.Visible = true;
            TextBox11.Visible = true;
            TextBox12.Visible = true;
            Label1.Visible = true;
            Label2.Visible = true;
            Label3.Visible = true;
            Label4.Visible = true;
            Label5.Visible = true;
            Label6.Visible = true;
            Label7.Visible = true;
            Label8.Visible = true;
            Label9.Visible = true;
            Label10.Visible = true;
            Label11.Visible = true;
            Label12.Visible = true;
            LabelTimeCheck.Visible = true;
            ComboBoxTimeCheck.Visible = true;
        }

        private void RefreshForm_ZuluATIS()
        {
            TextBoxZulu.Visible = true;
            ComboBoxZuluFrequency.Visible = true;
            LabelFrequency.Visible = true;
            ComboBoxLetter.Enabled = false;
            ButtonNext.Enabled = false;
            ButtonZulu.Checked = true;

            TextBox1.Visible = false;
            TextBox2.Visible = false;
            TextBox3.Visible = false;
            TextBox4.Visible = false;
            TextBox5.Visible = false;
            TextBox6.Visible = false;
            TextBox7.Visible = false;
            TextBox8.Visible = false;
            TextBox9.Visible = false;
            TextBox10.Visible = false;
            TextBox11.Visible = false;
            TextBox12.Visible = false;
            Label1.Visible = false;
            Label2.Visible = false;
            Label3.Visible = false;
            Label4.Visible = false;
            Label5.Visible = false;
            Label6.Visible = false;
            Label7.Visible = false;
            Label8.Visible = false;
            Label9.Visible = false;
            Label10.Visible = false;
            Label11.Visible = false;
            Label12.Visible = false;
            LabelTimeCheck.Visible = false;
            ComboBoxTimeCheck.Visible = false;
        }

        private void RefreshForm_ClearWindCalculator()
        {
            Invoke(new Action(() => {
                LabelWindComponents.Text = "";
                ComboBoxRunway.SelectedIndex = ComboBoxRunway.Items.IndexOf("");
            }));
        }

        private void RefreshForm_ResetColours()
        {
            Label1.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText); 
            Label2.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label3.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label4.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label5.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label6.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label7.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label8.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label9.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label10.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label11.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Label12.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            LabelATIS.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
        }

        private void GetMetar()
        {
            LabelMETAR.Text = "LOADING";
            
            MET.Instance.RequestProduct(new MET.ProductRequest(MET.ProductType.VATSIM_METAR, ICAO, true));
        }

        private void IncreaseID()
        {
            if (Control.IsZulu) return;

            // Get the current index of the ID in the valid codes list
            int currentIndex = ComboBoxLetter.Items.IndexOf(ID.ToString());
            
            if (currentIndex == -1) 
            {
                // If current ID is not in the valid list, select the first one
                if (ComboBoxLetter.Items.Count > 0)
                {
                    ComboBoxLetter.SelectedIndex = 0;
                    ID = ComboBoxLetter.Items[0].ToString()[0];
                }
                return;
            }
            
            // Calculate the next index, wrapping around if needed
            int nextIndex = (currentIndex + 1) % ComboBoxLetter.Items.Count;
            
            // Update the ID and selection
            ComboBoxLetter.SelectedIndex = nextIndex;
            ID = ComboBoxLetter.Items[nextIndex].ToString()[0];
        }

        private async void ButtonCreate_Click(object sender, EventArgs e)
        {
            if (!Network.IsConnected || !Network.IsValidATC) return;

            if (ICAO == null) return;

            var frequency = Plugin.ATISData.Frequencies.FirstOrDefault(x => x.Airport == ICAO);

            if (frequency == null) return;

            if (Airport == null) return;

            await Control.Create(ICAO, frequency.Frequency.ToString(), Airport.LatLong);

            LoadRunways();

            GetMetar();
        }

        private void ComboBoxAirport_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboBoxAirport.SelectedIndex == -1) return;

            if (ComboBoxAirport.SelectedIndex == 0) return;

            var icao = ComboBoxAirport.Items[ComboBoxAirport.SelectedIndex];

            if (icao.Length == 4)
            {
                ICAO = icao;

                LoadCodes();

                LoadModes();

                return;
            }

            ICAO = ComboBoxAirport.Items[ComboBoxAirport.SelectedIndex].Substring(0, 4);

            LoadCodes();

            LoadModes();
        }

        private async void ButtonDelete_Click(object sender, EventArgs e)
        {
            await Control.Delete();

            Change(Number);
        }

        private void ButtonGetMetar_Click(object sender, EventArgs e)
        {
            GetMetar();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            ID = Control.ID;

            Mode = Control.Mode;

            TimeCheck = Control.TimeCheck;

            Saves.Clear();

            Control.SuggestedLines.Clear();

            RefreshForm_ResetColours();

            RefreshForm();
        }

        private void ButtonNext_Click(object sender, EventArgs e)
        {
            IncreaseID();
        }

        private async void ButtonSave_Click(object sender, EventArgs e)
        {
            RefreshForm_ClearWindCalculator();

            await SaveATIS();
        }

        private async Task SaveATIS()
        {
            Control.VoiceName = VoiceName;

            Control.PromptRate = Rate;

            Control.Mode = Mode;

            await Control.Save(ID, Saves, TimeCheck);

            Saves.Clear();

            RefreshForm_ResetColours();

            RefreshForm();
        }

        private async void ButtonBroadcast_Click(object sender, EventArgs e)
        {
            if (!Control.Broadcasting)
            {
                if (Saves.Any()) await SaveATIS();

                Control.BroadcastStart();
            }
            else
            {
                await Control.BroadcastStop();
            }

            RefreshForm();
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            if (!(sender is TextBox textBox)) return;

            var lineNumber = 0;

            switch (textBox.Name)
            {
                case "TextBox1":
                    lineNumber = 1;
                    break;
                case "TextBox2":
                    lineNumber = 2;
                    break;
                case "TextBox3":
                    lineNumber = 3;
                    break;
                case "TextBox4":
                    lineNumber = 4;
                    break;
                case "TextBox5":
                    lineNumber = 5;
                    break;
                case "TextBox6":
                    lineNumber = 6;
                    break;
                case "TextBox7":
                    lineNumber = 7;
                    break;
                case "TextBox8":
                    lineNumber = 8;
                    break;
                case "TextBox9":
                    lineNumber = 9;
                    break;
                case "TextBox10":
                    lineNumber = 10;
                    break;
                case "TextBox11":
                    lineNumber = 11;
                    break;
                case "TextBox12":
                    lineNumber = 12;
                    break;
                case "TextBoxZulu":
                    lineNumber = 13;
                    break;
                default:
                    break;
            }

            if (lineNumber == 0) return;

            var line = Control.Lines.FirstOrDefault(x => x.Number == lineNumber);

            if (line == null) return;

            if (line.METARField == METARField.Weather)
            {
                // May set SFC COND, which comes back through here and updates the wind.
                UpdateSurfaceCondition();
            }

            if (line.METARField == METARField.Wind || line.Name == "RWY" || line.Name == SurfaceCondition.Field)
            {
                // May rewrite the WIND field, which comes back through here to record it.
                UpdateWindComponents();
            }

            if (line.METARField == METARField.Wind || line.Name == "RWY" || line.Name == "APCH" || line.Name == SurfaceCondition.Field)
            {
                RefreshForm_Limits();
            }

            var save = Saves.FirstOrDefault(x => x.Key == line.Name);

            if (save.Key != null)
            {
                Saves.Remove(save.Key);
            }

            // Edited back to the saved value, so there's nothing pending for this line.
            if ((line.Value ?? string.Empty) == textBox.Text) return;

            Saves.Add(line.Name, textBox.Text);

            if (Edits && ID == Control.ID && !Control.IsZulu)
            {
                IncreaseID();
                ButtonSave.Enabled = true;
                ButtonCancel.Enabled = true;
            }
            else if (Edits && Control.IsZulu)
            {
                ButtonSave.Enabled = true;
                ButtonCancel.Enabled = true;
            }

            if (line.Name.Contains("WIND"))
            {
                CalculateWind();
            }
        }

        private void ComboBoxLetter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(sender is DropDownBox comboBox)) return;

            if (comboBox.SelectedIndex == -1) return;

            string selectedLetter = comboBox.Items[comboBox.SelectedIndex];

            var ok = Char.TryParse(selectedLetter, out Char selectedChar);

            if (!ok) return;

            if (ID == selectedChar) return;

            ID = selectedChar;

            ButtonSave.Enabled = true;
            ButtonCancel.Enabled = true;
        }

        private void SelectMode(string selectedMode)
        {
            if (selectedMode == Mode) return;

            Mode = selectedMode;

            SetText(TextBoxMode, Mode);

            var mode = Modes.FirstOrDefault(x => x.Name == Mode);

            if (mode != null)
            {
                foreach (var field in mode.Fields)
                {
                    var line = Control.Lines.FirstOrDefault(x => x.Name == field.Name);

                    if (line == null) continue;

                    var textBox = GetTextBox(line.Number);

                    if (textBox == null) continue;

                    textBox.Text = PresetAirport.Clean(field.Value);
                }
            }

            ButtonSave.Enabled = true;
            ButtonCancel.Enabled = true;

            RefreshForm_Presets();
        }

        private void ComboBoxVoice_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(sender is DropDownBox comboBox)) return;

            if (comboBox.SelectedIndex == -1) return;

            string selectedVoice = comboBox.Items[comboBox.SelectedIndex];

            if (selectedVoice == null || selectedVoice == VoiceName) return;  

            VoiceName =  selectedVoice;

            ButtonSave.Enabled = true;
            ButtonCancel.Enabled = true;

            if (VoiceName == Plugin.ManualVoiceName)
            {
                ComboBoxRate.Visible = false;
                ButtonRecord.Visible = true;
            }
            else
            {
                ComboBoxRate.Visible = true;
                ButtonRecord.Visible = false;
            }
        }

        private void ComboBoxRate_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(sender is DropDownBox comboBox)) return;

            if (comboBox.SelectedIndex == -1) return;

            string rateString = comboBox.Items[comboBox.SelectedIndex];

            var rate = PromptRate.Medium;

            switch (rateString)
            {
                case "Extra Fast":
                    rate = PromptRate.ExtraFast;
                    break;
                case "Fast":
                    rate = PromptRate.Fast;
                    break;
                case "Medium":
                    rate = PromptRate.Medium;
                    break;
                case "Slow":
                    rate = PromptRate.Slow;
                    break;
                case "Extra Slow":
                    rate = PromptRate.ExtraSlow;
                    break;
                default:
                    break;
            }

            if (rate == Rate) return;

            Rate = rate;

            ButtonSave.Enabled = true;
            ButtonCancel.Enabled = true;
        }

        private void ButtonListen_Click(object sender, EventArgs e)
        {
            if (Control.Listening)
            {
                Control.ListenStop();
            }
            else
            {
                if (!Control.CanListen) return;
                Control.ListenStart();
            }

            RefreshForm();
        }

        private void ButtonATIS1_Click(object sender, EventArgs e)
        {
            Control.SoundPlayer.Stop();
            Change(1);
        }

        private void ButtonATIS2_Click(object sender, EventArgs e)
        {
            Control.SoundPlayer.Stop();
            Change(2);
        }

        private void ButtonATIS3_Click(object sender, EventArgs e)
        {
            Control.SoundPlayer.Stop();
            Change(3);
        }

        private void ButtonATIS4_Click(object sender, EventArgs e)
        {
            Control.SoundPlayer.Stop();
            Change(4);
        }

        private void ComboBoxTimeCheck_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(sender is DropDownBox comboBox)) return;

            if (comboBox.SelectedIndex == -1) return;

            var timecheckOK = bool.TryParse(comboBox.Items[comboBox.SelectedIndex], out bool timecheck);

            if (!timecheckOK) return;

            if (TimeCheck == timecheck) return;

            TimeCheck = timecheck;

            ButtonSave.Enabled = true;
            ButtonCancel.Enabled = true;
        }

        private void ButtonZulu_Click(object sender, EventArgs e)
        {
            ID = 'Z';

            Saves.Clear();

            if (!Control.IsZulu)
            {
                Control.IsZulu = true;

                GenerateZulu();
            }
            else
            {
                Control.IsZulu = false;

                TextBoxZulu.Text = string.Empty;

                ZuluFrequency = string.Empty;
            }

            RefreshForm();
        }

        private void ComboBoxZuluFrequency_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ComboBoxZuluFrequency.SelectedIndex == -1) return;

            var zuluFrequency = ComboBoxZuluFrequency.Items[ComboBoxZuluFrequency.SelectedIndex];

            if (zuluFrequency == ZuluFrequency) return;

            ZuluFrequency = zuluFrequency;

            GenerateZulu();
        }

        private void GenerateZulu()
        {
            var zuluInfo = Plugin.ZuluInfo.FirstOrDefault(x => x.ICAO == ICAO);

            var zuluLine = Control.Lines.FirstOrDefault(x => x.Name == "ZULU");

            if (zuluInfo == null || zuluLine == null) return;

            var atis = zuluInfo.Text;

            var frequency = Plugin.Frequencies.FirstOrDefault(x => x.Name == ZuluFrequency || x.FriendlyName == ZuluFrequency);

            if (frequency != null)
            {
                var station = string.IsNullOrWhiteSpace(frequency.FriendlyName) ? frequency.Name : frequency.FriendlyName;
                
                atis = atis.Replace("{STATION}", station.ToUpper());
                
                atis = atis.Replace("{FREQ}", Conversions.FrequencyToString(frequency.Frequency));
            }

            zuluLine.Value = atis;

            TextBoxZulu.Text = atis;
        }

        private string WindOutput(string wind, double rwyHeading)
        {
            var baseWind = Wind.StripComponents(wind);

            if (string.IsNullOrWhiteSpace(baseWind)) return string.Empty;

            if (baseWind == "CALM") return "CALM";

            if (baseWind.StartsWith("VRB")) return "VARIABLE";

            var parsed = Wind.Parse(baseWind);

            if (parsed == null) return string.Empty;

            if (parsed.MaxSpeed == 0) return "CALM";

            // Worst case across any direction range, at gust strength.
            var components = parsed.Worst(rwyHeading);

            var crosswind = Math.Round(Math.Abs(components.Crosswind));

            var tailwind = Math.Round(components.Tailwind);

            var headwind = Math.Round(components.Headwind);

            var along = tailwind > 0 ? $"{tailwind:F0}kt Tailwind" : headwind > 0 ? $"{headwind:F0}kt Headwind" : string.Empty;

            var across = crosswind > 0 ? $"{crosswind:F0}kt {(components.Crosswind > 0 ? "Right" : "Left")} Crosswind" : string.Empty;

            return string.Join(", ", new[] { along, across }.Where(x => !string.IsNullOrEmpty(x)));
        }

        private void CalculateWind()
        {
            var saveWind = Saves.FirstOrDefault(x => x.Key.Contains("WIND"));

            var atisWind = Control.Lines.FirstOrDefault(x => x.Name.Contains("WIND"));

            var wind = saveWind.Key != null ? saveWind.Value : atisWind?.Value;

            if (wind == null) return;

            var runwayName = ComboBoxRunway.Text.Replace("RWY ", "");

            var runwayHeading = GetRunwayHeading(runwayName);

            if (runwayHeading == null)
            {
                var runwayNameHeadingOk = double.TryParse(runwayName, out double runwayNameHeading);

                if (!runwayNameHeadingOk) return;

                runwayHeading = runwayNameHeading;
            }

            var windComponents = WindOutput(wind, runwayHeading.Value);

            LabelWindComponents.Text = windComponents;
        }

        private bool UpdatingWindComponents { get; set; }

        // Runways in the RWY field, using the selected mode's runways when it lists them and RWY still matches it
        // (e.g. "10L/28R ..." names both ends, but the 28 PROPS mode only uses 28L and 28R).
        private List<string> GetRunwaysForWind(string runwayText)
        {
            var mode = Modes.FirstOrDefault(x => x.Name == Mode);

            var modeRunway = mode?.Fields.FirstOrDefault(x => x.Name == "RWY");

            if (mode != null && !string.IsNullOrWhiteSpace(mode.Runways) && modeRunway != null
                && PresetAirport.Clean(modeRunway.Value) == PresetAirport.Clean(runwayText))
            {
                return mode.Runways.Split('|').First().Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return Wind.RunwaysInUse(runwayText);
        }

        // Appends the worst case crosswind (from the airport's reporting threshold) and tailwind (when SFC COND
        // isn't empty or DRY) for the runways named in the RWY field to the WIND field.
        private void UpdateWindComponents()
        {
            if (UpdatingWindComponents || Control.IsZulu) return;

            var windLine = Control.Lines.FirstOrDefault(x => x.METARField == METARField.Wind);
            var runwayLine = Control.Lines.FirstOrDefault(x => x.Name == "RWY");
            var surfaceLine = Control.Lines.FirstOrDefault(x => x.Name == SurfaceCondition.Field);

            var windBox = windLine == null ? null : GetTextBox(windLine.Number);
            var runwayBox = runwayLine == null ? null : GetTextBox(runwayLine.Number);
            var surfaceBox = surfaceLine == null ? null : GetTextBox(surfaceLine.Number);

            if (windBox == null || runwayBox == null) return;

            var baseWind = Wind.StripComponents(windBox.Text);

            var parsed = Wind.Parse(baseWind);

            var output = baseWind;

            if (parsed != null)
            {
                // Parallel runways (34L and 34R) share a direction, so only one entry per runway number.
                var directions = GetRunwaysForWind(runwayBox.Text).GroupBy(Wind.Direction).ToList();

                var reportCrosswindFrom = PresetAirport?.ReportCrosswindFrom ?? PresetAirport.DefaultReportCrosswindFrom;

                var wet = surfaceBox != null && SurfaceCondition.Rank(surfaceBox.Text) > 0;

                var crosswinds = new List<string>();
                var tailwinds = new List<string>();

                foreach (var direction in directions)
                {
                    var heading = GetRunwayHeading(direction.First()) ?? int.Parse(direction.Key) * 10;

                    var components = parsed.Worst(heading);

                    var crosswind = Math.Round(Math.Abs(components.Crosswind));
                    var tailwind = Math.Round(components.Tailwind);

                    // Only name the runway when there's more than one direction in use: the runway itself
                    // (e.g. 16L in SODPROPS), or just the number when parallels are used together.
                    var name = direction.Count() == 1 ? direction.First() : direction.Key;

                    var runway = directions.Count > 1 ? $" RWY {name}" : string.Empty;

                    if (crosswind >= reportCrosswindFrom) crosswinds.Add($", MX XW {crosswind:F0}KTS{runway}");

                    if (wet && tailwind > 0) tailwinds.Add($", MX TW {tailwind:F0}KTS{runway}");
                }

                output += string.Concat(crosswinds) + string.Concat(tailwinds);
            }

            if (windBox.Text == output) return;

            // Keep the caret where it was while the controller is typing in the WIND field.
            var selectionStart = windBox.SelectionStart;

            UpdatingWindComponents = true;

            windBox.Text = output;

            windBox.SelectionStart = Math.Min(selectionStart, baseWind.Length);

            UpdatingWindComponents = false;
        }

        private double? GetRunwayHeading(string runwayName)
        {
            if (Control.ICAO == null) return null;

            var airport = Airspace2.GetAirport(Control.ICAO);

            if (airport == null) return null;

            var runway = airport.Runways.FirstOrDefault(x => x.Name == runwayName);

            if (runway == null) return null;

            var oppositeRunway = OppositeRunway(airport, runway.Name);

            if (oppositeRunway == null) return null;

            var position = LogicalPositions.Positions.FirstOrDefault(x => x.Name == Control.ICAO);

            if (position == null) return null;

            var heading = Conversions.CalculateTrack(runway.LatLong, oppositeRunway.LatLong);

            heading = heading + position.MagneticVariation;

            return Math.Round(heading, 0);
        }

        public static Airspace2.Airport.Runway OppositeRunway(Airspace2.Airport airport, string runway)
        {
            if (runway == null) return null;

            var oppRwyNum = int.Parse(runway.Replace("L", "").Replace("C", "").Replace("R", "")) + 18;
            if (oppRwyNum > 36) oppRwyNum -= 36;
            var oppositeRunway = oppRwyNum.ToString("D2");
            if (runway.EndsWith("L"))
                oppositeRunway += "R";
            else if (runway.EndsWith("R"))
                oppositeRunway += "L";
            else if (runway.EndsWith("C"))
                oppositeRunway += "C";

            return airport.Runways.FirstOrDefault(x => x.Name == oppositeRunway);
        }

        private void ComboBoxRunway_SelectedIndexChanged(object sender, EventArgs e)
        {
            CalculateWind();
        }

        private void ButtonRecord_Click(object sender, EventArgs e)
        {
            if (Control.Recording)
            {
                Control.StopRecording();
            }
            else
            {
                Control.StartRecording();
            }

            RefreshForm();
        }
    }
}
