using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using static ErikwnkWFUI.Native.NativeMethods;
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
        private CellBorderLines _cellBorderLines = CellBorderLines.All;

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

        /// <summary>
        /// Whether the panel has a frame in <see cref="BorderColor"/> - a shortcut for <see cref="Panel.BorderStyle"/>
        /// <c>FixedSingle</c> (on) and <c>None</c> (off, the default). The frame takes one pixel from each side of the area inside.
        /// </summary>
        public bool ShowBorder
        {
            get { return BorderStyle != BorderStyle.None; }
            set
            {
                if (value != ShowBorder)
                    BorderStyle = value ? BorderStyle.FixedSingle : BorderStyle.None;
            }
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

        /// <summary>
        /// Which of the lines of <see cref="TableLayoutPanel.CellBorderStyle"/> <c>Single</c> are shown: all of them (the default), only
        /// the ones around the whole table, or only the ones between the cells. The lines that are not shown leave an empty
        /// pixel in their place (the layout does not change), painted in the background of the table or - with a transparent
        /// table - of what lies behind it. A background image is not repainted there.
        /// </summary>
        public CellBorderLines CellBorderLines
        {
            get { return _cellBorderLines; }
            set
            {
                _cellBorderLines = value;
                Invalidate();
            }
        }

        // Where both scroll bars meet, the system leaves a light square even with dark bars.
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_NCPAINT)
                NativeControlTheme.PaintScrollBarCorner(this);
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

            if (CellBorderStyle != TableLayoutPanelCellBorderStyle.Single || _cells.Count == 0)
                return;

            // The lines lie around the bounds of a cell, one pixel outside them.
            using (Pen pen = new Pen(CellBorderColor))
            {
                foreach (Rectangle cell in _cells)
                    e.Graphics.DrawRectangle(pen, cell.X - 1, cell.Y - 1, cell.Width + 1, cell.Height + 1);
            }

            if (_cellBorderLines != CellBorderLines.All)
                HideLines(e.Graphics);
        }

        // The lines that are not wanted are painted over with what is behind them. Neighbors share a line, so the lines are
        // the distinct edges of the cells: the outer ones are the first and the last of each direction.
        private void HideLines(Graphics graphics)
        {
            int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
            SortedSet<int> columns = new SortedSet<int>();
            SortedSet<int> rows = new SortedSet<int>();

            foreach (Rectangle cell in _cells)
            {
                columns.Add(cell.X - 1);
                columns.Add(cell.Right);
                rows.Add(cell.Y - 1);
                rows.Add(cell.Bottom);
                left = Math.Min(left, cell.X - 1);
                top = Math.Min(top, cell.Y - 1);
                right = Math.Max(right, cell.Right);
                bottom = Math.Max(bottom, cell.Bottom);
            }

            List<Rectangle> hidden = new List<Rectangle>();

            if (_cellBorderLines == CellBorderLines.InnerOnly)
            {
                hidden.Add(new Rectangle(left, top, 1, bottom - top + 1));
                hidden.Add(new Rectangle(right, top, 1, bottom - top + 1));
                hidden.Add(new Rectangle(left, top, right - left + 1, 1));
                hidden.Add(new Rectangle(left, bottom, right - left + 1, 1));
            }
            else
            {
                // The crossings of the inner lines belong to them; where an inner line meets an outer one, the outer one stays.
                foreach (int x in columns.Where(x => x != left && x != right))
                    hidden.Add(new Rectangle(x, top + 1, 1, bottom - top - 1));

                foreach (int y in rows.Where(y => y != top && y != bottom))
                    hidden.Add(new Rectangle(left + 1, y, right - left - 1, 1));
            }

            // (With one column or row there may be nothing to hide, and drawing no rectangles at all is an error.)
            hidden.RemoveAll(strip => strip.Width <= 0 || strip.Height <= 0);

            if (hidden.Count == 0)
                return;

            if (BackColor.A == 255)
            {
                using (SolidBrush brush = new SolidBrush(BackColor))
                    graphics.FillRectangles(brush, hidden.ToArray());

                return;
            }

            GraphicsState state = graphics.Save();

            try
            {
                using (Region region = new Region(Rectangle.Empty))
                {
                    foreach (Rectangle strip in hidden)
                        region.Union(strip);

                    graphics.SetClip(region, CombineMode.Replace);
                }

                if (Parent != null)
                    ParentBackground.Paint(this, graphics, args => InvokePaintBackground(Parent, args));
            }
            finally
            {
                graphics.Restore(state);
            }
        }
    }
}
