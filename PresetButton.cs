using System.Drawing;
using System.Windows.Forms;
using vatsys;

namespace ATISPlugin
{
    // A small button that draws a drop-down arrow, matching the DropDownBox arrow.
    internal class PresetButton : GenericButton
    {
        public PresetButton()
        {
            Text = string.Empty;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var colour = Enabled ? ForeColor : DisabledForeColor;

            var centreX = Width / 2;
            var centreY = Height / 2;

            var arrow = new[]
            {
                new Point(centreX - 4, centreY - 2),
                new Point(centreX + 5, centreY - 2),
                new Point(centreX, centreY + 3)
            };

            using (var brush = new SolidBrush(colour))
            {
                e.Graphics.FillPolygon(brush, arrow);
            }
        }
    }
}
