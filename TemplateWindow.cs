using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using vatsys;

namespace ATISPlugin
{
    // A preset with blanks to fill in, written in square brackets:
    //   [ACFT TYPE]                        free text
    //   [INTENSITY:LIGHT/MODERATE/STRONG]  choice, named INTENSITY
    //   [RWY], [OTHER RWY]                 choice of runways (any name containing RWY)
    //   [TIME]                             free text, starts as the current UTC time
    //   [TIME+15]                          free text, starts 15 minutes from now
    // An airport's <Default Name="..."> gives the starting value for a blank of that name.
    public static class Template
    {
        private static readonly Regex PlaceholderRegex = new Regex("\\[([^\\]]+)\\]");

        private static readonly Regex TimeRegex = new Regex("^TIME(?:\\+(\\d+))?$");

        public static bool IsTemplate(string text) => PlaceholderRegex.IsMatch(text ?? string.Empty);

        public static List<string> Placeholders(string text) =>
            PlaceholderRegex.Matches(text ?? string.Empty).Cast<Match>().Select(x => x.Groups[1].Value).Distinct().ToList();

        public static string Name(string placeholder) => placeholder.Split(':').First().Trim();

        // The name shown next to the blank, e.g. TIME for TIME+15.
        public static string Label(string placeholder)
        {
            var name = Name(placeholder);

            return TimeRegex.IsMatch(name) ? "TIME" : name;
        }

        public static List<string> Choices(string placeholder)
        {
            var parts = placeholder.Split(':');

            return parts.Length > 1 ? parts[1].Split('/').Select(x => x.Trim()).ToList() : null;
        }

        public static string Default(string placeholder, Dictionary<string, string> defaults, DateTime utcNow)
        {
            var name = Name(placeholder);

            var time = TimeRegex.Match(name);

            if (time.Success)
            {
                var minutes = time.Groups[1].Success ? int.Parse(time.Groups[1].Value) : 0;

                return utcNow.AddMinutes(minutes).ToString("HHmm");
            }

            return defaults != null && defaults.TryGetValue(name, out var value) ? value.Trim().ToUpper() : string.Empty;
        }

        public static string Fill(string text, Dictionary<string, string> values) =>
            PlaceholderRegex.Replace(text, x => values.TryGetValue(x.Groups[1].Value, out var value) ? value : x.Value);
    }

    // Asks for each blank in a template and builds the finished line.
    internal class TemplateWindow : BaseForm
    {
        private const int WindowWidth = 472;
        private const int RowHeight = 31;
        private const int ButtonWidth = 26;

        private readonly string TemplateText;
        private readonly ToolStripRenderer MenuRenderer;
        private readonly Dictionary<string, Func<string>> Inputs = new Dictionary<string, Func<string>>();
        private readonly Dictionary<string, Label> Labels = new Dictionary<string, Label>();

        public string Result { get; private set; }

        public TemplateWindow(string template, List<string> runways, Dictionary<string, string> defaults, ToolStripRenderer menuRenderer)
        {
            TemplateText = template;
            MenuRenderer = menuRenderer;

            Text = "ATIS Template";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Resizeable = false;
            HideOnClose = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);

            var y = 8;

            var preview = new Label
            {
                Location = new Point(8, y),
                MaximumSize = new Size(WindowWidth - 16, 0),
                AutoSize = true,
                Text = template
            };

            Controls.Add(preview);

            y += preview.PreferredHeight + 12;

            var placeholders = Template.Placeholders(template);

            // Labels are left aligned in a column as wide as the longest one; inputs fill the rest.
            var labelWidth = placeholders.Select(x => TextRenderer.MeasureText(Template.Label(x), Font).Width).DefaultIfEmpty(0).Max() + 8;

            var inputX = 8 + labelWidth;
            var inputWidth = WindowWidth - inputX - 8;

            var utcNow = DateTime.UtcNow;

            foreach (var placeholder in placeholders)
            {
                var name = Template.Name(placeholder);

                var label = new Label
                {
                    Location = new Point(8, y + 3),
                    Size = new Size(labelWidth, 17),
                    TextAlign = ContentAlignment.TopLeft,
                    Text = Template.Label(placeholder)
                };

                Controls.Add(label);

                Labels.Add(placeholder, label);

                var initial = Template.Default(placeholder, defaults, utcNow);

                var choices = Template.Choices(placeholder);

                if (choices == null && name.Contains("RWY") && runways.Any()) choices = PairedRunways(template, placeholder, runways);

                if (choices != null)
                {
                    if (!choices.Contains(initial)) initial = choices.First();

                    Inputs.Add(placeholder, AddChoice(inputX, y, inputWidth, choices, initial));
                }
                else
                {
                    var textField = new TextField
                    {
                        CharacterCasing = CharacterCasing.Upper,
                        Location = new Point(inputX, y),
                        Size = new Size(inputWidth, 25),
                        Text = initial
                    };

                    Controls.Add(textField);

                    Inputs.Add(placeholder, () => textField.Text.Trim());
                }

                y += RowHeight;
            }

            y += 8;

            var buttonWidth = (WindowWidth - 22) / 2;

            var buttonOk = new GenericButton
            {
                Text = "OK",
                Location = new Point(8, y),
                Size = new Size(buttonWidth, 28)
            };

            buttonOk.Click += ButtonOk_Click;

            var buttonCancel = new GenericButton
            {
                Text = "Cancel",
                Location = new Point(WindowWidth - 8 - buttonWidth, y),
                Size = new Size(buttonWidth, 28)
            };

            buttonCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.Add(buttonOk);
            Controls.Add(buttonCancel);

            ClientSize = new Size(WindowWidth, y + 28 + 8);
        }

        // For "[RWY] AND R" only the L runways make sense (and R runways for "AND L").
        private static List<string> PairedRunways(string template, string placeholder, List<string> runways)
        {
            var pair = Regex.Match(template, Regex.Escape($"[{placeholder}]") + "\\s+AND\\s+([LR])\\b");

            if (!pair.Success) return runways;

            var side = pair.Groups[1].Value == "R" ? "L" : "R";

            var paired = runways.Where(x => x.EndsWith(side)).ToList();

            return paired.Any() ? paired : runways;
        }

        // A read only box with an arrow button, matching the editor's MODE row.
        private Func<string> AddChoice(int x, int y, int width, List<string> choices, string initial)
        {
            var reference = new TextField();

            var field = new TextField
            {
                ReadOnly = true,
                Location = new Point(x, y),
                Size = new Size(width - ButtonWidth - 4, 25),
                Text = initial
            };

            field.BackColor = reference.BackColor;
            field.ForeColor = reference.ForeColor;

            reference.Dispose();

            var button = new PresetButton
            {
                Location = new Point(x + width - ButtonWidth, y),
                Size = new Size(ButtonWidth, field.Height)
            };

            var menu = PresetMenu.Create(MenuRenderer);

            void ShowMenu()
            {
                menu.Items.Clear();

                foreach (var choice in choices)
                {
                    var item = PresetMenu.Item(choice, choice == field.Text);

                    item.Click += (s, e) => field.Text = choice;

                    menu.Items.Add(item);
                }

                menu.Show(field, new Point(0, field.Height));
            }

            button.Click += (s, e) => ShowMenu();
            field.MouseUp += (s, e) => ShowMenu();

            Controls.Add(field);
            Controls.Add(button);

            return () => field.Text.Trim();
        }

        private void ButtonOk_Click(object sender, EventArgs e)
        {
            var values = Inputs.ToDictionary(x => x.Key, x => x.Value());

            var missing = false;

            foreach (var value in values)
            {
                var empty = string.IsNullOrWhiteSpace(value.Value);

                Labels[value.Key].ForeColor = empty
                    ? Colours.GetColour(Colours.Identities.Warning)
                    : Colours.GetColour(Colours.Identities.InteractiveText);

                missing |= empty;
            }

            if (missing) return;

            Result = Template.Fill(TemplateText, values);

            DialogResult = DialogResult.OK;

            Close();
        }
    }
}
