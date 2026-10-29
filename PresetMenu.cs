using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using vatsys;

namespace ATISPlugin
{
    // Drop-down menus for the arrow buttons: left aligned with the field they open under, with included
    // phrases marked by a tick at the right instead of a check margin that would indent every item.
    internal static class PresetMenu
    {
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

        public static ToolStripMenuItem Item(string text, bool ticked = false) => new ToolStripMenuItem(text)
        {
            ForeColor = Colours.GetColour(Colours.Identities.InteractiveText),
            Padding = new Padding(0, 1, 0, 1),
            Checked = ticked
        };

        private class TickRenderer : ToolStripProfessionalRenderer
        {
            public TickRenderer() { }

            public TickRenderer(ProfessionalColorTable colourTable) : base(colourTable) { }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                base.OnRenderItemText(e);

                if (!(e.Item is ToolStripMenuItem item) || !item.Checked) return;

                var x = e.Item.Width - 16;
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
