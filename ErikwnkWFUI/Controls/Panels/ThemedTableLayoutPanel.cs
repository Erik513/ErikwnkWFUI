using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The table layout panel of <c>UIStyles.TableLayoutPanels</c>: the standard <see cref="TableLayoutPanel"/>, drawn without
    /// flicker, with the scroll bars in the style of the theme, with an optional frame in the library's color
    /// (<see cref="Panel.BorderStyle"/>, <see cref="BorderColor"/>) and with the lines of <c>CellBorderStyle.Single</c> in colors of the
    /// library (<see cref="CellBorderColor"/>, <see cref="InnerCellBorderColor"/>, <see cref="OuterCellBorderColor"/>) of which the
    /// ones to show can be chosen (<see cref="CellBorderLines"/>). Everything else is the standard control. Named
    /// <c>ThemedTableLayoutPanel</c> so that it does not clash with the standard class in the files that use both namespaces.
    /// </summary>
    public class ThemedTableLayoutPanel : TableLayoutPanel
    {
        private CellBorderLines _cellBorderLines = CellBorderLines.All;

        private readonly PanelFrame _frame;

        /// <summary>
        /// The color of the frame when <see cref="Panel.BorderStyle"/> is <c>FixedSingle</c> (the system draws it in its own
        /// color, which is painted over; of <c>Fixed3D</c> only the outer line). Follows the theme (and, on a color, is moved until it is seen against it) until it is set;
        /// <see cref="Color.Empty"/> gives that back. There is no frame by default.
        /// </summary>
        public Color BorderColor
        {
            get { return _frame.Color; }
            set { _frame.Color = value; }
        }

        private readonly ThemeColor _cellBorderColor;
        private readonly ThemeColor _innerCellBorderColor;
        private readonly ThemeColor _outerCellBorderColor;
        private readonly List<Rectangle> _cells = new List<Rectangle>();

        /// <summary>
        /// The color of the lines around and between the cells when <see cref="TableLayoutPanel.CellBorderStyle"/> is <c>Single</c>
        /// (the system draws them in its own color, which is painted over; the other styles are left to the system). Follows the
        /// theme (and, on a color, is moved until it is seen against it) until it is set; <see cref="Color.Empty"/> gives that back.
        /// <see cref="InnerCellBorderColor"/> and
        /// <see cref="OuterCellBorderColor"/> take other colors for one kind of line. There are no lines by default.
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
        /// The color of the lines between the cells. Left empty (the default) it is the <see cref="CellBorderColor"/>, also when
        /// that changes; <see cref="Color.Empty"/> gives that back.
        /// </summary>
        public Color InnerCellBorderColor
        {
            get { return _innerCellBorderColor.Value; }
            set
            {
                if (value.IsEmpty)
                    _innerCellBorderColor.Reset();
                else
                    _innerCellBorderColor.Set(value);

                Invalidate();
            }
        }

        /// <summary>
        /// The color of the lines around the whole table (the outer edge of the cells; not the frame of <see cref="Panel.BorderStyle"/>). Left
        /// empty (the default) it is the <see cref="CellBorderColor"/>, also when that changes; <see cref="Color.Empty"/> gives that back.
        /// </summary>
        public Color OuterCellBorderColor
        {
            get { return _outerCellBorderColor.Value; }
            set
            {
                if (value.IsEmpty)
                    _outerCellBorderColor.Reset();
                else
                    _outerCellBorderColor.Set(value);

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

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            _frame.AfterWindowMessage(ref m);
        }

        public ThemedTableLayoutPanel()
        {
            _frame = new PanelFrame(this);
            _cellBorderColor = new ThemeColor(() => UIColors.GetLineColor(SurfaceColor.Of(this)));
            _innerCellBorderColor = new ThemeColor(() => CellBorderColor);
            _outerCellBorderColor = new ThemeColor(() => CellBorderColor);
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
            // The lines belong to the background: what a Paint handler of the application draws comes over them, not under.
            if (CellBorderStyle == TableLayoutPanelCellBorderStyle.Single && _cells.Count > 0)
                PaintLines(e.Graphics);

            base.OnPaint(e);
        }

        private void PaintLines(Graphics graphics)
        {

            // The system has drawn all the lines in its own color: the ones that are not wanted are painted over with what is
            // behind them, the others in their color. Neighbors share a line, so the lines are the distinct edges of the cells.
            GetLines(out List<Rectangle> outer, out List<Rectangle> inner);
            bool showOuter = _cellBorderLines != CellBorderLines.InnerOnly;
            bool showInner = _cellBorderLines != CellBorderLines.OuterOnly;

            if (!showOuter)
                Hide(graphics, outer);

            if (!showInner)
                Hide(graphics, inner);

            if (showOuter)
                Fill(graphics, outer, OuterCellBorderColor);

            if (showInner)
                Fill(graphics, inner, InnerCellBorderColor);
        }

        // The lines lie around the bounds of a cell, one pixel outside them. The outer ones are the first and the last of each
        // direction (with their corners); the inner ones lie between the outer ones (their crossings belong to them).
        private void GetLines(out List<Rectangle> outer, out List<Rectangle> inner)
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

            outer = new List<Rectangle>
            {
                new Rectangle(left, top, 1, bottom - top + 1),
                new Rectangle(right, top, 1, bottom - top + 1),
                new Rectangle(left, top, right - left + 1, 1),
                new Rectangle(left, bottom, right - left + 1, 1)
            };

            inner = new List<Rectangle>();

            foreach (int x in columns.Where(x => x != left && x != right))
                inner.Add(new Rectangle(x, top + 1, 1, bottom - top - 1));

            foreach (int y in rows.Where(y => y != top && y != bottom))
                inner.Add(new Rectangle(left + 1, y, right - left - 1, 1));

            // (With one column or row there may be nothing of a kind, and drawing no rectangles at all is an error.)
            outer.RemoveAll(line => line.Width <= 0 || line.Height <= 0);
            inner.RemoveAll(line => line.Width <= 0 || line.Height <= 0);
        }

        private static void Fill(Graphics graphics, List<Rectangle> lines, Color color)
        {
            if (lines.Count == 0)
                return;

            using (SolidBrush brush = new SolidBrush(color))
                graphics.FillRectangles(brush, lines.ToArray());
        }

        // Paints over lines: in the background of the table, or - when it is transparent - in what lies behind it.
        private void Hide(Graphics graphics, List<Rectangle> lines)
        {
            if (lines.Count == 0)
                return;

            if (BackColor.A == 255)
            {
                Fill(graphics, lines, BackColor);

                return;
            }

            GraphicsState state = graphics.Save();

            try
            {
                using (Region region = new Region(Rectangle.Empty))
                {
                    foreach (Rectangle line in lines)
                        region.Union(line);

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
