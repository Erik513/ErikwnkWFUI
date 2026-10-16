using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
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
    /// It is a plain control, with no native progress bar underneath.
    /// It offers what the standard <see cref="ProgressBar"/> does: <see cref="Minimum"/>, <see cref="Maximum"/>, <see cref="Value"/>,
    /// <see cref="Step"/>, <see cref="PerformStep"/>, <see cref="Increment"/> and the <see cref="Style"/>s Blocks, Continuous (the default)
    /// and Marquee with its <see cref="MarqueeAnimationSpeed"/>. One difference: a <see cref="Value"/> outside the range is limited to
    /// it instead of throwing, and a range may contain negative numbers (the standard control refuses a negative <see cref="Minimum"/>).
    /// Right-to-left layouts are not supported (<c>RightToLeft</c> is ignored).
    /// A <see cref="Text"/> (a message about the state) and the <see cref="ShowPercentage"/> are written next to the strip, at the
    /// <see cref="TextPosition"/>; the strip itself stays as thin as it is. A text that is too long is cut with an ellipsis or moves
    /// (<see cref="TextScroll"/>), and a tooltip shows it whole (<see cref="ShowFullTextToolTip"/>).
    /// </summary>
    public class SlimProgressBar : Control
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

        private readonly MarqueeAnimator _marquee;
        private readonly ProgressTextScroller _textScroller;
        private readonly FullTextToolTip _fullTextToolTip;
        private bool _showFullTextToolTip = true;

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

            _trackColor = new ThemeColor(() => UIColors.GetLineColor(GetSurface()));
            _fillColor = new ThemeColor(() => base.ForeColor);
            _textScroller = new ProgressTextScroller(this);
            _fullTextToolTip = new FullTextToolTip(this);
            _marquee = new MarqueeAnimator(this, () => _style == ProgressBarStyle.Marquee, () => _marqueeAnimationSpeed);

            // The disabled gray and the status colors are read while painting.
            ThemeWatcher.Attach(this, Invalidate);
        }

        /// <summary>
        /// The color of the track. By default the quiet line of the library for the panel the bar lies on (as the frame of a panel):
        /// the gray of the theme, moved only as far as the panel needs to keep the track seen (1.5:1) - the track has no border of its own
        /// here. It follows the panel and the theme. Set a color for another one, <see cref="Color.Empty"/> gives the default back; a
        /// transparent color is the one of the panel.
        /// </summary>
        public override Color BackColor
        {
            get
            {
                if (_trackColor == null)
                    return base.BackColor;

                Color track = _trackColor.Value;

                return track.A != 255 ? GetSurface() : track;
            }
            set
            {
                if (_trackColor == null)
                {
                    base.BackColor = value;

                    return;
                }

                if (value.IsEmpty)
                    _trackColor.Reset();
                else
                    _trackColor.Set(value);

                Invalidate();
            }
        }

        /// <summary>
        /// The color of the fill. The bars of <c>UIStyles.SlimProgressBars</c> have the main color of their color set (or the gray) until
        /// one is set; that color is moved only as far as needed to stand out from the track (3:1), a color set here is used as it is.
        /// <see cref="Color.Empty"/> gives the default back.
        /// </summary>
        public override Color ForeColor
        {
            get { return _fillColor == null ? base.ForeColor : _fillColor.Value; }
            set
            {
                if (_fillColor == null)
                {
                    base.ForeColor = value;

                    return;
                }

                if (value.IsEmpty)
                    _fillColor.Reset();
                else
                    _fillColor.Set(value);

                Invalidate();
            }
        }

        // The color the factory gives the fill; followed live (the theme, the accent) until a color is set by hand.
        internal void SetDefaultFill(Func<Color> color)
        {
            _fillColor.SetDefault(color);
            Invalidate();
        }

        // What the bar lies on: the first color that is not see-through behind it.
        private Color GetSurface()
        {
            return Parent != null ? SurfaceColor.Of(Parent) : UIColors.BackgroundMedium;
        }

        // The fill as painted: a color set by hand as it is, the others moved just far enough to stand out from the track.
        internal Color GetFillColor(Color track)
        {
            if (!Enabled)
                return UIColors.EnsureContrast(UIColors.DisabledGray, track, ProgressBarPainting.FillContrast);

            if (_useStatusGradient)
                return ProgressBarPainting.GetStatusFill(_value, _minimum, _maximum, track);

            return _fillColor.IsSet ? _fillColor.Value : UIColors.EnsureContrast(_fillColor.Value, track, ProgressBarPainting.FillContrast);
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

        /// <summary>
        /// Whether the value is written as a percentage ("42 %"; off by default), at the <see cref="TextPosition"/>. Above and below
        /// the bar it stands on the right, with the <see cref="Text"/> on the left; at the sides both are one text:
        /// "Upload – 42 %". Not in the <see cref="ProgressBarStyle.Marquee"/> style, which has no value.
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
                Relayout();
            }
        }

        /// <summary>
        /// Where the <see cref="Text"/> stands in its line above or below the strip: left (the default), centered or right. At the same
        /// place as the percentage (<see cref="PercentageAlign"/>) both are one text, "Upload – 42 %"; at another place they are
        /// drawn apart (by default the text on the left and the percentage on the right). At the sides of the strip there is one
        /// text that has just its room, so neither of the two alignments does anything there.
        /// </summary>
        [DefaultValue(HorizontalAlignment.Left)]
        public HorizontalAlignment TextAlign
        {
            get { return _textAlign; }
            set
            {
                if (value == _textAlign)
                    return;

                _textAlign = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Where the percentage stands in its line above or below the strip: right (the default), centered or left; see
        /// <see cref="TextAlign"/>. It keeps the room of its widest form ("100 %") when it is drawn apart from the text.
        /// </summary>
        [DefaultValue(HorizontalAlignment.Right)]
        public HorizontalAlignment PercentageAlign
        {
            get { return _percentageAlign; }
            set
            {
                if (value == _percentageAlign)
                    return;

                _percentageAlign = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Where the <see cref="Text"/> and the percentage are placed: <see cref="SlimProgressBarTextPosition.Above"/> (the default),
        /// below, left or right of the bar.
        /// </summary>
        [DefaultValue(SlimProgressBarTextPosition.Above)]
        public SlimProgressBarTextPosition TextPosition
        {
            get { return _textPosition; }
            set
            {
                if (value == _textPosition)
                    return;

                _textPosition = value;
                Relayout();
            }
        }

        /// <summary>
        /// A text (for a message about the state, like "Uploading ..."); empty by default. Without a text and without the percentage the
        /// control is only the strip, as thin as it was made. With one, its preferred size (for <c>AutoSize</c>) takes the text line,
        /// or the width of the text at the sides; a size that was set stays.
        /// </summary>
        [Browsable(true), EditorBrowsable(EditorBrowsableState.Always), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        /// <summary>
        /// How a <see cref="Text"/> that is too long for its place moves instead of ending in an ellipsis: <see cref="LabelScroll.None"/>
        /// (the default: it is cut with "..."), <c>LabelScroll.BackAndForth()</c> (to its end, a rest, back) or
        /// <c>LabelScroll.Endless()</c>, the same values as for a label (speed, pause, direction, separator, pause while the mouse is on
        /// the bar). The percentage does not move: it stays where it is, and a text that stood together with it stands apart from it
        /// while it moves, the percentage at the right end. The text moves only while it does not fit, while the bar is shown and enabled, and while Windows lets windows
        /// animate (Settings, Accessibility); several texts (<see cref="LabelScroll.Texts"/>) are not supported here, use <see cref="Text"/>.
        /// </summary>
        public LabelScroll TextScroll
        {
            get { return _textScroller.Scroll; }
            set { _textScroller.Scroll = value; }
        }

        /// <summary>
        /// Whether a tooltip shows the whole <see cref="Text"/> while the mouse is on the bar and the text is cut with an ellipsis or moves
        /// (on by default); a text that is shown whole has none.
        /// </summary>
        [DefaultValue(true)]
        public bool ShowFullTextToolTip
        {
            get { return _showFullTextToolTip; }
            set
            {
                if (value == _showFullTextToolTip)
                    return;

                _showFullTextToolTip = value;

                if (!value)
                    _fullTextToolTip.Set("");

                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _textScroller.SetHovered(true);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _textScroller.SetHovered(false);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Relayout();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            Relayout();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

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
            _marquee.Update();
        }

        // The text of the percentage is not shown in the marquee style, which has no value.
        private bool ShowsPercentage
        {
            get { return _showPercentage && _style != ProgressBarStyle.Marquee; }
        }

        private bool HasText
        {
            get { return !string.IsNullOrEmpty(Text) || ShowsPercentage; }
        }

        // The widest the percentage can be, so the strip does not change its length while the value grows.
        private string WidestPercent
        {
            get { return ProgressBarPainting.GetPercentText(0, 100, 100); }
        }

        private bool IsSide
        {
            get { return _textPosition == SlimProgressBarTextPosition.Left || _textPosition == SlimProgressBarTextPosition.Right; }
        }

        private void Relayout()
        {
            if (AutoSize)
                Size = GetPreferredSize(Size.Empty);

            Invalidate();
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            if (!HasText)
                return new Size(Width, DefaultHeight);

            Size line = ProgressBarPainting.MeasureText(ProgressBarPainting.Compose(Text, ShowsPercentage, WidestPercent), Font);

            if (IsSide)
                return new Size(line.Width + SideGap + MinimumSideStripWidth, Math.Max(line.Height, StripHeight));

            return new Size(Width, line.Height + TextGap + StripHeight);
        }

        // The strip and the room of the text, in the client area. Without a text the strip is all of it.
        private void GetLayout(out Rectangle strip, out Rectangle textArea)
        {
            Rectangle client = ClientRectangle;

            if (!HasText)
            {
                strip = client;
                textArea = Rectangle.Empty;

                return;
            }

            Size line = ProgressBarPainting.MeasureText(ProgressBarPainting.Compose(Text, ShowsPercentage, WidestPercent), Font);

            if (IsSide)
            {
                int textWidth = Math.Min(line.Width, Math.Max(0, client.Width - SideGap - MinimumSideStripWidth));
                int stripY = client.Y + (client.Height - StripHeight) / 2;

                if (_textPosition == SlimProgressBarTextPosition.Left)
                {
                    textArea = new Rectangle(client.X, client.Y, textWidth, client.Height);
                    strip = new Rectangle(client.X + textWidth + SideGap, stripY, Math.Max(0, client.Width - textWidth - SideGap), StripHeight);
                }
                else
                {
                    textArea = new Rectangle(client.Right - textWidth, client.Y, textWidth, client.Height);
                    strip = new Rectangle(client.X, stripY, Math.Max(0, client.Width - textWidth - SideGap), StripHeight);
                }

                return;
            }

            // Above or below: the text line and the strip together, in the middle of the control.
            int groupHeight = line.Height + TextGap + StripHeight;
            int top = client.Y + Math.Max(0, (client.Height - groupHeight) / 2);

            if (_textPosition == SlimProgressBarTextPosition.Above)
            {
                textArea = new Rectangle(client.X, top, client.Width, line.Height);
                strip = new Rectangle(client.X, top + line.Height + TextGap, client.Width, StripHeight);
            }
            else
            {
                strip = new Rectangle(client.X, top, client.Width, StripHeight);
                textArea = new Rectangle(client.X, top + StripHeight + TextGap, client.Width, line.Height);
            }
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

            _fullTextToolTip.Set(_showFullTextToolTip && cut ? Text : "");
        }

        private bool PaintText(Graphics g, Rectangle textArea)
        {
            Color surface = GetSurface();
            Color textColor = Enabled ? ProgressBarPainting.GetTextColorOn(surface) : UIColors.GetDisabledText(surface);
            string percent = ProgressBarPainting.GetPercentText(_minimum, _maximum, _value);

            if (IsSide)
            {
                // One text (the percentage with it); a text that moves leaves the percentage where it is, at the end of the room.
                HorizontalAlignment side = _textPosition == SlimProgressBarTextPosition.Left ? HorizontalAlignment.Right : HorizontalAlignment.Left;

                return ProgressBarPainting.PaintTexts(
                    g,
                    textArea,
                    Text,
                    ShowsPercentage ? percent : null,
                    WidestPercent,
                    side,
                    side,
                    Font,
                    textColor,
                    textColor,
                    Rectangle.Empty,
                    _textScroller);
            }

            // Above or below: each where its alignment says (by default the text on the left, the percentage on the right).
            return ProgressBarPainting.PaintTexts(
                g,
                textArea,
                Text,
                ShowsPercentage ? percent : null,
                WidestPercent,
                _textAlign,
                _percentageAlign,
                Font,
                textColor,
                textColor,
                Rectangle.Empty,
                _textScroller);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
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
