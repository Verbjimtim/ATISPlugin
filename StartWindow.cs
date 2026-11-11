using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using vatsys;

namespace ATISPlugin
{
    // Shown after creating an ATIS when ozserver has the airport's last one: Broadcast it as it was, or Create a
    // fresh one with the next letter. Nothing goes live until the controller chooses Broadcast. Closing the
    // window creates a fresh one.
    internal class StartWindow : BaseForm
    {
        private const int WindowWidth = 300;

        // Older than this, starting fresh is the default and the age is shown as a warning.
        public static readonly TimeSpan StaleAge = TimeSpan.FromHours(1);

        public enum Choice { StartFresh, Broadcast }

        public Choice Result { get; private set; } = Choice.StartFresh;

        public StartWindow(SavedATIS saved, DateTime utcNow)
        {
            var letter = saved.Letter ?? '?';

            var age = saved.LastUpdatedUtc == null ? (TimeSpan?)null : utcNow - saved.LastUpdatedUtc.Value;

            var stale = age == null || age.Value > StaleAge;

            Text = $"{saved.ICAO} ATIS";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Resizeable = false;
            HideOnClose = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            Font = MMI.eurofont_winsml;

            var rowHeight = Font.Height + 3;

            var y = 8;

            var time = saved.LastUpdatedUtc == null ? string.Empty : $" at {saved.LastUpdatedUtc.Value:HHmm}Z ({Age(age.Value)} ago)";

            y = AddText($"Last published ATIS for {saved.ICAO}:", y, ForeColor);

            if (stale)
            {
                y = AddText("It's over an hour old. Check it before broadcasting.", y, Colours.GetColour(Colours.Identities.Warning));
            }

            y += 4;

            // Its letter and time, then the saved fields as they'd go out, with + on the changed ones.
            var fields = new[] { new KeyValuePair<string, string>("INFO", $"{letter}{time}") }
                .Concat(saved.Content.Where(x => !string.IsNullOrWhiteSpace(x.Value))
                    .Select(x => new KeyValuePair<string, string>(x.Key, (saved.Changed.Contains(x.Key) ? "+" : string.Empty) + x.Value)))
                .ToList();

            var nameWidth = fields.Select(x => TextRenderer.MeasureText(x.Key, Font).Width).DefaultIfEmpty(0).Max() + 12;

            var valueColour = Colours.GetColour(Colours.Identities.NonInteractiveText);

            foreach (var field in fields)
            {
                // Auto sized like the header, and moved left by however far the first letter's ink sits in from
                // its cell (an I is drawn in the middle of it), so every name's first stroke lines up with the header's.
                Controls.Add(new Label
                {
                    Location = new Point(8 - (InkLeft(field.Key[0]) - InkLeft('L')), y),
                    AutoSize = true,
                    Text = field.Key,
                    ForeColor = valueColour
                });

                var value = new Label
                {
                    Location = new Point(8 + nameWidth, y),
                    MaximumSize = new Size(WindowWidth - 16 - nameWidth, 0),
                    AutoSize = true,
                    Text = field.Value
                };

                Controls.Add(value);

                y += Math.Max(rowHeight, value.PreferredHeight + 2);
            }

            y += 10;

            var buttonWidth = (WindowWidth - 22) / 2;

            var buttonBroadcast = new GenericButton
            {
                Text = "Broadcast",
                Font = Font,
                Location = new Point(8, y),
                Size = new Size(buttonWidth, 26)
            };

            buttonBroadcast.Click += (s, e) => EditorWindow.Safely("broadcast the saved ATIS", () => Finish(Choice.Broadcast));

            var buttonFresh = new GenericButton
            {
                Text = "Create New",
                Font = Font,
                Location = new Point(WindowWidth - 8 - buttonWidth, y),
                Size = new Size(buttonWidth, 26)
            };

            buttonFresh.Click += (s, e) => EditorWindow.Safely("start a fresh ATIS", () => Finish(Choice.StartFresh));

            Controls.Add(buttonBroadcast);
            Controls.Add(buttonFresh);

            // Enter takes the default: broadcasting a recent ATIS, or starting fresh from an old one.
            var preferred = stale ? buttonFresh : buttonBroadcast;

            AcceptButton = preferred;

            Shown += (s, e) => preferred.Focus();

            ClientSize = new Size(WindowWidth, y + 26 + 8);
        }

        // How far in from the left of its cell a letter's ink starts, in the window's font.
        private int InkLeft(char letter)
        {
            using (var bitmap = new Bitmap(64, 64))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);

                TextRenderer.DrawText(graphics, letter.ToString(), Font, Point.Empty, Color.Black, Color.White, TextFormatFlags.NoPadding);

                for (var x = 0; x < bitmap.Width; x++)
                {
                    for (var y = 0; y < bitmap.Height; y++)
                    {
                        if (bitmap.GetPixel(x, y).R < 128) return x;
                    }
                }
            }

            return 0;
        }

        private int AddText(string text, int y, Color colour)
        {
            var label = new Label
            {
                Location = new Point(8, y),
                MaximumSize = new Size(WindowWidth - 16, 0),
                AutoSize = true,
                ForeColor = colour,
                Text = text
            };

            Controls.Add(label);

            return y + label.PreferredHeight + 4;
        }

        private void Finish(Choice choice)
        {
            Result = choice;

            DialogResult = DialogResult.OK;

            Close();
        }

        public static string Age(TimeSpan age)
        {
            if (age < TimeSpan.Zero) age = TimeSpan.Zero;

            var minutes = (int)Math.Round(age.TotalMinutes);

            if (minutes < 60) return $"{minutes} min";

            return minutes % 60 == 0 ? $"{minutes / 60} h" : $"{minutes / 60} h {minutes % 60} min";
        }
    }
}
