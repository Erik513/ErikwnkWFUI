using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UILabelFactory
    {
        public static Label CreateTitle(string text = "")
        {
            return new ThemedLabel(UIColors.TextPrimary, null)
            {
                Text = text ?? "",
                Font = UIFonts.Title,
                BackColor = UIColors.BackgroundDark,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                AutoSize = false,
                UseMnemonic = false,
                // The text is drawn with GDI, as in every other control of the library - the
                // disabled label below draws it that way too - whatever the application chose.
                UseCompatibleTextRendering = false
            };
        }

        public static Label CreateStandard(string text = "", UIColorSet colors = null)
        {
            return new ThemedLabel(UIColors.TextSecondary, colors)
            {
                Text = text ?? "",
                Font = UIFonts.Normal,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false,
                // The text is drawn with GDI, as in every other control of the library - the
                // disabled label below draws it that way too - whatever the application chose.
                UseCompatibleTextRendering = false
            };
        }

        // The Standard one in the current accent - the primary color set.
        public static Label CreatePrimary(string text = "")
        {
            return CreateStandard(text, UIColors.PrimarySet);
        }

        public static Label CreateMuted(string text = "")
        {
            return new ThemedLabel(UIColors.TextMuted, null)
            {
                Text = text ?? "",
                Font = UIFonts.Small,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                UseMnemonic = false,
                // The text is drawn with GDI, as in every other control of the library - the
                // disabled label below draws it that way too - whatever the application chose.
                UseCompatibleTextRendering = false
            };
        }

        // The standard label does the painting - text, alignment, padding, ellipsis, mnemonic,
        // border, transparent background - with the ForeColor this class answers. Only one thing
        // it does not let us decide: a disabled label ignores its ForeColor and takes a fixed
        // system gray (set ForeColor to pure red, the disabled text is still that gray). So a
        // disabled label alone is painted here, in UIColors.DisabledGray, the one gray every
        // other control uses for "disabled", so that it reads the same in every control and
        // both themes.
        private sealed class ThemedLabel : Label
        {
            // The least contrast the text of a label with a color set has against what is behind it.
            private const double TextContrast = 4.5;

            private readonly Color _enabledForeColor;
            private readonly UIColorSet _colors;
            private bool _hasOwnForeColor;

            public ThemedLabel(Color enabledForeColor, UIColorSet colors)
            {
                _enabledForeColor = enabledForeColor;
                _colors = colors;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (Enabled)
                {
                    base.OnPaint(e);

                    return;
                }

                // The background is already painted (the label's own, or the page's behind a
                // transparent one); what is left is the text, as the standard label would lay it out.
                Rectangle textArea = new Rectangle(
                    Padding.Left,
                    Padding.Top,
                    Width - Padding.Horizontal,
                    Height - Padding.Vertical);

                TextRenderer.DrawText(e.Graphics, Text, Font, textArea, GetTextColor(), GetTextFormatFlags());
            }

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

            // Disabled, always the one gray of the library, whatever the ForeColor.
            internal Color GetTextColor()
            {
                return Enabled ? GetEnabledTextColor() : UIColors.DisabledGray;
            }

            // A ForeColor that was set wins. With a color set the text takes the main color of
            // the set (the one its controls show hovered), lightened or darkened where the page
            // behind it needs that to read; the page is looked at when the label is painted, so
            // a theme change is followed.
            private Color GetEnabledTextColor()
            {
                // (Before the constructor has run, the base class may already ask.)
                if (_hasOwnForeColor || _enabledForeColor.IsEmpty)
                    return base.ForeColor;

                if (_colors == null)
                    return _enabledForeColor;

                return UIColors.EnsureContrast(_colors.Hover, GetEffectiveBackColor(), TextContrast);
            }

            // How the disabled text is laid out, as the standard label does it: the alignment, what
            // the text does at the end of a line, and what an ampersand is - a literal one, or,
            // with UseMnemonic, the sign for the key that reaches the next control (underlined
            // only while the keyboard cues are shown).
            internal TextFormatFlags GetTextFormatFlags()
            {
                TextFormatFlags flags = ToTextFormatFlags(TextAlign);

                if (!UseMnemonic)
                    flags |= TextFormatFlags.NoPrefix;
                else if (!ShowKeyboardCues)
                    flags |= TextFormatFlags.HidePrefix;

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

            private static TextFormatFlags ToTextFormatFlags(ContentAlignment alignment)
            {
                TextFormatFlags horizontal;
                TextFormatFlags vertical;

                switch (alignment)
                {
                    case ContentAlignment.TopLeft:
                    case ContentAlignment.MiddleLeft:
                    case ContentAlignment.BottomLeft:
                        horizontal = TextFormatFlags.Left;
                        break;
                    case ContentAlignment.TopCenter:
                    case ContentAlignment.MiddleCenter:
                    case ContentAlignment.BottomCenter:
                        horizontal = TextFormatFlags.HorizontalCenter;
                        break;
                    default:
                        horizontal = TextFormatFlags.Right;
                        break;
                }

                switch (alignment)
                {
                    case ContentAlignment.TopLeft:
                    case ContentAlignment.TopCenter:
                    case ContentAlignment.TopRight:
                        vertical = TextFormatFlags.Top;
                        break;
                    case ContentAlignment.BottomLeft:
                    case ContentAlignment.BottomCenter:
                    case ContentAlignment.BottomRight:
                        vertical = TextFormatFlags.Bottom;
                        break;
                    default:
                        vertical = TextFormatFlags.VerticalCenter;
                        break;
                }

                return horizontal | vertical;
            }
        }
    }
}
