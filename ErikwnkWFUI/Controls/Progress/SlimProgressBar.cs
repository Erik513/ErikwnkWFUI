using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A thin, fully custom-drawn progress bar - unlike <see cref="ProgressBar"/>,
    /// there is no native Win32 control underneath, so none of the
    /// theming/non-client-border quirks that control needs working around
    /// apply here. Meant for slim status-strip-style progress indicators
    /// (e.g. a background task's progress shown as a thin strip under a
    /// status label) rather than a prominent, full-size bar - see
    /// <see cref="UIStyles.ProgressBars"/> for that.
    /// It offers what the standard <see cref="ProgressBar"/> does: <see cref="Minimum"/>, <see cref="Maximum"/>, <see cref="Value"/>,
    /// <see cref="Step"/>, <see cref="PerformStep"/>, <see cref="Increment"/> and the <see cref="Style"/>s Blocks, Continuous (the default)
    /// and Marquee with its <see cref="MarqueeAnimationSpeed"/>. One difference: a <see cref="Value"/> outside the range is limited to
    /// it instead of throwing. Right-to-left layouts are not supported (<c>RightToLeft</c> is ignored).
    /// </summary>
    public class SlimProgressBar : Control
    {
        private const int DefaultHeight = 4;

        private readonly MarqueeAnimator _marquee;

        private int _minimum;
        private int _maximum = 100;
        private int _value;
        private int _step = 10;
        private int _marqueeAnimationSpeed = 100;
        private bool _useStatusGradient;
        private ProgressBarStyle _style = ProgressBarStyle.Continuous;

        public SlimProgressBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            DoubleBuffered = true;
            Height = DefaultHeight;

            // A screen reader announces it as a progress bar (the value itself is not reported).
            AccessibleRole = AccessibleRole.ProgressBar;

            _marquee = new MarqueeAnimator(this, () => _style == ProgressBarStyle.Marquee, () => _marqueeAnimationSpeed);

            // The disabled gray and the status colors are read while painting.
            ThemeWatcher.Attach(this, Invalidate);
        }

        public int Minimum
        {
            get { return _minimum; }
            set
            {
                _minimum = value;
                ClampValue();
                Invalidate();
            }
        }

        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                ClampValue();
                Invalidate();
            }
        }

        public int Value
        {
            get { return _value; }
            set
            {
                int clamped = Clamp(value, _minimum, _maximum);

                if (clamped == _value)
                    return;

                _value = clamped;
                Invalidate();
            }
        }

        /// <summary>How much <see cref="PerformStep"/> adds to <see cref="Value"/> (10 by default, as in the standard control).</summary>
        public int Step
        {
            get { return _step; }
            set { _step = value; }
        }

        /// <summary>
        /// Adds <see cref="Step"/> to <see cref="Value"/>, limited to <see cref="Minimum"/> and <see cref="Maximum"/>. Like the
        /// standard control, it throws while the <see cref="Style"/> is Marquee (there is no value then).
        /// </summary>
        public void PerformStep()
        {
            Increment(_step);
        }

        /// <summary>
        /// Adds <paramref name="value"/> (it can be negative) to <see cref="Value"/>, limited to <see cref="Minimum"/> and
        /// <see cref="Maximum"/>. Like the standard control, it throws while the <see cref="Style"/> is Marquee.
        /// </summary>
        public void Increment(int value)
        {
            if (_style == ProgressBarStyle.Marquee)
                throw new InvalidOperationException("The value of a progress bar in the Marquee style cannot be changed by steps.");

            Value = _value + value;
        }

        /// <summary>
        /// How the fill looks: <see cref="ProgressBarStyle.Continuous"/> (the default), <see cref="ProgressBarStyle.Blocks"/>, or
        /// <see cref="ProgressBarStyle.Marquee"/> - a block that runs through the bar again and again, for an action whose duration
        /// is not known (<see cref="Value"/> does not matter then).
        /// </summary>
        public ProgressBarStyle Style
        {
            get { return _style; }
            set
            {
                if (value == _style)
                    return;

                _style = value;
                _marquee.Update();
                Invalidate();
            }
        }

        /// <summary>The time in milliseconds between two steps of the marquee (100 by default); 0 stops it.</summary>
        public int MarqueeAnimationSpeed
        {
            get { return _marqueeAnimationSpeed; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _marqueeAnimationSpeed = value;
            }
        }

        /// <summary>
        /// When true, the fill color follows <see cref="Value"/> instead of
        /// <see cref="Control.ForeColor"/> - red at <see cref="Minimum"/>,
        /// yellow at the midpoint, green at <see cref="Maximum"/>, blending
        /// smoothly between them. For a "status/health" bar where the color
        /// itself communicates good/bad, not just the fill amount.
        /// </summary>
        public bool UseStatusGradient
        {
            get { return _useStatusGradient; }
            set
            {
                if (value == _useStatusGradient)
                    return;

                _useStatusGradient = value;
                Invalidate();
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _marquee.Update();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            _marquee.Update();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            using (SolidBrush trackBrush = new SolidBrush(BackColor))
                g.FillRectangle(trackBrush, ClientRectangle);

            Color fillColor;

            if (!Enabled)
                fillColor = UIColors.DisabledGray;
            else if (_useStatusGradient)
                fillColor = UIColors.GetStatusColor(_value, _minimum, _maximum);
            else
                fillColor = ForeColor;

            ProgressBarPainting.PaintFill(
                g,
                ClientRectangle,
                _style,
                ProgressBarPainting.GetFraction(_minimum, _maximum, _value),
                fillColor,
                _marquee.Offset);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _marquee.Dispose();

            base.Dispose(disposing);
        }

        private void ClampValue()
        {
            _value = Clamp(_value, _minimum, _maximum);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (max < min)
                max = min;

            return Math.Max(min, Math.Min(max, value));
        }
    }
}
