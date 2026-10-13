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
    /// The standard <see cref="System.Windows.Forms.NumericUpDown"/> with the two arrow fields drawn by the library, in
    /// the colors of a <see cref="UIColorSet"/> (the standard control draws them in the system's look, a light block
    /// next to a dark field), a frame of the library around the field, and a prefix and a suffix for the number.
    /// Everything else is the standard control: the text field, the value, the limits, the keyboard, the mouse wheel and
    /// the repeat while an arrow is held.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Look.</b> The two arrow fields take their fill and arrow from <see cref="Colors"/> for rest, hovered, pressed
    /// and disabled; a field whose fill is close to the color of the field gets a quiet outline. With the focus the fields
    /// are framed in two pixels in the color of the set. The frame around the whole control is the border of
    /// <c>BorderStyle.FixedSingle</c> painted over in the library's colors (see <see cref="InputField"/>): a quiet outline,
    /// with the focus the color of the set. The number is moved to the middle of the box vertically. Everything is read
    /// while painting, so a change of the theme or the accent is followed live.
    /// </para>
    /// <para>
    /// <b>Colors of the field.</b> The field's own colors are the standard control's <c>BackColor</c> and
    /// <c>ForeColor</c>; the factories set them from the theme (the one background of all input fields, dimmed when
    /// disabled) and a color set by hand stays.
    /// </para>
    /// <para>
    /// <b>Left to the standard control:</b> the text of a disabled field (Windows draws it in its gray), the thick border
    /// of <c>BorderStyle.Fixed3D</c> (only its outer pixel gets the library's frame), and right-to-left layouts (not
    /// supported by the library). Everything else the standard control offers - <c>ReadOnly</c>, <c>UpDownAlign</c>,
    /// <c>Hexadecimal</c>, <c>DecimalPlaces</c>, <c>ThousandsSeparator</c>, <c>InterceptArrowKeys</c>,
    /// <c>Accelerations</c>, <c>TextAlign</c> - works as before.
    /// </para>
    /// </remarks>
    public class NumericUpDown : System.Windows.Forms.NumericUpDown
    {
        // A field whose fill differs less than this from the field's color (contrast ratio) gets an outline.
        private const double OutlineBelowContrast = 2.0;

        // The frame of the focus must differ at least this much from the fill it surrounds.
        private const double FocusFrameAgainstFill = 1.5;

        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_PRINTCLIENT = 0x0318;
        private const int WM_MOUSELEAVE = 0x02A3;
        private const int WM_CAPTURECHANGED = 0x0215;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;

        // The two children of the standard control: the arrow buttons (UpDownButtons, not public) and the text field.
        private readonly Control _buttons;
        private readonly Control _edit;
        private readonly ArrowFields _arrowFields;

        private UIColorSet _colors;
        private Func<UIColorSet> _colorsSource;

        // The arrow field under the mouse and the one that is pressed: 0 = upper, 1 = lower, -1 = none.
        private int _hotField = -1;
        private int _pressedField = -1;

        // Moving the text field to the middle (see CenterNumber).
        private bool _centering;
        private int _shiftedTop = int.MinValue;
        private int _appliedShift;

        // Reading the number back with a prefix and a suffix (see ValidateEditText).
        private bool _validating;
        private string _prefix = "";
        private string _suffix = "";

        public NumericUpDown()
        {
            _buttons = FindButtons();
            _arrowFields = new ArrowFields(this);
            _edit = FindEdit();

            // The standard control places the text field again whenever the size or the font changes: the move that puts
            // the number in the middle is made again each time (see CenterNumber).
            if (_edit != null)
            {
                _edit.LocationChanged += (sender, e) => CenterNumber();
                _edit.SizeChanged += (sender, e) => CenterNumber();
            }

            if (_buttons != null)
            {
                // The window of the buttons is made again with every new window of the control.
                _buttons.HandleCreated += (sender, e) => _arrowFields.AssignHandle(_buttons.Handle);
                _buttons.HandleDestroyed += (sender, e) => _arrowFields.ReleaseHandle();

                if (_buttons.IsHandleCreated)
                    _arrowFields.AssignHandle(_buttons.Handle);
            }

            // The border of BorderStyle.FixedSingle (the factory's) is part of the window outside its client area, in the
            // system's gray: the library paints its own frame over it - a quiet outline, with the focus the color of the
            // set.
            InputField.AttachFrame(this, () => BorderStyle == BorderStyle.FixedSingle, () => InputField.GetFrameColor(this, Colors, ContainsFocus));

            // The colors are read while painting: a change of the theme only needs a repaint.
            ThemeWatcher.Attach(this, () => { RepaintArrows(); Invalidate(); });
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

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            RepaintArrows();
            Invalidate();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            RepaintArrows();
            Invalidate();
        }

        // ---- prefix and suffix ----

        /// <summary>Text shown before the number in the field (for example "$ "); the <see cref="System.Windows.Forms.NumericUpDown.Value"/> stays the plain number. Empty by default.</summary>
        public string Prefix
        {
            get { return _prefix; }
            set
            {
                _prefix = value ?? "";
                UpdateEditText();
            }
        }

        /// <summary>Text shown after the number in the field (for example " %" or " px"); the <see cref="System.Windows.Forms.NumericUpDown.Value"/> stays the plain number. Empty by default.</summary>
        public string Suffix
        {
            get { return _suffix; }
            set
            {
                _suffix = value ?? "";
                UpdateEditText();
            }
        }

        protected override void UpdateEditText()
        {
            base.UpdateEditText();

            if (_prefix.Length == 0 && _suffix.Length == 0)
                return;

            // Not a change by the user (that would make the standard control read the text as an edit).
            ChangingText = true;

            try
            {
                Text = _prefix + Text + _suffix;
            }
            finally
            {
                ChangingText = false;
            }
        }

        // What the user typed may still carry the prefix and the suffix (or have lost a space of them): they are taken
        // off before the number is read (the standard control reads it when the field is left and before an arrow
        // steps), and put back afterwards.
        protected override void ValidateEditText()
        {
            // Putting the bare number into the field validates again (the standard control does that for a text set by
            // code): the inner round reads it, the outer one must not take off a second suffix.
            if (_validating)
            {
                base.ValidateEditText();
                return;
            }

            _validating = true;

            try
            {
                TakeOffAffixes();
                base.ValidateEditText();
                PutOnAffixes();
            }
            finally
            {
                _validating = false;
            }
        }

        public override void UpButton()
        {
            TakeOffAffixes();
            base.UpButton();
            PutOnAffixes();
        }

        public override void DownButton()
        {
            TakeOffAffixes();
            base.DownButton();
            PutOnAffixes();
        }

        private void TakeOffAffixes()
        {
            if (_prefix.Length == 0 && _suffix.Length == 0)
                return;

            string bare = RemoveAffixes(Text);

            if (bare != Text)
                Text = bare;
        }

        private void PutOnAffixes()
        {
            if (_prefix.Length > 0 || _suffix.Length > 0)
                UpdateEditText();
        }

        private string RemoveAffixes(string text)
        {
            text = text.Trim();
            text = RemoveAffix(text, _prefix, atStart: true);

            return RemoveAffix(text, _suffix, atStart: false);
        }

        private static string RemoveAffix(string text, string affix, bool atStart)
        {
            if (affix.Length == 0)
                return text;

            foreach (string candidate in new[] { affix, affix.Trim() })
            {
                if (candidate.Length == 0)
                    continue;

                if (atStart && text.StartsWith(candidate, StringComparison.Ordinal))
                    return text.Substring(candidate.Length).TrimStart();

                if (!atStart && text.EndsWith(candidate, StringComparison.Ordinal))
                    return text.Substring(0, text.Length - candidate.Length).TrimEnd();
            }

            return text;
        }

        // The standard control draws a ring around its text field (black when it is disabled); what lies outside the two
        // children is painted over in the field's color.
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (Region ring = new Region(ClientRectangle))
            using (SolidBrush back = new SolidBrush(BackColor))
            {
                foreach (Control child in Controls)
                    ring.Exclude(child.Bounds);

                e.Graphics.FillRegion(back, ring);
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            _hotField = -1;
            _pressedField = -1;
            RepaintArrows();
            Invalidate();
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

        private Control FindEdit()
        {
            foreach (Control child in Controls)
            {
                if (child != _buttons)
                    return child;
            }

            return null;
        }

        // The text field of the standard control puts the line of text at its top edge, a fixed distance from the border,
        // so the number sits above the middle of the box with a small font and below it with a large one (measured
        // for 8 to 24 points: one pixel too high up to a line height of 16, centered from 18 to 25, then more and more
        // too low). The field is moved by that much. The standard control places it again whenever the size or the
        // font changes, which is why this follows the field's own location (and remembers what it moved it by).
        private void CenterNumber()
        {
            if (_centering || _edit == null)
                return;

            _centering = true;

            try
            {
                // Where the standard control put the field: what it has now, less what this moved it by before.
                int baseTop = _edit.Top == _shiftedTop ? _edit.Top - _appliedShift : _edit.Top;
                int lineHeight = Font.Height;
                int shift = lineHeight <= 16 ? 1 : lineHeight >= 30 ? -(int)Math.Round((lineHeight - 26) / 7.0) : 0;

                _appliedShift = shift;
                _shiftedTop = baseTop + shift;

                if (_edit.Top != _shiftedTop)
                    _edit.Top = _shiftedTop;
            }
            finally
            {
                _centering = false;
            }
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

        // Paints the whole button area: the field's own color behind, a field per arrow with the arrow on it. Drawn into
        // the window of the buttons by ArrowFields.
        internal void PaintArrows(Graphics g, Size size)
        {
            using (SolidBrush back = new SolidBrush(BackColor))
                g.FillRectangle(back, new Rectangle(Point.Empty, size));

            // A field per arrow, one pixel from the edge and from the other field.
            UIColorSet colors = Colors ?? UIStyles.Colors.Sets.Gray;
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
            Color frame = UIColors.EnsureContrast(colors.Hover, BackColor, 3.0);

            if (UIColors.GetContrastRatio(frame, fill) >= FocusFrameAgainstFill)
                return frame;

            return UIColors.EnsureContrast(ForeColor, BackColor, 4.5);
        }

        // The arrow of a set is its text color on that fill; where a set leaves less than 3:1 between the two (the pressed
        // fill of the green set), the better of black and white is taken, so the arrow is always seen.
        internal static Color GetArrowColor(Color arrow, Color fill)
        {
            return UIColors.GetContrastRatio(arrow, fill) >= 3.0 ? arrow : UIColors.GetBestTextColor(fill);
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

            // The fields and their outline are drawn sharp; only the arrow is smoothed.
            g.SmoothingMode = SmoothingMode.None;

            using (SolidBrush brush = new SolidBrush(fill))
                g.FillRectangle(brush, cell);

            // With the focus the fields are framed in the color of the set (brought to where it is seen against the
            // field); otherwise by a quiet outline, and only where the fill comes close to the field in brightness -
            // a dark fill on a light field needs none.
            if (ContainsFocus && Enabled)
            {
                // Two pixels, drawn inside the field: the fields are small, a thin frame is hardly seen.
                using (Pen pen = new Pen(GetFocusColor(colors, fill), Math.Max(2, Px(2))))
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

            using (SolidBrush brush = new SolidBrush(GetArrowColor(arrow, fill)))
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
