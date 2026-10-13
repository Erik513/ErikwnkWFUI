using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    /// <summary>The box of a check box - outline, fill, checkmark or dash - drawn the same for the check box and for the list view.</summary>
    internal static class CheckGlyph
    {
        /// <summary>
        /// Draws just the box of a check box - outline, fill when checked or undetermined, and the
        /// mark - for the controls that show check boxes (the list view, and the check box itself). The colors are the
        /// caller's; the measures follow the screen's scaling.
        /// </summary>
        internal static void Draw(Graphics g, Rectangle box, CheckState state, Color outline, Color fill, Color mark, int dpi)
        {
            CheckBox.Metrics metrics = new CheckBox.Metrics(dpi);
            SmoothingMode previous = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = RoundedRectangle.CreatePath(box, metrics.Scale(3)))
            {
                if (state != CheckState.Unchecked)
                {
                    using (SolidBrush brush = new SolidBrush(fill))
                        g.FillPath(brush, path);
                }

                using (Pen pen = new Pen(outline, metrics.Scale(15) / 10f))
                    g.DrawPath(pen, path);
            }

            if (state == CheckState.Checked)
                DrawCheckmark(g, box, mark, metrics.Scale(2));
            else if (state == CheckState.Indeterminate)
                DrawDash(g, box, mark, metrics.Scale(2));

            g.SmoothingMode = previous;
        }

        internal static void DrawDash(Graphics g, Rectangle box, Color color, float width)
        {
            int y = box.Y + box.Height / 2;
            int left = box.X + (int)(box.Width * 0.26);
            int right = box.X + (int)(box.Width * 0.74);

            using (Pen pen = new Pen(color, width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                g.DrawLine(pen, left, y, right, y);
            }
        }

        internal static void DrawCheckmark(Graphics g, Rectangle box, Color color, float width)
        {
            Point p1 = new Point(box.X + (int)(box.Width * 0.22), box.Y + (int)(box.Height * 0.52));
            Point p2 = new Point(box.X + (int)(box.Width * 0.42), box.Y + (int)(box.Height * 0.74));
            Point p3 = new Point(box.X + (int)(box.Width * 0.80), box.Y + (int)(box.Height * 0.26));

            using (Pen pen = new Pen(color, width))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                g.DrawLines(pen, new[] { p1, p2, p3 });
            }
        }
    }
}
