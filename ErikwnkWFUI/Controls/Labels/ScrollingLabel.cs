using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The label of the library, made by <c>UIStyles.Labels</c>: the standard <see cref="Label"/> in the
    /// colors of the library, with a text that can move when it is too long for the label: back and forth
    /// or endlessly (<see cref="ScrollMode"/>, <see cref="Scroll"/>).
    /// </summary>
    /// <remarks>
    /// Who paints what: a label that is enabled and whose text stands still is painted by the standard label -
    /// text, alignment, padding, ellipsis, mnemonic, border, image, transparent background - with the
    /// <see cref="ForeColor"/> this class answers (a color set, or one that was set, or the color of its kind).
    /// A <b>disabled</b> label is painted here: the standard one ignores its ForeColor when disabled and takes a
    /// fixed system gray, so the text is drawn in <c>UIColors.DisabledGray</c>, the one gray every other control
    /// uses for "disabled", and reads the same in every control and both themes. The <b>moving</b> text is
    /// painted here as well, from a picture of its own that can be put between two pixels (see
    /// <see cref="ScrollMode"/>); the picture is made and moved on the beat of <c>AnimationClock</c>.
    /// </remarks>
    public sealed partial class ScrollingLabel : Label
    {
        // ---- the constants ----

        // The least contrast the text of a label has against what is behind it (a color that reads already stays).
        private const double TextContrast = 4.5;

        // The muted label takes the color of its set dimmed: this share of the way toward the page behind it,
        // and never below this contrast (it is meant to be quiet, not unreadable). A disabled label is held to
        // the same floor: WCAG does not ask for more, but the text has to be seen.
        private const double MutedBlend = 0.35;
        private const double MutedContrast = 3.0;
        private const double DisabledContrast = 3.0;

        // ---- the colors ----

        // The color of its kind, the color set (or none), whether it is the muted kind, whether a ForeColor was set.
        // (Read each time, so a change of the theme or of the accent is followed.)
        private readonly Func<Color> _enabledForeColor;
        private readonly Func<UIColorSet> _colors;
        private readonly bool _muted;
        private bool _hasOwnForeColor;

        internal ScrollingLabel(Func<Color> enabledForeColor, Func<UIColorSet> colors, bool muted = false)
        {
            _enabledForeColor = enabledForeColor;
            _colors = colors;
            _muted = muted;
            _fullTextToolTip = new Helpers.FullTextToolTip(this);

            // The text color is worked out while painting: a change of the theme only needs a repaint.
            ThemeWatcher.Attach(this, Invalidate);
        }

        // ---- the text color ----

        // Like the standard label: a ForeColor that was set is the text color; set back to
        // empty, the label has its own color again. Read without one, it says the color the
        // text has when the label is enabled.
        public override Color ForeColor
        {
            get { return GetEnabledTextColor(); }
            set
            {
                _hasOwnForeColor = !value.IsEmpty;
                base.ForeColor = value;
                Invalidate();
            }
        }

        // Disabled, always the one gray of the library, whatever the ForeColor - moved lighter or darker
        // only where the page behind it would leave it unseen.
        internal Color GetTextColor()
        {
            return Enabled
                ? GetEnabledTextColor()
                : UIColors.EnsureContrast(UIColors.DisabledGray, GetEffectiveBackColor(), DisabledContrast);
        }

        // A ForeColor that was set wins. Without one the text has the color of its kind (or of its set: the
        // main color of the set, the one its controls show hovered), lightened or darkened where the page
        // behind it needs that to read; the page is looked at when the label is painted, so a theme
        // change, or a label on a surface of another shade, is followed. A color that reads already stays.
        private Color GetEnabledTextColor()
        {
            // (Before the constructor has run, the base class may already ask.)
            if (_hasOwnForeColor || _enabledForeColor == null)
                return base.ForeColor;

            Color back = GetEffectiveBackColor();

            UIColorSet colors = _colors?.Invoke();

            if (colors == null)
                return UIColors.EnsureContrast(_enabledForeColor(), back, _muted ? MutedContrast : TextContrast);

            Color color = UIColors.EnsureContrast(colors.Hover, back, TextContrast);

            // A muted label is the same color, but quiet.
            if (_muted)
                color = UIColors.EnsureContrast(UIColors.BlendToward(color, back, MutedBlend), back, MutedContrast);

            return color;
        }

        // What the text is read against: the color of the label, or - for a transparent one - of the first
        // ancestor that has one, or the medium background of the theme where there is none.
        private Color GetEffectiveBackColor()
        {
            if (BackColor.A > 0)
                return BackColor;

            for (Control ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
            {
                if (ancestor.BackColor.A > 0)
                    return ancestor.BackColor;
            }

            return UIColors.BackgroundMedium;
        }

        // ---- painting ----

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!Enabled)
            {
                // The background is already painted (the label's own, or the page's behind a
                // transparent one); what is left is the text, as the standard label would lay it out - inside the border, if it has one.
                Rectangle textArea = new Rectangle(
                    Padding.Left,
                    Padding.Top,
                    ClientSize.Width - Padding.Horizontal,
                    ClientSize.Height - Padding.Vertical);

                TextRenderer.DrawText(e.Graphics, Text, Font, textArea, GetTextColor(), GetTextFormatFlags());

                return;
            }

            if (_maxOffset > 0 || _cycle > 0)
            {
                PaintMovingText(e.Graphics);

                return;
            }

            base.OnPaint(e);
        }

        // ---- how the disabled text is laid out ----

        // What an ampersand is: a literal one, or - with UseMnemonic - the sign for the key that
        // reaches the next control (underlined only while the keyboard cues are shown).
        private TextFormatFlags GetPrefixFlags()
        {
            if (!UseMnemonic)
                return TextFormatFlags.NoPrefix;

            return ShowKeyboardCues ? TextFormatFlags.Default : TextFormatFlags.HidePrefix;
        }

        // How the disabled text is laid out, as the standard label does it: the alignment, what
        // the text does at the end of a line, and what an ampersand is.
        internal TextFormatFlags GetTextFormatFlags()
        {
            TextFormatFlags flags = GetHorizontalFlag(TextAlign) | GetVerticalFlag(TextAlign) | GetPrefixFlags();

            // Like the standard label: a fixed-size label either cuts
            // its text with an ellipsis or wraps it. An auto-sized one
            // is exactly as big as its text - nothing to cut - and only
            // wraps when it has a maximum width to wrap at.
            if (AutoSize)
            {
                if (MaximumSize.Width > 0)
                    flags |= TextFormatFlags.WordBreak;
            }
            else
            {
                flags |= AutoEllipsis ? TextFormatFlags.EndEllipsis : TextFormatFlags.WordBreak;
            }

            return flags;
        }

        private static TextFormatFlags GetHorizontalFlag(ContentAlignment alignment)
        {
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.BottomLeft:
                    return TextFormatFlags.Left;
                case ContentAlignment.TopCenter:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.BottomCenter:
                    return TextFormatFlags.HorizontalCenter;
                default:
                    return TextFormatFlags.Right;
            }
        }

        private static TextFormatFlags GetVerticalFlag(ContentAlignment alignment)
        {
            switch (alignment)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.TopCenter:
                case ContentAlignment.TopRight:
                    return TextFormatFlags.Top;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomCenter:
                case ContentAlignment.BottomRight:
                    return TextFormatFlags.Bottom;
                default:
                    return TextFormatFlags.VerticalCenter;
            }
        }
    }
}
