using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A loading spinner: an arc that turns over a faint full ring (the load is unknown), or, with <see cref="IsIndeterminate"/> off,
    /// a ring that fills up to the <see cref="Value"/> (the load is known). Fully custom-drawn, follows the theme, transparent background.
    /// The arc turns while <see cref="IsSpinning"/> is set and the control is shown and enabled and Windows lets windows animate (a spinner
    /// on a hidden window only looks four times a second whether it is shown again); <see cref="Speed"/> says how fast, and
    /// <see cref="HideWhenStopped"/> hides the ring while it does not. A <see cref="Text"/> stands next to the ring (at the
    /// <see cref="TextPosition"/>), and <see cref="ShowPercentage"/> writes the <see cref="Value"/> in its centre. The color of the arc is
    /// <see cref="ArcColor"/> if set, else, with <see cref="UseStatusGradient"/>, red over yellow to green by the <see cref="Value"/>,
    /// else the main color of <see cref="Colors"/> (a color set) or the neutral gray; a disabled spinner is in the disabled color.
    /// The <see cref="UIStyles.Spinners"/> factories preset these. For a screen reader it is a progress bar: the text is its name and, for a
    /// known load, the percentage its value. Right-to-left layouts are not supported (<c>RightToLeft</c> is ignored).
    /// </summary>
    public partial class Spinner : Control
    {
        private const int TickMs = 16;              // ~60 fps
        private const int HiddenPollMs = 250;       // how often a spinner on a hidden window looks whether it is shown again
        private const int AskEveryTicks = 32;       // Windows is asked again for its animation setting after this many ticks (~0.5 s)
        private const float MinimumDigitSize = 7f;  // smaller digits in the ring are not legible, so they are left out

        private readonly Timer _timer;
        private float _angle;
        private int _thickness;
        private float _arcSweep = 300f;
        private UIColorSet _colors;
        private Func<UIColorSet> _colorsSource;
        private Color _arcColor = Color.Empty;
        private bool _useStatusGradient;
        private int _value;
        private bool _showPercentage;
        private bool _isSpinning = true;
        private bool _isIndeterminate = true;
        private int _ticksSinceAsked;
        private bool _hideWhenStopped;
        private SpinnerSpeed _speed = SpinnerSpeed.Medium;

        public Spinner()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor,
                true);

            Size = new Size(24, 24);
            BackColor = Color.Transparent;
            TabStop = false;

            _timer = new Timer { Interval = TickMs };
            _timer.Tick += OnTick;

            // The track, the text and the gray arc are read while painting; a change of theme or accent only has to paint again.
            ThemeWatcher.Attach(this, Invalidate);
        }

        /// <summary>Stroke width in pixels. 0 (the default) scales it to the size of the control (an eighth, at least 2 px).</summary>
        [DefaultValue(0)]
        public int Thickness
        {
            get { return _thickness; }
            set
            {
                _thickness = Math.Max(0, value);
                Invalidate();
            }
        }

        /// <summary>
        /// The colors of the arc: its main color is the <c>Hover</c> color of the set (Green is the usual green, the primary set the
        /// accent). Null (the default) is the neutral gray; <c>UIStyles.Spinners.CreatePrimary</c> makes one that follows the accent.
        /// <see cref="ArcColor"/> and <see cref="UseStatusGradient"/> win over it. Not written by the designer (a set is code).
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

        /// <summary>An explicit color of the arc, overriding <see cref="Colors"/> and <see cref="UseStatusGradient"/> (not the disabled look). <see cref="Color.Empty"/> (the default) uses those rules.</summary>
        [DefaultValue(typeof(Color), "")]
        public Color ArcColor
        {
            get { return _arcColor; }
            set
            {
                _arcColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// True = the arc color tells the state of the load: a solid color blended red (0) over yellow to green (100) by
        /// <see cref="Value"/>. Ignored when <see cref="ArcColor"/> is set. As <see cref="SlimProgressBar.UseStatusGradient"/>.
        /// </summary>
        [DefaultValue(false)]
        public bool UseStatusGradient
        {
            get { return _useStatusGradient; }
            set
            {
                _useStatusGradient = value;
                Invalidate();
            }
        }

        /// <summary>
        /// The load, as a percentage from 0 to 100 (a value outside is limited to it; 0 by default). It colors the arc with
        /// <see cref="UseStatusGradient"/>, is written in the centre with <see cref="ShowPercentage"/>, is the length of the arc
        /// with <see cref="IsIndeterminate"/> off and then also the value for a screen reader.
        /// </summary>
        [DefaultValue(0)]
        public int Value
        {
            get { return _value; }
            set
            {
                int limited = Math.Max(0, Math.Min(100, value));

                if (limited == _value)
                    return;

                _value = limited;
                Invalidate();
                if (!_isIndeterminate)
                    AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
            }
        }

        /// <summary>
        /// Whether the <see cref="Value"/> is written (bold, as a number) in the centre of the ring (off by default). The digits
        /// scale to the ring, with the size of "100" so that they do not change while the number counts up; in a ring too small for
        /// them (below about 30 px) they are left out. <c>UIStyles.Spinners.CreateProgress*</c> make a ring of 40 px.
        /// </summary>
        [DefaultValue(false)]
        public bool ShowPercentage
        {
            get { return _showPercentage; }
            set
            {
                if (value == _showPercentage)
                    return;

                _showPercentage = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Whether the arc turns (true by default). False stands it still where it is, e.g. while nothing is loading; with
        /// <see cref="HideWhenStopped"/> the ring is gone then. It also stands still while the control is disabled or while Windows has the
        /// animations switched off (the ring stays visible then). A known load (<see cref="IsIndeterminate"/> off) never turns.
        /// </summary>
        [DefaultValue(true)]
        public bool IsSpinning
        {
            get { return _isSpinning; }
            set
            {
                if (value == _isSpinning)
                    return;

                _isSpinning = value;
                UpdateRunning();
                Invalidate();
            }
        }

        /// <summary>
        /// True = the ring is not drawn while <see cref="IsSpinning"/> is false (the <see cref="Text"/> stays); false (the default) =
        /// the ring stands still where it is.
        /// </summary>
        [DefaultValue(false)]
        public bool HideWhenStopped
        {
            get { return _hideWhenStopped; }
            set
            {
                if (value == _hideWhenStopped)
                    return;

                _hideWhenStopped = value;
                Invalidate();
            }
        }

        /// <summary>How fast the arc turns (a turn in about 1.2 seconds by default). It has no effect on a known load.</summary>
        [DefaultValue(SpinnerSpeed.Medium)]
        public SpinnerSpeed Speed
        {
            get { return _speed; }
            set { _speed = value; }
        }

        /// <summary>
        /// True (the default) = the load is unknown: an arc of fixed length turns. False = the load is known: the arc is as long as
        /// the <see cref="Value"/> (from the top, clockwise) and stands still.
        /// </summary>
        [DefaultValue(true)]
        public bool IsIndeterminate
        {
            get { return _isIndeterminate; }
            set
            {
                if (value == _isIndeterminate)
                    return;

                _isIndeterminate = value;
                UpdateRunning();
                Invalidate();
                AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
            }
        }

        /// <summary>How much of the circle the moving arc covers, in degrees (10..350, default 300).</summary>
        internal float ArcSweep
        {
            get { return _arcSweep; }
            set
            {
                if (value < 10f)
                    value = 10f;
                if (value > 350f)
                    value = 350f;
                _arcSweep = value;
                Invalidate();
            }
        }

        private int EffectiveThickness
        {
            get
            {
                return _thickness > 0
                    ? _thickness
                    : Math.Max(2, Math.Min(Width, Height) / 8);
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            // A window that is hidden (or a spinner taken off its window) hides the spinner without telling it (no VisibleChanged,
            // and none when the window is shown again): the clock keeps watching at a slow pace and takes up its speed when it is
            // shown again.
            if (!Visible)
            {
                if (_timer.Interval != HiddenPollMs)
                    _timer.Interval = HiddenPollMs;

                return;
            }

            if (_timer.Interval != TickMs)
                _timer.Interval = TickMs;

            if (++_ticksSinceAsked >= AskEveryTicks)
            {
                _ticksSinceAsked = 0;

                if (!ThemedLabel.AnimationsEnabled())
                {
                    _timer.Stop();

                    return;
                }
            }

            float degrees = _speed == SpinnerSpeed.Slow ? 3f : _speed == SpinnerSpeed.Fast ? 9f : 5f;

            _angle = (_angle + degrees) % 360f;
            Invalidate();
        }

        // Starts or stops the clock: it runs only while there is something to turn and somebody to see it.
        private void UpdateRunning()
        {
            bool run = Visible && IsHandleCreated && !DesignMode
                && _isSpinning && _isIndeterminate && Enabled && ThemedLabel.AnimationsEnabled();
            _ticksSinceAsked = 0;
            if (run && !_timer.Enabled)
            {
                _timer.Interval = TickMs;
                _timer.Start();
            }
            else if (!run && _timer.Enabled)
                _timer.Stop();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateRunning();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            UpdateRunning();
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateRunning();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _timer.Stop();
            base.OnHandleDestroyed(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle box = GetLayout(out Rectangle textArea, out TextFormatFlags alignment);

            if (textArea != Rectangle.Empty)
                DrawText(g, textArea, alignment);

            if (_hideWhenStopped && !_isSpinning)
                return;

            float t = EffectiveThickness;
            float inset = t / 2f + 1f;

            // Always a circle, centred - a non-square control (e.g. stretched by
            // a table cell) draws the ring in the largest square that fits, not
            // an ellipse.
            float d = Math.Min(box.Width, box.Height);
            RectangleF ring = new RectangleF(
                box.X + (box.Width - d) / 2f + inset,
                box.Y + (box.Height - d) / 2f + inset,
                d - inset * 2f,
                d - inset * 2f);

            if (ring.Width <= 0f || ring.Height <= 0f)
                return;

            using (Pen track = new Pen(GetTrackColor(), t))
            {
                g.DrawEllipse(track, ring);
            }

            using (Pen arc = new Pen(ResolveArcColor(), t))
            {
                arc.StartCap = LineCap.Round;
                arc.EndCap = LineCap.Round;

                if (_isIndeterminate)
                    g.DrawArc(arc, ring, _angle, _arcSweep);
                else if (_value > 0)
                    g.DrawArc(arc, ring, -90f, _value * 3.6f);
            }

            if (_showPercentage)
            {
                DrawProgressText(g, ring, t, _value.ToString());
            }
        }

        // What the spinner lies on: its own color if it has one, else the first color that is not see-through behind it.
        private Color GetSurface()
        {
            return SurfaceColor.Of(this);
        }

        // The faint full ring: the quiet line color of the surface (1.5:1 against it, in both themes).
        internal Color GetTrackColor()
        {
            return UIColors.GetLineColor(GetSurface());
        }

        // The arc as painted: a color set by hand as it is, the others moved just far enough to stand out from the track (3:1, as the
        // fill of the bars). A disabled spinner has the disabled look.
        private Color ResolveArcColor()
        {
            Color track = GetTrackColor();

            if (!Enabled)
                return UIColors.EnsureContrast(UIColors.GetDisabledText(GetSurface()), track, ProgressBarPainting.FillContrast);
            if (!_arcColor.IsEmpty)
                return _arcColor;
            if (_useStatusGradient)
                return ProgressBarPainting.GetStatusFill(_value, 0, 100, track);

            UIColorSet colors = Colors;

            return UIColors.EnsureContrast(colors != null ? colors.Hover : UIColors.DisabledGray, track, ProgressBarPainting.FillContrast);
        }

        // The color of the text (the digits, the text next to the ring): readable on what the spinner lies on.
        private Color GetTextColor()
        {
            Color surface = GetSurface();

            return Enabled ? ProgressBarPainting.GetTextColorOn(surface) : UIColors.GetDisabledText(surface);
        }

        // The digits sit inside the ring stroke; the widest they may get is the inner diameter. The size is worked out for "100"
        // whatever the value is, so the digits do not change their size while the number counts up; where that size is not legible
        // (a small ring), they are left out.
        private void DrawProgressText(Graphics g, RectangleF ring, float stroke, string text)
        {
            float inner = ring.Width - stroke * 2f - 2f;
            if (inner <= 3f)
                return;

            Color textColor = GetTextColor();

            float size = inner * 0.7f;
            for (int i = 0; i < 8; i++)
            {
                using (Font font = new Font(Font.FontFamily, size, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    SizeF measured = g.MeasureString("100", font);
                    if (measured.Width <= inner && measured.Height <= inner)
                    {
                        if (size < MinimumDigitSize)
                            return;

                        using (SolidBrush brush = new SolidBrush(textColor))
                        using (StringFormat sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        })
                        {
                            g.DrawString(text, font, brush, ring, sf);
                        }

                        return;
                    }
                }

                size *= 0.85f;
            }
        }

        protected override AccessibleObject CreateAccessibilityInstance()
        {
            return new SpinnerAccessibleObject(this);
        }

        // A screen reader asks for the value of a progress bar: the percentage when the load is known, nothing when it is not.
        private sealed class SpinnerAccessibleObject : ControlAccessibleObject
        {
            private readonly Spinner _spinner;

            public SpinnerAccessibleObject(Spinner spinner)
                : base(spinner)
            {
                _spinner = spinner;
            }

            public override AccessibleRole Role
            {
                get { return AccessibleRole.ProgressBar; }
            }

            public override string Name
            {
                get { return base.Name ?? (string.IsNullOrEmpty(_spinner.Text) ? null : _spinner.Text); }
                set { base.Name = value; }
            }

            public override string Value
            {
                get { return _spinner._isIndeterminate ? null : ProgressBarPainting.GetPercentText(0, 100, _spinner._value); }
                set { }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Tick -= OnTick;
                _timer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
