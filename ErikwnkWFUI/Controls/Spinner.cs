using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// An indeterminate loading spinner: an accent-coloured arc rotating over a
    /// faint full ring. Owner-drawn, theme-aware, transparent background. Only
    /// animates while it is actually visible, so an off-screen or hidden one
    /// costs nothing. Set <see cref="Progress"/> to also show a load percentage
    /// in the centre.
    /// </summary>
    public class Spinner : Control
    {
        private const int TickMs = 16;              // ~60 fps
        private const float DegreesPerTick = 5f;    // full turn in ~1.2 s

        private readonly Timer _timer;
        private float _angle;
        private int _thickness;
        private float _arcSweep = 300f;
        private bool _accent;
        private Color? _arcColor;
        private int? _progress;

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
        /// True = the arc follows the live theme accent
        /// (<see cref="UIStyles.Colors.Primary"/>); false (the default) = a
        /// fixed neutral grey. Ignored when <see cref="ArcColor"/> is set.
        /// </summary>
        public bool Accent
        {
            get { return _accent; }
            set
            {
                _accent = value;
                Invalidate();
            }
        }

        /// <summary>An explicit arc colour, overriding <see cref="Accent"/>. Null (the default) uses the accent/grey rule.</summary>
        public Color? ArcColor
        {
            get { return _arcColor; }
            set
            {
                _arcColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// An optional load percentage drawn (bold) in the centre of the ring
        /// while the arc keeps spinning. Null (the default) shows nothing; a
        /// value is clamped to 0..100. The digits auto-scale to the ring, so a
        /// small spinner needs to be a few px larger than usual to stay legible.
        /// </summary>
        public int? Progress
        {
            get { return _progress; }
            set
            {
                if (value.HasValue)
                {
                    int v = value.Value;
                    if (v < 0)
                        v = 0;
                    if (v > 100)
                        v = 100;
                    value = v;
                }
                if (_progress == value)
                    return;
                _progress = value;
                Invalidate();
            }
        }

        /// <summary>How much of the circle the moving arc covers, in degrees (10..350, default 300).</summary>
        public float ArcSweep
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
            Color arcColor =
                !Enabled ? UIStyles.Colors.TextDisabled :
                _arcColor.HasValue ? _arcColor.Value :
                _accent ? UIStyles.Colors.Primary :
                UIColors.DisabledGray;

            using (Pen track = new Pen(trackColor, t))
            {
                g.DrawEllipse(track, ring);
            }
            using (Pen arc = new Pen(arcColor, t))
            {
                arc.StartCap = LineCap.Round;
                arc.EndCap = LineCap.Round;
                g.DrawArc(arc, ring, _angle, _arcSweep);
            }

            if (_progress.HasValue)
            {
                DrawProgressText(g, ring, t, _progress.Value.ToString());
            }
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
