using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>Where the text of a <see cref="SlimProgressBar"/> is placed in relation to the bar.</summary>
    public enum SlimProgressBarTextPosition
    {
        /// <summary>Above the bar (the default).</summary>
        Above,

        /// <summary>Below the bar.</summary>
        Below,

        /// <summary>To the left of the bar.</summary>
        Left,

        /// <summary>To the right of the bar.</summary>
        Right
    }

    /// <summary>
    /// A thin, fully custom-drawn progress bar for slim status-strip-style indicators (a background task's progress as a thin
    /// strip under a status label), see <c>UIStyles.SlimProgressBars</c>; <c>UIStyles.ProgressBars</c> is the prominent, full-size one.
    /// It is a plain control, with no native progress bar underneath. Every color can be chosen on its own: the fill
    /// (<see cref="ForeColor"/>), the track (<see cref="BackColor"/>) and the text (<see cref="TextColor"/>).
    /// It offers what the standard <see cref="ProgressBar"/> does: <see cref="Minimum"/>, <see cref="Maximum"/>, <see cref="Value"/>,
    /// <see cref="Step"/>, <see cref="PerformStep"/>, <see cref="Increment"/> and the <see cref="Style"/>s Blocks, Continuous (the default)
    /// and Marquee with its <see cref="MarqueeAnimationSpeed"/>. One difference: a <see cref="Value"/> outside the range is limited to
    /// it instead of throwing, and a range may contain negative numbers (the standard control refuses a negative <see cref="Minimum"/>).
    /// Right-to-left layouts are not supported (<c>RightToLeft</c> is ignored).
    /// A <see cref="Text"/> (a message about the state) and the <see cref="ShowPercentage"/> are written next to the strip, at the
    /// <see cref="TextPosition"/>; the strip itself stays as thin as it is. A text that is too long is cut with an ellipsis or moves
    /// (<see cref="TextScroll"/>), and a tooltip shows it whole (<see cref="ShowFullTextToolTip"/>).
    /// </summary>
    // In files by task: this one (the range, the value, the styles, the clock, the painting of the strip), SlimProgressBar.Colors.cs
    // (track, fill and text colors), SlimProgressBar.Text.cs (the text and the percentage next to the strip: settings, layout, painting)
    // and SlimProgressBar.Accessibility.cs (what a screen reader is told).
    public partial class SlimProgressBar : Control
    {
        private const int DefaultHeight = 4;

        // The strip keeps this height when there is a text next to it; the rest of the control is the text.
        private const int StripHeight = DefaultHeight;
        private const int TextGap = 2;
        private const int SideGap = 8;
        private const int MinimumSideStripWidth = 40;

        // The track follows the panel the bar lies on (see BackColor), the fill the factory's color (see ForeColor) until one is set.
        private readonly ThemeColor _trackColor;
        private readonly ThemeColor _fillColor;
        private Color _textColor = Color.Empty;

        private readonly MarqueeAnimator _marquee;
        private readonly ProgressTextScroller _textScroller;
        private readonly FullTextToolTip _fullTextToolTip;

        private int _minimum;
        private int _maximum = 100;
        private int _value;
        private int _step = 10;
        private int _marqueeAnimationSpeed = 100;
        private bool _useStatusGradient;
        private bool _showPercentage;
        private HorizontalAlignment _textAlign = HorizontalAlignment.Left;
        private HorizontalAlignment _percentageAlign = HorizontalAlignment.Right;
        private SlimProgressBarTextPosition _textPosition = SlimProgressBarTextPosition.Above;
        private ProgressBarStyle _style = ProgressBarStyle.Continuous;

        public SlimProgressBar()
        {
            SetStyle(ControlStyleSets.OwnerDrawn, true);

            Height = DefaultHeight;

            // A screen reader announces it as a progress bar (the value itself is not reported).
            AccessibleRole = AccessibleRole.ProgressBar;

            _trackColor = new ThemeColor(() => UIColors.GetLineColor(GetSurface()));
            _fillColor = new ThemeColor(() => base.ForeColor);
            _textScroller = new ProgressTextScroller(this);
            UIStrings.LanguageChanged += OnLanguageChanged;
            _fullTextToolTip = new FullTextToolTip(this);
            _marquee = new MarqueeAnimator(this, () => _style == ProgressBarStyle.Marquee, () => _marqueeAnimationSpeed);

            // The disabled gray and the status colors are read while painting.
            ThemeWatcher.Attach(this, Invalidate);
        }

        /// <summary>The lower end of the range (0 by default); it may be negative. A <see cref="Value"/> below it is raised to it.</summary>
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

        /// <summary>The upper end of the range (100 by default). A <see cref="Value"/> above it is lowered to it.</summary>
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

        /// <summary>The current value. One outside the range is limited to it instead of throwing (the standard control throws).</summary>
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

            // (In a long: a step of int.MaxValue must reach the maximum, not wrap around to a negative number.)
            Value = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, (long)_value + value));
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
                Relayout();
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

            // (A bar that is disabled gets no MouseLeave: the mouse is not "on" it any more.)
            _textScroller.SetHovered(false);
            Invalidate();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            Invalidate();
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
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

            // (A bar that is hidden gets no MouseLeave either.)
            _textScroller.SetHovered(false);
            _marquee.Update();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            Rectangle strip;
            Rectangle textArea;
            GetLayout(out strip, out textArea);

            // With a text the strip does not fill the control: the rest is the surface the control lies on.
            if (HasText)
            {
                using (SolidBrush surfaceBrush = new SolidBrush(GetSurface()))
                    g.FillRectangle(surfaceBrush, ClientRectangle);
            }

            Color track = BackColor;

            using (SolidBrush trackBrush = new SolidBrush(track))
                g.FillRectangle(trackBrush, strip);

            Color fillColor = GetFillColor(track);

            ProgressBarPainting.PaintFill(
                g,
                strip,
                _style,
                ProgressBarPainting.GetFraction(_minimum, _maximum, _value),
                fillColor,
                _marquee.Offset);

            bool cut = false;

            if (HasText)
                cut = PaintText(g, textArea);
            else
                _textScroller.Configure(0, 0, Font);

            _fullTextToolTip.Update(Text, cut);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIStrings.LanguageChanged -= OnLanguageChanged;
                _marquee.Dispose();
                _textScroller.Dispose();
                _fullTextToolTip.Dispose();
            }

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
