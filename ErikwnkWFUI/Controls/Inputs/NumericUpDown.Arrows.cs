using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Native;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // The two arrow fields: their colors in each state, the mouse state they are painted from, and the window of the arrow buttons they are painted into.
    public partial class NumericUpDown : System.Windows.Forms.NumericUpDown
    {

        // The arrow field under the mouse and the one that is pressed: 0 = upper, 1 = lower, -1 = none.
        private int _hotField = -1;
        private int _pressedField = -1;

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
                    if (!Enabled)
                        break;

                    SetHotField(GetField(new Point(0, (short)((long)m.LParam >> 16))));
                    break;

                case WM_LBUTTONDOWN:
                    // (A handler of the value may have disabled the control while the press was handled.)
                    if (!Enabled)
                        break;

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

        // Where the mouse is (the tests put it elsewhere).
        internal Func<Point> CursorPosition = () => Cursor.Position;

        // The arrow field under the mouse is worked out from where the mouse is, for the moments when no mouse message says it.
        private void RefreshHotField()
        {
            if (_buttons == null || !_buttons.IsHandleCreated)
                return;

            Point point = _buttons.PointToClient(CursorPosition());

            SetHotField(_buttons.ClientRectangle.Contains(point) ? GetField(point) : -1);
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

        // The arrow buttons are a window of their own: Invalidate() on the control does not reach them.
        private void RepaintAll()
        {
            RepaintArrows();
            Invalidate();
        }

        // Paints the whole button area: the field's own color behind, a field per arrow with the arrow on it. Drawn into
        // the window of the buttons by ArrowFields.
        internal void PaintArrows(Graphics g, Size size)
        {
            using (SolidBrush back = new SolidBrush(BackColor))
                g.FillRectangle(back, new Rectangle(Point.Empty, size));

            // A field per arrow, one pixel from the edge and from the other field.
            UIColorSet colors = Colors ?? GetNeutralSet();
            int gap = Math.Max(1, Px(1));
            int cellHeight = (size.Height - 3 * gap) / 2;
            Rectangle up = new Rectangle(gap, gap, size.Width - 2 * gap, cellHeight);
            Rectangle down = new Rectangle(gap, size.Height - gap - cellHeight, size.Width - 2 * gap, cellHeight);

            PaintField(g, up, colors, 0, true);
            PaintField(g, down, colors, 1, false);
        }

        // The frame of the focus: the color of the set where it is seen against the field and against the fill it
        // surrounds; the neutral gray set is too close to its own fill for that, so it gets the text color.
        private Color GetFocusColor(UIColorSet colors, Color fill)
        {
            Color frame = UIColors.EnsureContrast(colors.Hover, BackColor, FocusFrameContrast);

            if (UIColors.GetContrastRatio(frame, fill) >= FocusFrameAgainstFill)
                return frame;

            return UIColors.EnsureContrast(ForeColor, BackColor, FocusTextContrast);
        }

        // The look of the control without a set: neutral gray fields. On a dark theme the gray set of the library; on a
        // light theme a pale gray with a dark arrow (the dark blocks of the gray set are heavy next to a white field).
        private static UIColorSet _lightNeutralSet;

        internal static UIColorSet GetNeutralSet()
        {
            if (!UIColors.IsLight(UIColors.BackgroundMedium))
                return UIStyles.Colors.Sets.Gray;

            return _lightNeutralSet ?? (_lightNeutralSet = UIColors.CreateSet(LightNeutralBasic, LightNeutralHover, LightNeutralPressed));
        }

        // The fill and the arrow of a field in a state (0 rest, 1 hovered, 2 pressed, 3 disabled). A disabled field is the
        // rest fill drawn toward the color of the field, with a muted arrow, so it reads as switched off in both themes.
        internal static (Color Fill, Color Arrow) GetStateColors(UIColorSet colors, int state, Color field)
        {
            switch (state)
            {
                case 1:
                    return (colors.Hover, GetArrowColor(colors.HoverText, colors.Hover));

                case 2:
                    return (colors.Pressed, GetArrowColor(colors.PressedText, colors.Pressed));

                case 3:
                    Color fill = UIColors.BlendToward(colors.Basic, field, DisabledFillBlend);
                    return (fill, UIColors.GetDisabledText(fill));

                default:
                    return (colors.Basic, GetArrowColor(colors.BasicText, colors.Basic));
            }
        }

        // The arrow of a set is its text color on that fill; where a set leaves less than 3:1 between the two (the pressed
        // fill of the green set), the better of black and white is taken, so the arrow is always seen.
        internal static Color GetArrowColor(Color arrow, Color fill)
        {
            return UIColors.GetContrastRatio(arrow, fill) >= ArrowContrast ? arrow : UIColors.GetBestTextColor(fill);
        }

        private void PaintField(Graphics g, Rectangle cell, UIColorSet colors, int field, bool pointsUp)
        {
            int state = !Enabled ? 3 : _pressedField == field ? 2 : _hotField == field ? 1 : 0;
            (Color fill, Color arrow) = GetStateColors(colors, state, BackColor);

            // The fields and their outline are drawn sharp; only the arrow is smoothed.
            g.SmoothingMode = SmoothingMode.None;

            using (SolidBrush brush = new SolidBrush(fill))
                g.FillRectangle(brush, cell);

            // With the focus the fields are framed in the color of the set (brought to where it is seen against the
            // field); otherwise by a quiet outline, and only where the fill comes close to the field in brightness -
            // a dark fill on a light field needs none.
            if (ContainsFocus && Enabled)
            {
                // One pixel, drawn inside the field, like the line around the whole control (two pixels made a heavy ring in a field of 14 pixels).
                // (Without a set the frame has the color of the one around the whole control, so the two match.)
                Color frame = Colors == null ? InputField.GetFrameColor(this, null, true) : GetFocusColor(colors, fill);

                using (Pen pen = new Pen(frame, Math.Max(1, Px(1))))
                {
                    pen.Alignment = PenAlignment.Inset;
                    g.DrawRectangle(pen, cell.X, cell.Y, cell.Width - 1, cell.Height - 1);
                }
            }
            else if (UIColors.GetContrastRatio(fill, BackColor) < OutlineBelowContrast)
            {
                using (Pen pen = new Pen(UIColors.GetOutlineColor(BackColor)))
                    g.DrawRectangle(pen, cell.X, cell.Y, cell.Width - 1, cell.Height - 1);
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;

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

            /// <inheritdoc/>
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
