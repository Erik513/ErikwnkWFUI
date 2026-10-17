using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// An indeterminate loading spinner: a coloured arc rotating over a faint
    /// full ring. Owner-drawn, theme-aware, transparent background. Only animates
    /// while it is actually visible, so an off-screen or hidden one costs
    /// nothing. It turns while <see cref="IsSpinning"/> and the control is enabled and Windows lets windows animate; with
    /// <see cref="IsIndeterminate"/> off it is a ring that fills up to the <see cref="Value"/> and stands still. Set <see cref="ShowPercentage"/> to also show the <see cref="Value"/> (a load
    /// percentage) in the centre. The colour comes from <see cref="ArcColor"/>, the main colour of
    /// <see cref="Colors"/> (a colour set) or the neutral gray - or, with <see cref="UseStatusGradient"/>,
    /// from the <see cref="Value"/>. The <see cref="UIStyles.Spinners"/> factories preset
    /// these (a colour set, the accent, and with a percentage the red→yellow→green status colour).
    /// </summary>
    public partial class Spinner : Control
    {
        private const int TickMs = 16;              // ~60 fps
        private const int HiddenPollMs = 250;      // how often a spinner on a hidden window looks whether it is shown again
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

        // Windows is asked again for its animation setting after this many ticks (~0.5 s), not on every one.
        private const int AskEveryTicks = 32;

        // Same two-segment red->yellow->green blend as SlimProgressBar, and the
        // same 0.65 breakpoint (green as "done" should only take over near the
        // top of the range). Duplicated per the codebase's per-control colour
        // helper pattern rather than shared.
        private const double YellowBreakpoint = 0.65;

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

            // The track, the text and the gray arc are read while painting.
            ThemeWatcher.Attach(this, Invalidate);
        }

        /// <summary>Stroke width in pixels. 0 (the default) scales it to the control's size.</summary>
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
        /// The colours of the arc: its main colour is the <c>Hover</c> colour of the set (Green is the usual green, the primary set the
        /// accent). Null (the default) is the neutral gray; <c>UIStyles.Spinners.CreatePrimary</c> makes one that follows the accent.
        /// <see cref="ArcColor"/> and <see cref="UseStatusGradient"/> win over it.
        /// </summary>
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

        // A set that is asked for again each time (the primary set, which follows the accent).
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorsSource = source;
            _colors = null;
            Invalidate();
        }

        /// <summary>An explicit arc colour, overriding <see cref="Colors"/> and <see cref="UseStatusGradient"/>. <see cref="Color.Empty"/> (the default) uses those rules.</summary>
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
        /// True = the arc colour communicates the load state - a solid colour
        /// blended red (0) → yellow → green (100) by <see cref="Value"/>.
        /// Ignored when <see cref="ArcColor"/> is set. Mirrors
        /// <see cref="Controls.SlimProgressBar.UseStatusGradient"/>.
        /// </summary>
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
        /// The load, as a percentage from 0 to 100 (a value outside is limited to it; 0 by default). It colours the arc with
        /// <see cref="UseStatusGradient"/> and is written in the centre with <see cref="ShowPercentage"/>.
        /// </summary>
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
        /// Whether the <see cref="Value"/> is drawn (bold, as a number) in the centre of the ring while the arc keeps spinning (off by
        /// default). The digits scale to the ring, so a small spinner needs to be a few px larger than usual to stay legible.
        /// </summary>
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
        /// Whether the arc turns (true by default). False stands it still where it is, e.g. while nothing is loading. It also stands
        /// still while the control is disabled or while Windows has the animations switched off.
        /// </summary>
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

            Color trackColor = UIStyles.Colors.BorderMedium;

            using (Pen track = new Pen(trackColor, t))
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

        private Color ResolveArcColor()
        {
            if (!Enabled)
                return UIStyles.Colors.TextDisabled;
            if (!_arcColor.IsEmpty)
                return _arcColor;
            if (_useStatusGradient)
                return StatusColor(_value / 100f);

            UIColorSet colors = Colors;

            return colors != null ? colors.Hover : UIColors.DisabledGray;
        }

        private static Color StatusColor(float fraction)
        {
            if (fraction < 0f)
                fraction = 0f;
            if (fraction > 1f)
                fraction = 1f;

            if (fraction <= YellowBreakpoint)
                return Lerp(UIColors.Red, UIColors.Yellow, fraction / (float)YellowBreakpoint);

            return Lerp(UIColors.Yellow, UIColors.Green,
                (fraction - (float)YellowBreakpoint) / (float)(1 - YellowBreakpoint));
        }

        private static Color Lerp(Color from, Color to, float amount)
        {
            if (amount < 0f)
                amount = 0f;
            if (amount > 1f)
                amount = 1f;

            return Color.FromArgb(
                (int)Math.Round(from.R + (to.R - from.R) * amount),
                (int)Math.Round(from.G + (to.G - from.G) * amount),
                (int)Math.Round(from.B + (to.B - from.B) * amount));
        }

        // The digits sit inside the ring stroke; the widest they may get is the inner diameter. The size is worked out for "100"
        // whatever the value is, so the digits do not change their size while the number counts up; where that size is not legible
        // (a small ring), they are left out.
        private void DrawProgressText(Graphics g, RectangleF ring, float stroke, string text)
        {
            float inner = ring.Width - stroke * 2f - 2f;
            if (inner <= 3f)
                return;

            Color textColor = Enabled ? UIStyles.Colors.TextPrimary : UIStyles.Colors.TextDisabled;

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
