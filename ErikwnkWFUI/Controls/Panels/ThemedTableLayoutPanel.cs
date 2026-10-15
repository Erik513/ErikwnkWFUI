using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The table layout panel of <c>UIStyles.TableLayoutPanels</c>: the standard <see cref="TableLayoutPanel"/>, drawn without
    /// flicker and with the scroll bars in the style of the theme. Everything else is the standard control. Named
    /// <c>ThemedTableLayoutPanel</c> so that it does not clash with the standard class in the files that use both namespaces.
    /// </summary>
    public class ThemedTableLayoutPanel : TableLayoutPanel
    {
        private readonly PanelFrame _frame;

        /// <summary>
        /// The color of the frame when <see cref="Panel.BorderStyle"/> is <c>FixedSingle</c> (the system draws it in its own
        /// color, which is painted over; of <c>Fixed3D</c> only the outer line). Follows the theme until it is set;
        /// <see cref="Color.Empty"/> gives that back. There is no frame by default.
        /// </summary>
        public Color BorderColor
        {
            get { return _frame.Color; }
            set { _frame.Color = value; }
        }

        private readonly ThemeColor _cellBorderColor = new ThemeColor(() => UIColors.BorderMedium);
        private readonly List<Rectangle> _cells = new List<Rectangle>();

        /// <summary>
        /// The color of the lines between the cells when <see cref="TableLayoutPanel.CellBorderStyle"/> is <c>Single</c> (the system
        /// draws them in its own color, which is painted over; the other styles are left to the system). Follows the theme until
        /// it is set; <see cref="Color.Empty"/> gives that back. There are no lines by default.
        /// </summary>
        public Color CellBorderColor
        {
            get { return _cellBorderColor.Value; }
            set
            {
                if (value.IsEmpty)
                    _cellBorderColor.Reset();
                else
                    _cellBorderColor.Set(value);

                Invalidate();
            }
        }

        public ThemedTableLayoutPanel()
        {
            _frame = new PanelFrame(this);
            DoubleBuffered = true;
            NativeControlTheme.Track(this);
            ThemeWatcher.Attach(this, Invalidate);
        }

        // The cells are noted while the table paints its background (the system draws its lines after them, so they are
        // painted over in OnPaint).
        protected override void OnCellPaint(TableLayoutCellPaintEventArgs e)
        {
            base.OnCellPaint(e);
            _cells.Add(e.CellBounds);
        }

        // (The table paints its cells and its lines with the background, before OnPaint.)
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            _cells.Clear();
            base.OnPaintBackground(pevent);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (CellBorderStyle != TableLayoutPanelCellBorderStyle.Single)
                return;

            // The lines lie around the bounds of a cell, one pixel outside them.
            using (Pen pen = new Pen(CellBorderColor))
            {
                foreach (Rectangle cell in _cells)
                    e.Graphics.DrawRectangle(pen, cell.X - 1, cell.Y - 1, cell.Width + 1, cell.Height + 1);
            }
        }
    }
}
