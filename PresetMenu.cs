using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using vatsys;

namespace ATISPlugin
{
    // Drop-down menus for the arrow buttons: left aligned with the field they open under. In menus that tick
    // items (included phrases, the current choice), the tick sits in a narrow column left of the text, where
    // the eye starts reading, rather than the wide check margin.
    internal static class PresetMenu
    {
        private const int TickColumn = 18;

        public static ContextMenuStrip Create(ToolStripRenderer vatsysRenderer)
        {
            var colourTable = (vatsysRenderer as ToolStripProfessionalRenderer)?.ColorTable;

            return new ContextMenuStrip
            {
                BackColor = Colours.GetColour(Colours.Identities.WindowBackground),
                ForeColor = Colours.GetColour(Colours.Identities.InteractiveText),
                Font = MMI.eurofont_winsml,
                ShowImageMargin = false,
                ShowCheckMargin = false,
                Padding = new Padding(0, 2, 0, 2),
                Renderer = colourTable == null ? new TickRenderer() : new TickRenderer(colourTable)
            };
        }

        // Marks items drawn with a tick column.
        private static readonly object TickColumnTag = new object();

        // tickColumn leaves room for a tick, so every item in a menu that ticks lines up whether ticked or not.
        // Drop-down menus ignore an item's left padding when placing its text, so the renderer moves the text
        // across, and right padding widens the item by the same amount.
        public static ToolStripMenuItem Item(string text, bool ticked = false, bool tickColumn = false) => new ToolStripMenuItem(text)
        {
            ForeColor = Colours.GetColour(Colours.Identities.InteractiveText),
            Padding = new Padding(0, 1, tickColumn ? TickColumn : 0, 1),
            Checked = ticked,
            Tag = tickColumn ? TickColumnTag : null
        };

        private class TickRenderer : ToolStripProfessionalRenderer
        {
            public TickRenderer() { }

            public TickRenderer(ProfessionalColorTable colourTable) : base(colourTable) { }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                var tickColumn = e.Item.Tag == TickColumnTag;

                var tickX = e.TextRectangle.Left;

                if (tickColumn)
                {
                    var text = e.TextRectangle;

                    text.Offset(TickColumn, 0);

                    e.TextRectangle = text;
                }

                base.OnRenderItemText(e);

                if (!(e.Item is ToolStripMenuItem item) || !item.Checked) return;

                // In the tick column, where the text would otherwise start.
                var x = tickColumn ? tickX + 2 : e.Item.Width - 16;
                var y = e.Item.Height / 2;

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (var pen = new Pen(e.TextColor, 2))
                {
                    e.Graphics.DrawLines(pen, new[] { new Point(x, y), new Point(x + 3, y + 3), new Point(x + 9, y - 4) });
                }
            }
        }
    }
}
