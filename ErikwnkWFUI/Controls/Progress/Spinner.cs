using System;
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
    /// nothing. Set <see cref="ShowPercentage"/> to also show the <see cref="Value"/> (a load
    /// percentage) in the centre. The colour comes from <see cref="ArcColor"/>, the main colour of
    /// <see cref="Colors"/> (a colour set) or the neutral gray - or, with <see cref="UseStatusGradient"/>,
    /// from the <see cref="Value"/>. The <see cref="UIStyles.Spinners"/> factories preset
    /// these (a colour set, the accent, and with a percentage the red→yellow→green status colour).
    /// </summary>
    public class Spinner : Control
    {
        private const int TickMs = 16;              // ~60 fps
        private const float DegreesPerTick = 5f;    // full turn in ~1.2 s

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
            _angle = (_angle + DegreesPerTick) % 360f;
            Invalidate();
        }

        private void UpdateRunning()
        {
            bool run = Visible && IsHandleCreated && !DesignMode;
            if (run && !_timer.Enabled)
                _timer.Start();
            else if (!run && _timer.Enabled)
                _timer.Stop();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateRunning();
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

            float t = EffectiveThickness;
            float inset = t / 2f + 1f;

            // Always a circle, centred - a non-square control (e.g. stretched by
            // a table cell) draws the ring in the largest square that fits, not
            // an ellipse.
            float d = Math.Min(Width, Height);
            RectangleF ring = new RectangleF(
                (Width - d) / 2f + inset,
                (Height - d) / 2f + inset,
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
                g.DrawArc(arc, ring, _angle, _arcSweep);
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

        // The digits sit inside the ring stroke; the widest they may get is the
        // inner diameter, so the font is measured down until "100" fits.
        private void DrawProgressText(Graphics g, RectangleF ring, float stroke, string text)
        {
            float inner = ring.Width - stroke * 2f - 2f;
            if (inner <= 3f)
                return;

            Color textColor = Enabled ? UIStyles.Colors.TextPrimary : UIStyles.Colors.TextDisabled;

            float size = inner * 0.7f;
            for (int i = 0; i < 8; i++)
            {
                Font font = new Font(Font.FontFamily, size, FontStyle.Bold, GraphicsUnit.Pixel);
                SizeF measured = g.MeasureString(text, font);
                bool fits = measured.Width <= inner && measured.Height <= inner;
                if (fits || size <= 4f)
                {
                    using (font)
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
                font.Dispose();
                size *= 0.85f;
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
