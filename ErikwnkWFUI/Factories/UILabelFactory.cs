using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UILabelFactory
    {
        private const AnchorStyles AllSides = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        public static ThemedLabel CreateTitle(string text = "", UIColorSet colors = null, LabelScroll scroll = null)
        {
            return Create(text, () => UIColors.TextPrimary, ColorSetSource.Of(colors), UIFonts.Title, ContentAlignment.MiddleCenter, false, scroll);
        }

        public static ThemedLabel CreateStandard(string text = "", UIColorSet colors = null, LabelScroll scroll = null)
        {
            return Standard(text, ColorSetSource.Of(colors), scroll);
        }

        // The Standard one in the current accent - the primary color set.
        public static ThemedLabel CreatePrimary(string text = "", LabelScroll scroll = null)
        {
            return Standard(text, () => UIColors.PrimarySet, scroll);
        }

        public static ThemedLabel CreateMuted(string text = "", UIColorSet colors = null, LabelScroll scroll = null)
        {
            return Create(text, () => UIColors.TextMuted, ColorSetSource.Of(colors), UIFonts.Small, ContentAlignment.MiddleLeft, true, scroll);
        }

        private static ThemedLabel Standard(string text, Func<UIColorSet> colors, LabelScroll scroll)
        {
            return Create(text, () => UIColors.TextSecondary, colors, UIFonts.Normal, ContentAlignment.MiddleLeft, false, scroll);
        }

        // What the kinds have in common; they differ in the color of their kind, the font, the alignment
        // and whether they are the muted kind.
        private static ThemedLabel Create(
            string text,
            Func<Color> enabledForeColor,
            Func<UIColorSet> colors,
            Font font,
            ContentAlignment textAlign,
            bool muted,
            LabelScroll scroll)
        {
            ThemedLabel label = new ThemedLabel(enabledForeColor, colors, muted)
            {
                Text = text ?? "",
                Font = font,
                BackColor = Color.Transparent,
                TextAlign = textAlign,
                AutoEllipsis = true,
                AutoSize = false,
                Anchor = AllSides,
                UseMnemonic = false,
                // The text is drawn with GDI, as in every other control of the library - the
                // disabled and the moving text are drawn that way too - whatever the application chose.
                UseCompatibleTextRendering = false
            };

            // Last: the movement needs the text, the font and the size to be there.
            label.Scroll = scroll;

            return label;
        }
    }
}
