using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UITextBoxFactory
    {
        public static TextBox CreateStandard(
            string text = "",
            string placeholder = "",
            UIColorSet colors = null)
        {
            Controls.TextBox textBox = Create(text, BorderStyle.FixedSingle);
            textBox.Colors = colors;

            if (!string.IsNullOrWhiteSpace(placeholder))
                SetPlaceholder(textBox, placeholder);

            return textBox;
        }

        // The set is asked for again each time: the primary set follows the accent.
        public static TextBox CreatePrimary(
            string text = "",
            string placeholder = "")
        {
            Controls.TextBox textBox = Create(text, BorderStyle.FixedSingle);
            textBox.FollowColors(() => UIColors.PrimarySet);

            if (!string.IsNullOrWhiteSpace(placeholder))
                SetPlaceholder(textBox, placeholder);

            return textBox;
        }

        private static Controls.TextBox Create(string text, BorderStyle borderStyle)
        {
            Controls.TextBox textBox = new Controls.TextBox
            {
                Text = text ?? "",
                BorderStyle = borderStyle,
                Font = UIFonts.Normal
            };

            Follow(textBox);

            return textBox;
        }

        public static TextBox CreateBorderless(
            string text = "",
            string placeholder = "")
        {
            Controls.TextBox textBox = Create(text, BorderStyle.None);

            if (!string.IsNullOrWhiteSpace(placeholder))
                SetPlaceholder(textBox, placeholder);

            return textBox;
        }

        // The fill and the text follow the theme as long as they still are the colors the factory gave.
        private static void Follow(Controls.TextBox textBox)
        {
            System.Drawing.Color primary = UIColors.TextPrimary;
            textBox.ForeColor = primary;

            Controls.InputField.FollowBackground(textBox);

            // Scroll bars of a multi-line box: the system's dark ones on a dark theme.
            Helpers.NativeControlTheme.Track(textBox);

            Controls.ThemeWatcher.Attach(textBox, () =>
            {
                if (textBox.ForeColor == primary)
                    textBox.ForeColor = UIColors.TextPrimary;

                primary = UIColors.TextPrimary;
            });
        }

        // The hint is painted by the box while it is empty and has not the focus (it is not its Text).
        private static void SetPlaceholder(
            Controls.TextBox textBox,
            string placeholder)
        {
            if (textBox != null)
                textBox.Placeholder = placeholder;
        }
    }
}
