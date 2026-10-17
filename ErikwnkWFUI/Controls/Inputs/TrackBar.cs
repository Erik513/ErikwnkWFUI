using System;
using System.ComponentModel;
using System.Globalization;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A slim, theme-matching horizontal slider: the library's version of the WinForms <c>TrackBar</c>, fully custom-drawn. The
    /// <see cref="Value"/> runs from <see cref="Minimum"/> (0 by default) to <see cref="Maximum"/> (1 by default) and is a fraction,
    /// not a whole number. The mouse drags the thumb (a press on the thumb takes hold of it where it was pressed, a press on the track
    /// puts it there), and the keys (arrows by <see cref="SmallChange"/>, Page Up and Down by <see cref="LargeChange"/>, Home and End to
    /// the ends) and the mouse wheel work as on the original. <see cref="ValueChanged"/> fires on every change from the user (drag,
    /// click on the track, keys, wheel); unlike the original it does not fire when <see cref="Value"/> is set from code, so that code
    /// which keeps the slider up to date cannot start a loop. The filled part and the thumb are gray, the color of <see cref="Colors"/>
    /// (a color set; <c>UIStyles.TrackBars.CreatePrimary</c> follows the accent) or <see cref="FillColor"/>. <see cref="ShowValuePopup"/>
    /// shows the value above the thumb, written as <see cref="FormatValue"/> says (<see cref="FormatPercent"/> for a volume from 0 to 1);
    /// a screen reader is told it is a slider and its value. The measures are scaled with the screen. Not offered: tick marks, the
    /// vertical orientation, <c>AutoSize</c>; right-to-left layouts are not supported (<c>RightToLeft</c> is ignored).
    /// </summary>
    public partial class TrackBar : Control
    {
        private double _value;
        private double _minimum;
        private double _maximum = 1.0;
        private double? _smallChange;
        private double? _largeChange;
        private Func<double, string> _formatValue;
        private int _grabOffset;
        private bool _dragging;
        private bool _hover;
        private UIColorSet _colors;
        private Func<UIColorSet> _colorsSource;
        private Color? _fillColor;

        // The measures at 96 dpi; the screen of the control scales them (see TrackHeight, ThumbRadius, TrackInsetPixels).
        private const int TrackHeight96 = 4;
        private const int ThumbRadius96 = 6;

        // The range cannot be empty: the top is at least this far above the bottom.
        private const double MinimumRange = 0.0001;

        /// <summary>
        /// Pixels the track is inset from each edge (room for the thumb) at 96 dpi. Anything meant to line up with the track (e.g. a
        /// progress bar next to the slider) should use the same left/right margin - <see cref="TrackInsetPixels"/> where the screen is
        /// scaled.
        /// </summary>
        public const int TrackInset = ThumbRadius96 + 1;

        /// <summary>The same in the pixels of the screen this slider is on (more than <see cref="TrackInset"/> where the screen is scaled).</summary>
        [Browsable(false)]
        public int TrackInsetPixels
        {
            get { return ThumbRadius + 1; }
        }

        // Another dpi than the one of the screen, for the tests.
        internal int DpiOverride;

        private int TrackHeight
        {
            get { return Dpi.Scale(this, TrackHeight96, DpiOverride); }
        }

        private int ThumbRadius
        {
            get { return Dpi.Scale(this, ThumbRadius96, DpiOverride); }
        }

        public TrackBar()
        {
            // Everything is read while painting: a change of the theme only needs a repaint.
            ThemeWatcher.Attach(this, Invalidate);
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor
                | ControlStyles.Selectable,
                true);
            Height = 20;
            TabStop = true;
            BackColor = Color.Transparent;

            InitializePopup();
        }

        /// <summary>Raised when the user changes the value (drag, click on the track, keys, wheel), once for each other value; not when code sets it.</summary>
        public event EventHandler ValueChanged;

        /// <summary>Raised when the user presses the thumb or the track to begin a drag (before the value follows the mouse).</summary>
        public event EventHandler DragStarted;

        /// <summary>Raised once when a drag ends: the mouse is released, taken away (a dialog, another window) or the slider is disabled.</summary>
        public event EventHandler DragEnded;

        /// <summary>True while the user is dragging the thumb - callers can pause external updates.</summary>
        [Browsable(false)]
        public bool IsDragging
        {
            get { return _dragging; }
        }

        /// <summary>
        /// The colors of the filled part and the thumb: the <c>Hover</c> color of the set is the main color (Green is the usual
        /// green, the primary set the accent). Null (the default) is a neutral gray; <c>UIStyles.TrackBars.CreatePrimary</c> makes
        /// one that follows the accent. <see cref="FillColor"/> wins over it. Not written by the designer (a set is code).
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIColorSet Colors
        {
            get { return _colorsSource?.Invoke() ?? _colors; }
            set
            {
                _colors = value;
                _colorsSource = null;
                Invalidate();
            }
        }

        // A set that is asked for again each time it is needed (the primary set, which follows the accent); a later Colors replaces it.
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorsSource = source;
            _colors = null;
            Invalidate();
        }

        /// <summary>An explicit fill/thumb color, overriding <see cref="Colors"/> (not the disabled look). Null (the default) uses the color of the set, or the neutral gray.</summary>
        [DefaultValue(null)]
        public Color? FillColor
        {
            get { return _fillColor; }
            set { _fillColor = value; Invalidate(); }
        }

        /// <summary>The bottom of the range (0 by default). A <see cref="Value"/> below it is raised to it; not a number and infinity are ignored.</summary>
        [DefaultValue(0.0)]
        public double Minimum
        {
            get { return _minimum; }
            set
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    return;

                _minimum = Math.Min(value, _maximum - MinimumRange);
                RangeChanged();
            }
        }

        /// <summary>The top of the range (1 by default); it is always above <see cref="Minimum"/>. A <see cref="Value"/> above it is lowered to it; not a number and infinity are ignored.</summary>
        [DefaultValue(1.0)]
        public double Maximum
        {
            get { return _maximum; }
            set
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    return;

                _maximum = Math.Max(value, _minimum + MinimumRange);
                RangeChanged();
            }
        }

        // The range has changed: the value stays in it, the popup is measured again, and the thumb moves.
        private void RangeChanged()
        {
            _value = Clamp(_value, _minimum, _maximum);
            _popupWidthStale = true;
            Invalidate();
        }

        /// <summary>How far an arrow key or a step of the mouse wheel moves the value. 0 or less (the default) is a twentieth of the range.</summary>
        public double SmallChange
        {
            get { return _smallChange ?? _maximum / 20.0 - _minimum / 20.0; }
            set { _smallChange = value > 0 ? value : (double?)null; }
        }

        private bool ShouldSerializeSmallChange()
        {
            return _smallChange.HasValue;
        }

        private void ResetSmallChange()
        {
            _smallChange = null;
        }

        /// <summary>How far Page Up and Page Down move the value. 0 or less (the default) is a tenth of the range.</summary>
        public double LargeChange
        {
            get { return _largeChange ?? _maximum / 10.0 - _minimum / 10.0; }
            set { _largeChange = value > 0 ? value : (double?)null; }
        }

        private bool ShouldSerializeLargeChange()
        {
            return _largeChange.HasValue;
        }

        private void ResetLargeChange()
        {
            _largeChange = null;
        }

        /// <summary>
        /// How a value is written for the popup and for a screen reader. By default as a number with up to two decimals;
        /// <see cref="FormatPercent"/> writes a value from 0 to 1 as whole percent. Null gives the default back.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<double, string> FormatValue
        {
            get { return _formatValue ?? FormatNumber; }
            set
            {
                _formatValue = value;
                _popupWidthStale = true;
            }
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.##", CultureInfo.CurrentCulture);
        }

        /// <summary>A value from 0 to 1 as whole percent (0.1 is "10%"): for <see cref="FormatValue"/> of a volume or the like.</summary>
        public static string FormatPercent(double value)
        {
            return (int)Math.Round(value * 100) + "%";
        }

        /// <summary>
        /// Whether the value is shown in a small popup above the thumb: while the user drags it, then briefly after they let go, click
        /// once or use a key or the wheel, and for as long as the mouse hovers over the slider. Off by default.
        /// </summary>
        [DefaultValue(false)]
        public bool ShowValuePopup
        {
            get { return _showValuePopup; }
            set
            {
                if (value == _showValuePopup)
                    return;

                _showValuePopup = value;

                if (!value)
                {
                    if (_hideTimer != null)
                        _hideTimer.Stop();

                    HidePopup();
                }
            }
        }

        /// <summary>
        /// The value, from <see cref="Minimum"/> to <see cref="Maximum"/> (one outside is limited to it, infinity is the end of the range,
        /// not a number is ignored). Setting it from code updates the display without raising <see cref="ValueChanged"/>.
        /// </summary>
        [DefaultValue(0.0)]
        public double Value
        {
            get { return _value; }
            set
            {
                if (double.IsNaN(value))
                    return;

                double clamped = Clamp(value, _minimum, _maximum);
                if (Math.Abs(clamped - _value) < double.Epsilon)
                {
                    return;
                }
                _value = clamped;
                Invalidate();
                AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
            }
        }

        // How far along the track the value is, from 0 to 1.
        private double Fraction
        {
            // (Halved first: the width of a range from the lowest to the highest number would be too big for a double.)
            get { return (_value / 2 - _minimum / 2) / (_maximum / 2 - _minimum / 2); }
        }

        /// <summary>Pixel x of the thumb centre - for positioning something (e.g. a value popup) over it.</summary>
        protected int ThumbCenterX
        {
            get
            {
                return TrackLeft + (int)Math.Round(Fraction * TrackWidth);
            }
        }

        // v limited to min..max (the callers leave out a v that is not a number before).
        private static double Clamp(double v, double min, double max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private void SetValueFromUser(double v)
        {
            if (double.IsNaN(v))
            {
                return;
            }

            double clamped = Clamp(v, _minimum, _maximum);
            if (Math.Abs(clamped - _value) < double.Epsilon)
            {
                return;
            }
            _value = clamped;
            Invalidate();
            AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
            if (ValueChanged != null)
            {
                ValueChanged(this, EventArgs.Empty);
            }
        }

        private int TrackLeft
        {
            get { return TrackInsetPixels; }
        }

        private int TrackRight
        {
            get { return Width - TrackInsetPixels; }
        }

        private int TrackWidth
        {
            get { return Math.Max(1, TrackRight - TrackLeft); }
        }

        // The value at a pixel of the track: its left end is the minimum, its right end the maximum (beyond the ends they are the ends).
        private double ValueFromX(int x)
        {
            double fraction = Clamp((x - TrackLeft) / (double)TrackWidth, 0, 1);

            return _minimum * (1 - fraction) + _maximum * fraction;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Only the left button, and not while a drag is on already (a second press with the mouse held starts no second drag).
            if (e.Button == MouseButtons.Left && Enabled && !_dragging)
            {
                Focus();
                _dragging = true;

                // A press on the thumb takes hold of it where it was pressed (the thumb does not jump to the mouse); a press on the
                // track puts the thumb under the mouse and drags it from its middle.
                _grabOffset = Math.Abs(e.X - ThumbCenterX) <= ThumbRadius + Dpi.Scale(this, 2, DpiOverride) ? e.X - ThumbCenterX : 0;

                if (DragStarted != null)
                {
                    DragStarted(this, EventArgs.Empty);
                }
                SetValueFromUser(ValueFromX(e.X - _grabOffset));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                SetValueFromUser(ValueFromX(e.X - _grabOffset));
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            EndDrag();
        }

        // The drag also ends where no MouseUp comes: the mouse is taken from the control (a dialog, another window, Alt+Tab), the
        // control is disabled or its window is gone. Otherwise it would go on following the mouse with no button pressed.
        private void EndDrag()
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;
            Invalidate();

            if (DragEnded != null)
            {
                DragEnded(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture)
            {
                EndDrag();
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (!Enabled)
            {
                EndDrag();
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            // (No DragEnded: whoever listens may be on its way out as well.)
            _dragging = false;
            _hover = false;
            base.OnHandleDestroyed(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
            PopupMouseEnter();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
            PopupMouseLeave();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Up || keyData == Keys.Down
                || keyData == Keys.PageUp || keyData == Keys.PageDown
                || keyData == Keys.Home || keyData == Keys.End)
            {
                return true;
            }
            return base.IsInputKey(keyData);
        }

        // As on the original: left and down make it less, right and up more, by SmallChange; Page Down and Up by LargeChange;
        // Home and End go to the ends. A disabled slider does not move.
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!Enabled)
            {
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Down:
                    SetValueFromUser(_value - SmallChange);
                    e.Handled = true;
                    break;
                case Keys.Right:
                case Keys.Up:
                    SetValueFromUser(_value + SmallChange);
                    e.Handled = true;
                    break;
                case Keys.PageDown:
                    SetValueFromUser(_value - LargeChange);
                    e.Handled = true;
                    break;
                case Keys.PageUp:
                    SetValueFromUser(_value + LargeChange);
                    e.Handled = true;
                    break;
                case Keys.Home:
                    SetValueFromUser(_minimum);
                    e.Handled = true;
                    break;
                case Keys.End:
                    SetValueFromUser(_maximum);
                    e.Handled = true;
                    break;
            }
        }

        // A step of the wheel moves the value by SmallChange (up = more) while the slider has the focus, as on the original; the
        // parent does not scroll with it then.
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            HandledMouseEventArgs handled = e as HandledMouseEventArgs;

            if (handled != null)
            {
                handled.Handled = true;
            }

            if (!Enabled || _dragging)
            {
                return;
            }

            SetValueFromUser(_value + e.Delta / (double)SystemInformation.MouseWheelScrollDelta * SmallChange);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        // The color of the filled part and the thumb: the disabled color, else FillColor, else the main color of the set, else gray.
        internal Color GetFillColor()
        {
            if (!Enabled)
                return UIStyles.Colors.TextDisabled;
            if (_fillColor.HasValue)
                return _fillColor.Value;

            UIColorSet colors = Colors;

            return colors != null ? colors.Hover : UIColors.DisabledGray;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int midY = Height / 2;
            int trackTop = midY - TrackHeight / 2;
            int thumbX = TrackLeft + (int)Math.Round(Fraction * TrackWidth);

            Color trackColor = UIStyles.Colors.BorderMedium;
            Color fillColor = GetFillColor();

            using (SolidBrush track = new SolidBrush(trackColor))
            {
                g.FillRectangle(track, TrackLeft, trackTop, TrackWidth, TrackHeight);
            }
            using (SolidBrush fill = new SolidBrush(fillColor))
            {
                g.FillRectangle(fill, TrackLeft, trackTop, Math.Max(0, thumbX - TrackLeft), TrackHeight);
            }

            if (Enabled)
            {
                // The thumb grows slightly on hover, drag or keyboard focus -
                // that is the only focus cue (a dotted rectangle around a slider
                // looks out of place).
                int r = _hover || _dragging || Focused ? ThumbRadius : ThumbRadius - Math.Max(1, ThumbRadius / 6);
                using (SolidBrush thumb = new SolidBrush(fillColor))
                {
                    g.FillEllipse(thumb, thumbX - r, midY - r, r * 2, r * 2);
                }
            }
        }

        protected override AccessibleObject CreateAccessibilityInstance()
        {
            return new TrackBarAccessibleObject(this);
        }

        // A screen reader asks for the role (a slider) and the value, written as the popup writes it.
        private sealed class TrackBarAccessibleObject : ControlAccessibleObject
        {
            private readonly TrackBar _bar;

            public TrackBarAccessibleObject(TrackBar bar)
                : base(bar)
            {
                _bar = bar;
            }

            public override AccessibleRole Role
            {
                get { return AccessibleRole.Slider; }
            }

            public override string Value
            {
                get { return _bar.FormatValue(_bar._value); }
                set { }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposePopup();
            }

            base.Dispose(disposing);
        }
    }
}
