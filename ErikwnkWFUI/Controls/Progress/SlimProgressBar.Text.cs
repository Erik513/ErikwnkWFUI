using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // The text and the percentage next to the strip: the settings, the layout and the painting of them.
    public partial class SlimProgressBar
    {
        /// <summary>
        /// Whether the value is written as a percentage ("42%" in English, "42 %" in German; off by default), at the <see cref="TextPosition"/>. Above and below
        /// the bar it stands on the right, with the <see cref="Text"/> on the left; at the sides both are one text:
        /// "Upload – 42%". Not in the <see cref="ProgressBarStyle.Marquee"/> style, which has no value.
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
        /// place as the percentage (<see cref="PercentageAlign"/>) both are one text, "Upload – 42%"; at another place they are
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
            get { return _fullTextToolTip.Enabled; }
            set
            {
                _fullTextToolTip.Enabled = value;
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

        // The percentage is written the way of the language ("42%", "42 %"), which can change the width of the text.
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            if (InvokeRequired)
                BeginInvoke(new Action(Relayout));
            else
                Relayout();
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

        /// <summary>The size the bar needs: only the strip without a text, with one the line of the text above or below it, or its width at the sides.</summary>
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

        private bool PaintText(Graphics g, Rectangle textArea)
        {
            Color surface = GetSurface();
            Color textColor = !Enabled ? UIColors.GetDisabledText(surface) : _textColor.IsEmpty ? ProgressBarPainting.GetTextColorOn(surface) : _textColor;
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
    }
}
