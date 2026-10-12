using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Native;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The standard <see cref="System.Windows.Forms.NumericUpDown"/> with the two arrow fields drawn by the
    /// library, in the colors of a <see cref="UIColorSet"/> - the standard control draws them in the system's
    /// look, a light block next to a dark field. Everything else is the standard control: the text field, the
    /// value, the limits, the keyboard, the mouse wheel and the repeat while an arrow is held.
    /// </summary>
    /// <remarks>
    /// Followed live: a change of the theme or the accent repaints the arrows (the set of
    /// <c>UIStyles.NumericUpDowns.CreatePrimary</c> follows the accent). The colors of the field itself are the
    /// standard control's <c>BackColor</c> and <c>ForeColor</c>; the factory sets them from the theme.
    /// <para>
    /// Left to the standard control: the border of <c>BorderStyle.FixedSingle</c>/<c>Fixed3D</c> (drawn by Windows in
    /// its own colors - the factory makes the control borderless), the text of a disabled field (Windows draws it in
    /// its gray) and right-to-left layouts (not supported by the library). Everything else the standard control
    /// offers - <c>ReadOnly</c>, <c>UpDownAlign</c>, <c>Hexadecimal</c>, <c>DecimalPlaces</c>,
    /// <c>ThousandsSeparator</c>, <c>InterceptArrowKeys</c>, <c>Accelerations</c>, <c>TextAlign</c> - works as before.
    /// </para>
    /// </remarks>
    public class NumericUpDown : System.Windows.Forms.NumericUpDown
    {
        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_PRINTCLIENT = 0x0318;
        private const int WM_MOUSELEAVE = 0x02A3;
        private const int WM_CAPTURECHANGED = 0x0215;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;

        private readonly Control _buttons;
        private readonly ArrowFields _arrowFields;
        private UIColorSet _colors;
        private Func<UIColorSet> _colorsSource;
        private int _hotField = -1;
        private int _pressedField = -1;

        public NumericUpDown()
        {
            _buttons = FindButtons();
            _arrowFields = new ArrowFields(this);

            if (_buttons != null)
            {
                _buttons.HandleCreated += (sender, e) => _arrowFields.AssignHandle(_buttons.Handle);
                _buttons.HandleDestroyed += (sender, e) => _arrowFields.ReleaseHandle();
                if (_buttons.IsHandleCreated)
                    _arrowFields.AssignHandle(_buttons.Handle);
            }

            // The colors are read while painting: a change of the theme only needs a repaint.
            ThemeWatcher.Attach(this, RepaintArrows);
        }

        /// <summary>
        /// The colors of the arrow fields: their fill at rest, hovered, pressed and disabled, and the arrow on each.
        /// Left null, the neutral gray (<c>UIStyles.Colors.Sets.Gray</c>);
        /// <c>UIStyles.NumericUpDowns.CreatePrimary</c> makes one in the accent color.
        /// </summary>
        public UIColorSet Colors
        {
            get { return _colorsSource?.Invoke() ?? _colors; }
            set
            {
                _colors = value;
                _colorsSource = null;
                RepaintArrows();
            }
        }

        // A set that is asked for again each time (the primary set, which follows the accent).
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorsSource = source;
            _colors = null;
            RepaintArrows();
        }

        // A seam for the tests: pretends another screen (0 is the real one).
        internal int DpiOverride;

        private int Px(int value96)
        {
            int dpi = DpiOverride > 0 ? DpiOverride : DeviceDpi;

            return (int)Math.Round(value96 * dpi / 96.0, MidpointRounding.AwayFromZero);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            _hotField = -1;
            _pressedField = -1;
            RepaintArrows();
        }

        // The arrow buttons are a child control of the standard control (UpDownButtons, not public).
        private Control FindButtons()
        {
            foreach (Control child in Controls)
            {
                if (child.GetType().Name == "UpDownButtons")
                    return child;
            }

            return null;
        }

        // 0 = the upper field (up), 1 = the lower one (down).
        private int GetField(Point location)
        {
            return location.Y < _buttons.Height / 2 ? 0 : 1;
        }

        // The buttons do not raise their own mouse events (they pass them on to this control with its own
        // coordinates), so the state is taken from their messages.
        private void NoteMouse(Message m)
        {
            switch (m.Msg)
            {
                case WM_MOUSEMOVE:
                    SetHotField(GetField(new Point(0, (short)((long)m.LParam >> 16))));
                    break;

                case WM_LBUTTONDOWN:
                    _hotField = GetField(new Point(0, (short)((long)m.LParam >> 16)));
                    _pressedField = _hotField;
                    RepaintArrows();
                    break;

                case WM_LBUTTONUP:
                case WM_CAPTURECHANGED:
                    if (_pressedField != -1)
                    {
                        _pressedField = -1;
                        RepaintArrows();
                    }

                    break;

                case WM_MOUSELEAVE:
                    _pressedField = -1;
                    SetHotField(-1);
                    RepaintArrows();
                    break;
            }
        }

        private void SetHotField(int field)
        {
            if (_hotField == field)
                return;

            _hotField = field;
            RepaintArrows();
        }

        private void RepaintArrows()
        {
            _buttons?.Invalidate();
        }

        // Paints the whole button area: the field's own color behind, a field per arrow with the arrow on it.
        internal void PaintArrows(Graphics g, Size size)
        {
            using (SolidBrush back = new SolidBrush(BackColor))
                g.FillRectangle(back, new Rectangle(Point.Empty, size));

            g.SmoothingMode = SmoothingMode.AntiAlias;

            UIColorSet colors = Colors ?? UIStyles.Colors.Sets.Gray;
            int gap = Math.Max(1, Px(1));
            int cellHeight = (size.Height - 3 * gap) / 2;
            Rectangle up = new Rectangle(gap, gap, size.Width - 2 * gap, cellHeight);
            Rectangle down = new Rectangle(gap, size.Height - gap - cellHeight, size.Width - 2 * gap, cellHeight);

            PaintField(g, up, colors, 0, true);
            PaintField(g, down, colors, 1, false);
        }

        private void PaintField(Graphics g, Rectangle cell, UIColorSet colors, int field, bool pointsUp)
        {
            Color fill;
            Color arrow;

            if (!Enabled)
            {
                fill = colors.Disabled;
                arrow = colors.DisabledText;
            }
            else if (_pressedField == field)
            {
                fill = colors.Pressed;
                arrow = colors.PressedText;
            }
            else if (_hotField == field)
            {
                fill = colors.Hover;
                arrow = colors.HoverText;
            }
            else
            {
                fill = colors.Basic;
                arrow = colors.BasicText;
            }

            using (SolidBrush brush = new SolidBrush(fill))
                g.FillRectangle(brush, cell);

            // The fill is kept apart from the field by an outline where the two come close in brightness.
            using (Pen pen = new Pen(UIColors.GetOutlineColor(BackColor)))
                g.DrawRectangle(pen, cell.X, cell.Y, cell.Width - 1, cell.Height - 1);

            // The arrow grows with a field that is taller than usual (a larger font), up to three times.
            float scale = Math.Min(3f, Math.Max(1f, cell.Height / (float)Px(10)));
            float width = Px(8) * scale;
            float height = Px(4) * scale;
            float centerX = cell.Left + cell.Width / 2f;
            float centerY = cell.Top + cell.Height / 2f;

            PointF[] triangle = pointsUp
                ? new[]
                {
                    new PointF(centerX - width / 2, centerY + height / 2),
                    new PointF(centerX + width / 2, centerY + height / 2),
                    new PointF(centerX, centerY - height / 2)
                }
                : new[]
                {
                    new PointF(centerX - width / 2, centerY - height / 2),
                    new PointF(centerX + width / 2, centerY - height / 2),
                    new PointF(centerX, centerY + height / 2)
                };

            using (SolidBrush brush = new SolidBrush(arrow))
                g.FillPolygon(brush, triangle);
        }

        // Takes over the painting of the arrow buttons; their behavior stays the standard one.
        private sealed class ArrowFields : NativeWindow
        {
            private readonly NumericUpDown _owner;

            public ArrowFields(NumericUpDown owner)
            {
                _owner = owner;
            }

            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case WM_ERASEBKGND:
                        m.Result = (IntPtr)1;
                        return;

                    case WM_PAINT:
                        NativePaintStruct paint;
                        IntPtr hdc = BeginPaint(Handle, out paint);

                        try
                        {
                            PaintTo(hdc);
                        }
                        finally
                        {
                            EndPaint(Handle, ref paint);
                        }

                        m.Result = IntPtr.Zero;
                        return;

                    case WM_PRINTCLIENT:
                        PaintTo(m.WParam);
                        m.Result = IntPtr.Zero;
                        return;
                }

                base.WndProc(ref m);
                _owner.NoteMouse(m);
            }

            private void PaintTo(IntPtr hdc)
            {
                Size size = _owner._buttons.ClientSize;

                if (hdc == IntPtr.Zero || size.Width <= 0 || size.Height <= 0)
                    return;

                using (BufferedGraphics buffer = BufferedGraphicsManager.Current.Allocate(hdc, new Rectangle(Point.Empty, size)))
                {
                    _owner.PaintArrows(buffer.Graphics, size);
                    buffer.Render();
                }
            }
        }
    }
}
