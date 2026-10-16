using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The progress bar of <c>UIStyles.ProgressBars</c>. It is the standard <see cref="ProgressBar"/> (<c>Minimum</c>, <c>Maximum</c>,
    /// <c>Value</c>, <c>Step</c>, <c>PerformStep</c>, <c>Increment</c> and the <c>Style</c>s Blocks, Continuous and Marquee with its
    /// <c>MarqueeAnimationSpeed</c>), drawn by the library in its colors (the native control ignores them while visual styles are on),
    /// with a thin border (<see cref="BorderStyle"/>) and a track color of its own (<see cref="BackColor"/>, which may be transparent).
    /// The status variants fill in a color that follows the value from red over yellow to green. A <see cref="Text"/> and the
    /// percentage (<see cref="ShowPercentage"/>) can be written on the bar, each left, centered or right (<see cref="TextAlign"/>,
    /// <see cref="PercentageAlign"/>); a text that is too long is cut with an ellipsis or moves (<see cref="TextScroll"/>), and a
    /// tooltip shows it whole (<see cref="ShowFullTextToolTip"/>). Right-to-left layouts are not supported (<c>RightToLeft</c> is
    /// ignored). The factories hand it back as a <see cref="ProgressBar"/>.
    /// </summary>
    public class ProgressBar : System.Windows.Forms.ProgressBar
    {
        // The 1 px border keeps the extent of the bar legible even when the panel it lies on has the shade of the track (BackColor
        // alone does not guarantee a contrast); BorderStyle None leaves it out for a bar that is to blend into its panel.

        private readonly bool _useStatusGradient;
        private readonly ThemeColor _trackColor;
        private readonly MarqueeAnimator _marquee;
        private readonly ProgressTextScroller _textScroller;
        private readonly FullTextToolTip _fullTextToolTip;
        private bool _showFullTextToolTip = true;
        private BorderStyle _borderStyle = BorderStyle.FixedSingle;
        private bool _showPercentage;
        private HorizontalAlignment _textAlign = HorizontalAlignment.Left;
        private HorizontalAlignment _percentageAlign = HorizontalAlignment.Right;

        internal ProgressBar(bool useStatusGradient = false)
        {
            _useStatusGradient = useStatusGradient;

            // The native bar ignores ForeColor and BackColor while visual styles are on, so it is not used for painting at all.
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            _textScroller = new ProgressTextScroller(this);
            _fullTextToolTip = new FullTextToolTip(this);
            _marquee = new MarqueeAnimator(this, () => Style == ProgressBarStyle.Marquee, () => MarqueeAnimationSpeed);

            // The track: the recessed surface of the theme (until BackColor is set).
            _trackColor = new ThemeColor(() => UIColors.BackgroundMedium);
            ApplyTrack();
            ThemeWatcher.Attach(this, () =>
            {
                ApplyTrack();
                Invalidate();
            });
        }

        /// <summary>
        /// The color of the track. By default the recessed surface of the theme (it follows a change of the theme). Set a color for another
        /// one. <see cref="Color.Transparent"/> - which a <see cref="ProgressBar"/> cannot have itself - makes the bar look transparent: the
        /// track takes the color of the panel it lies on, also when that changes (and in both themes). <see cref="Color.Empty"/> gives the
        /// default back. Independent of <see cref="BorderStyle"/>.
        /// </summary>
        public override Color BackColor
        {
            get { return base.BackColor; }
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

                ApplyTrack();
            }
        }

        // What the bar lies on: the first color that is not see-through behind it.
        private Color GetParentSurface()
        {
            return Parent != null ? SurfaceColor.Of(Parent) : UIColors.BackgroundMedium;
        }

        // The native bar is given a color of its own always: Transparent would throw. Transparent means the one of the panel.
        private void ApplyTrack()
        {
            Color track = _trackColor.Value;

            if (track.A != 255)
                track = GetParentSurface();

            if (base.BackColor != track)
                base.BackColor = track;
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            ApplyTrack();
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            ApplyTrack();
        }

        /// <summary>
        /// The thin border around the bar: <see cref="BorderStyle.FixedSingle"/> (the default; <see cref="BorderStyle.Fixed3D"/> looks the
        /// same) or <see cref="BorderStyle.None"/>, where the bar has no frame of its own and its edge is the track.
        /// Only the border: the color of the track is <see cref="BackColor"/>, which can be transparent.
        /// </summary>
        public BorderStyle BorderStyle
        {
            get { return _borderStyle; }
            set
            {
                if (value == _borderStyle)
                    return;

                _borderStyle = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Whether the value is written on the bar as a percentage ("42 %"; off by default). With a <see cref="Text"/> the text stands
        /// on the left and the percentage on the right (see <see cref="TextAlign"/> and <see cref="PercentageAlign"/>). Not in the
        /// <see cref="ProgressBarStyle.Marquee"/> style, which has no value.
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
        /// Where the <see cref="Text"/> stands on the bar: left (the default), centered or right. At the same place as the percentage
        /// (<see cref="PercentageAlign"/>) both are one text, "Upload – 42 %"; at another place they are drawn apart, for
        /// instance the text left and the percentage right.
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
        /// Where the percentage stands on the bar: left, centered or right (the default); see <see cref="TextAlign"/>. It keeps the
        /// room of its widest form ("100 %") when it is drawn apart from the text.
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
        /// A text written on the bar (for a message about the state, like "Uploading ..."); empty by default. It is in the colors that
        /// can be read on the track and on the fill, on whatever fill level the bar has.
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
        /// <c>LabelScroll.Endless()</c> - the same values as for a label (speed, pause, direction, separator, pause while the mouse is on
        /// the bar). The percentage does not move: it stays where it is, and a text that stood together with it stands apart from it
        /// while it moves, the percentage at the right end. The text moves only while it does not fit, the bar is shown and enabled,
        /// and Windows lets windows animate (Settings, Accessibility). Several texts (<see cref="LabelScroll.Texts"/>) are not
        /// supported here: use <see cref="Text"/>.
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
            Invalidate();
        }

        // The color of the 1 px edge around the bar: the border - the quiet line of the library, as of a panel, moved only as far as the
        // panel the bar lies on needs to keep it seen (1.5:1) - or, without one, the track itself (what is seen when it is transparent).
        internal Color EdgeColor
        {
            get { return _borderStyle != BorderStyle.None ? UIColors.GetLineColor(GetParentSurface()) : BackColor; }
        }

        // The bar draws itself: the native control paints in WM_PAINT without the Paint event, ignores the colors while visual styles are
        // on, and shows nothing for the marquee style with them off. UserPaint makes WM_PAINT reach OnPaint (the native control still
        // asks for a repaint when the value changes: Value, Step, PerformStep and Increment need nothing here).
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle area = ClientRectangle;

            using (SolidBrush track = new SolidBrush(BackColor))
                g.FillRectangle(track, area);

            // The edge: the border, or - without one - the track itself.
            using (Pen edge = new Pen(EdgeColor))
                g.DrawRectangle(edge, 0, 0, area.Width - 1, area.Height - 1);

            Color fill;

            if (!Enabled)
                fill = UIColors.DisabledGray;
            else if (_useStatusGradient)
                fill = ProgressBarPainting.GetStatusFill(Value, Minimum, Maximum, BackColor);
            else
                fill = ForeColor;

            area.Inflate(-1, -1);
            Rectangle covered = ProgressBarPainting.PaintFill(g, area, Style, ProgressBarPainting.GetFraction(Minimum, Maximum, Value), fill, _marquee.Offset);

            bool percentage = _showPercentage && Style != ProgressBarStyle.Marquee;

            // The text keeps away from the edge of the bar.
            Rectangle textArea = area;
            textArea.Inflate(-GetTextPadding(), 0);

            bool cut = ProgressBarPainting.PaintTexts(
                g,
                textArea,
                Text,
                percentage ? ProgressBarPainting.GetPercentText(Minimum, Maximum, Value) : null,
                ProgressBarPainting.GetPercentText(0, 100, 100),
                _textAlign,
                _percentageAlign,
                Font,
                Enabled ? ProgressBarPainting.GetTextColorOn(BackColor) : UIColors.GetDisabledText(BackColor),
                ProgressBarPainting.GetTextColorOn(fill),
                covered,
                _textScroller);

            _fullTextToolTip.Set(_showFullTextToolTip && cut ? Text : "");
        }

        /// <summary>The room between the border of the bar and its text, in pixels at 96 dpi.</summary>
        internal const int TextPaddingAt96Dpi = 6;

        internal int GetTextPadding()
        {
            return (int)Math.Round(TextPaddingAt96Dpi * DeviceDpi / 96.0);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // The native bar has a sunken edge of its own around the client area (WS_EX_STATICEDGE, which the control gives itself
            // when it is created): the whole window is drawn by the library instead, so the edge is taken away and the border
            // of BorderStyle is the only one.
            int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);

            if ((exStyle & WS_EX_STATICEDGE) != 0)
            {
                SetWindowLong(Handle, GWL_EXSTYLE, exStyle & ~WS_EX_STATICEDGE);
                SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
            }

            // Changing the Style creates the handle again: the right moment to start or stop the marquee.
            _marquee.Update();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            _marquee.Update();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
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
    }
}
