using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using vatsys;
using VATSYSControls;

namespace ATISPlugin
{
    // Lets the airport drop-down be searched by typing. A text field sits over the drop-down's text, leaving
    // its arrow to open the full list. Typing lists the best matches below it, by ICAO or name, in part or with
    // a letter wrong ("SYD", "KINGSFORD", "YSY", "MELBORNE"). Up and Down move through them, Enter or a click
    // picks one, and Esc or leaving the field puts back the airport selected.
    internal sealed class AirportSearch
    {
        private const int MaxResults = 10;

        private readonly DropDownBox Box;
        private readonly TextField Field;
        private readonly ListBox List;

        // Index in Box.Items of each item listed.
        private readonly List<int> Results = new List<int>();

        // Set while the field's text is changed here, not by typing.
        private bool Updating;

        public AirportSearch(DropDownBox box)
        {
            Box = box;

            var parent = box.Parent;

            Field = new TextField
            {
                CharacterCasing = CharacterCasing.Upper,
                Location = box.Location,
                // The arrow button is drawn as a square at the right of the drop-down.
                Size = new Size(box.Width - box.Height, box.Height),
                Font = box.Font,
                TabIndex = box.TabIndex,
                Enabled = box.Enabled
            };

            List = new ListBox
            {
                Visible = false,
                DrawMode = DrawMode.OwnerDrawFixed,
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false,
                TabStop = false,
                BackColor = Colours.GetColour(Colours.Identities.WindowBackground),
                ForeColor = Colours.GetColour(Colours.Identities.InteractiveText),
                Font = MMI.eurofont_winsml
            };

            List.ItemHeight = Math.Max(List.Font.Height + 4, 16);

            parent.Controls.Add(Field);
            parent.Controls.Add(List);

            Field.BringToFront();

            Field.TextChanged += (s, e) => Safely(() => { if (!Updating && Field.Focused) Search(); });
            Field.KeyDown += (s, e) => Safely(() => OnKeyDown(e));
            Field.GotFocus += (s, e) => Field.BeginInvoke(new Action(() => Safely(Field.SelectAll)));
            Field.Leave += (s, e) => Field.BeginInvoke(new Action(() => Safely(() => { if (!List.Focused) Close(); })));

            List.DrawItem += (s, e) => Safely(() => DrawItem(e));
            List.MouseClick += (s, e) => Safely(() => Pick(List.IndexFromPoint(e.Location)));
            List.Leave += (s, e) => Safely(() => { if (!Field.Focused) Close(); });

            Box.SelectedIndexChanged += (s, e) => Safely(() => { if (!Field.Focused) ShowSelected(); });
            Box.EnabledChanged += (s, e) => Safely(() => { Field.Enabled = Box.Enabled; if (!Box.Enabled) Close(); });
            Box.VisibleChanged += (s, e) => Field.Visible = Box.Visible;
        }

        // Shows the selected airport, as after the drop-down's items or selection change.
        public void ShowSelected()
        {
            if (Field.Focused && List.Visible) return;

            SetText(Box.SelectedIndex >= 0 && Box.SelectedIndex < Box.Items.Count ? Box.Items[Box.SelectedIndex] : string.Empty);
        }

        private void SetText(string text)
        {
            if (Field.Text == text) return;

            Updating = true;

            try { Field.Text = text; }
            finally { Updating = false; }
        }

        private void Search()
        {
            Results.Clear();

            Results.AddRange(Find(Box.Items, Field.Text, MaxResults));

            if (Results.Count == 0)
            {
                List.Visible = false;

                return;
            }

            List.BeginUpdate();

            List.Items.Clear();

            foreach (var index in Results) List.Items.Add(Box.Items[index]);

            List.SelectedIndex = 0;

            List.EndUpdate();

            List.Location = new Point(Field.Left, Field.Bottom + 1);
            List.Size = new Size(Box.Width, Results.Count * List.ItemHeight + 2);
            List.Visible = true;
            List.BringToFront();
        }

        private void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                case Keys.Up:
                    if (!List.Visible) Search();
                    if (!List.Visible) break;
                    var step = e.KeyCode == Keys.Down ? 1 : -1;
                    List.SelectedIndex = Math.Max(0, Math.Min(List.Items.Count - 1, List.SelectedIndex + step));
                    break;
                case Keys.Enter:
                    if (List.Visible) Pick(Math.Max(List.SelectedIndex, 0));
                    break;
                case Keys.Escape:
                    Close();
                    Field.SelectAll();
                    break;
                default:
                    return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void Pick(int listIndex)
        {
            if (listIndex < 0 || listIndex >= Results.Count) return;

            var index = Results[listIndex];

            List.Visible = false;

            if (Box.SelectedIndex != index) Box.SelectedIndex = index;

            SetText(Box.Items[index]);

            Field.SelectAll();
        }

        private void Close()
        {
            List.Visible = false;

            ShowSelected();
        }

        private void DrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var selected = (e.State & DrawItemState.Selected) != 0;

            var back = selected ? Colours.GetColour(Colours.Identities.InteractiveText) : List.BackColor;
            var fore = selected ? List.BackColor : List.ForeColor;

            using (var brush = new SolidBrush(back)) e.Graphics.FillRectangle(brush, e.Bounds);

            TextRenderer.DrawText(e.Graphics, List.Items[e.Index].ToString(), List.Font, new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height),
                fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }

        // Events here run outside the editor's own handlers, where an exception would take vatSys down.
        private static void Safely(Action action)
        {
            try { action(); }
            catch (Exception ex) { Errors.Add(new Exception($"Could not search the airports: {ex.Message}"), Plugin.DisplayName); }
        }

        // The indexes of the items best matching the query, best first; items are "ICAO" or "ICAO - NAME".
        // Every word of the query has to match the item somehow.
        public static IEnumerable<int> Find(IList<string> items, string query, int max)
        {
            var tokens = Words(query);

            if (tokens.Length == 0) return Enumerable.Empty<int>();

            return items
                .Select((item, index) => new { index, score = Score(item, tokens) })
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.score)
                .ThenBy(x => x.index)
                .Take(max)
                .Select(x => x.index)
                .ToList();
        }

        private static string[] Words(string text) =>
            new string(((text ?? string.Empty).ToUpperInvariant()).Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray())
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        private static int Score(string item, string[] tokens)
        {
            if (string.IsNullOrWhiteSpace(item)) return 0;

            var upper = item.ToUpperInvariant();

            var icao = upper.Length >= 4 ? upper.Substring(0, 4) : upper;

            var name = upper.Length > 4 ? upper.Substring(4) : string.Empty;

            var words = Words(name);

            var total = 0;

            foreach (var token in tokens)
            {
                var score = TokenScore(token, icao, words, string.Concat(words));

                if (score == 0) return 0;

                total += score;
            }

            return total;
        }

        private static int TokenScore(string token, string icao, string[] words, string joined)
        {
            if (icao == token) return 100;

            if (icao.StartsWith(token)) return 80;

            if (words.Any(w => w == token)) return 70;

            if (words.Any(w => w.StartsWith(token))) return 60;

            if (icao.Contains(token) || joined.Contains(token)) return 40;

            // Letters left out of the ICAO ("YSY").
            if (token.Length >= 2 && IsSubsequence(token, icao)) return 30;

            // A letter or two wrong, missing or extra in an ICAO or the start of a word ("MELBORNE", "YSDY").
            if (token.Length >= 4)
            {
                var allowed = token.Length >= 7 ? 2 : 1;

                if (new[] { icao }.Concat(words).Any(w => CloseTo(token, w, allowed))) return 20;
            }

            // Letters left out of the name ("KSMITH").
            if (token.Length >= 3 && IsSubsequence(token, joined)) return 10;

            return 0;
        }

        private static bool IsSubsequence(string token, string text)
        {
            var i = 0;

            foreach (var c in text)
            {
                if (i < token.Length && c == token[i]) i++;
            }

            return i == token.Length;
        }

        // Whether the token is within the allowed edits of the word, or of its start (so it can be typed part way).
        private static bool CloseTo(string token, string word, int allowed)
        {
            if (word.Length < 3) return false;

            for (var length = Math.Max(3, token.Length - allowed); length <= Math.Min(word.Length, token.Length + allowed); length++)
            {
                if (Distance(token, word.Substring(0, length)) <= allowed) return true;
            }

            return false;
        }

        // Levenshtein distance, counting a swap of two letters next to each other as one edit.
        private static int Distance(string a, string b)
        {
            var d = new int[a.Length + 1, b.Length + 1];

            for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (var j = 0; j <= b.Length; j++) d[0, j] = j;

            for (var i = 1; i <= a.Length; i++)
            {
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;

                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);

                    if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1]) d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
                }
            }

            return d[a.Length, b.Length];
        }
    }
}
