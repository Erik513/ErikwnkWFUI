using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

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
        private const float ArcSweepDegrees = 300f; // how much of the circle the turning arc covers

        private int _thickness;
        private int _value;
        private bool _showPercentage;
        private bool _isIndeterminate = true;
        private bool _hideWhenStopped;

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

            InitializeClock();

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

        private int EffectiveThickness
        {
            get
            {
                return _thickness > 0
                    ? _thickness
                    : Math.Max(2, Math.Min(Width, Height) / 8);
            }
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
                    g.DrawArc(arc, ring, _angle, ArcSweepDegrees);
                else if (_value > 0)
                    g.DrawArc(arc, ring, -90f, _value * 3.6f);
            }

            if (_showPercentage)
            {
                DrawProgressText(g, ring, t, _value.ToString());
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeClock();
                _digitFont?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
