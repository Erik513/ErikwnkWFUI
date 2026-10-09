using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UILabelFactory
    {
        public static ThemedLabel CreateTitle(string text = "")
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
                // disabled and the moving text are drawn that way too - whatever the application chose.
                UseCompatibleTextRendering = false
            };
        }

        public static ThemedLabel CreateStandard(string text = "", UIColorSet colors = null)
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
                // disabled and the moving text are drawn that way too - whatever the application chose.
                UseCompatibleTextRendering = false
            };
        }

        // The Standard one in the current accent - the primary color set.
        public static ThemedLabel CreatePrimary(string text = "")
        {
            return CreateStandard(text, UIColors.PrimarySet);
        }

        public static ThemedLabel CreateMuted(string text = "")
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
                // disabled and the moving text are drawn that way too - whatever the application chose.
                UseCompatibleTextRendering = false
            };
        }
    }
}
