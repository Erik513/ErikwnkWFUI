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

        // The fill and the text follow the theme as long as they still are the colors the factory gave. The text is
        // the main color, or the muted one while the placeholder is shown.
        private static void Follow(TextBox textBox)
        {
            System.Drawing.Color primary = UIColors.TextPrimary;
            System.Drawing.Color muted = UIColors.TextMuted;
            textBox.ForeColor = primary;

            Controls.InputField.FollowBackground(textBox);

            // Scroll bars of a multi-line box: the system's dark ones on a dark theme.
            Helpers.NativeControlTheme.Track(textBox);

            Controls.ThemeWatcher.Attach(textBox, () =>
            {
                System.Drawing.Color current = textBox.ForeColor;

                if (current == primary)
                    textBox.ForeColor = UIColors.TextPrimary;
                else if (current == muted)
                    textBox.ForeColor = UIColors.TextMuted;

                primary = UIColors.TextPrimary;
                muted = UIColors.TextMuted;
            });
        }

        private static void SetPlaceholder(
            TextBox textBox,
            string placeholder)
        {
            if (textBox == null)
                return;

            string placeholderText = placeholder ?? "";

            // A text that was given stays; the placeholder stands in only where there is none.
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = placeholderText;
                textBox.ForeColor = UIColors.TextMuted;
            }

            textBox.GotFocus += delegate
            {
                if (textBox.Text != placeholderText)
                    return;

                textBox.Text = "";
                textBox.ForeColor = UIColors.TextPrimary;
            };

            textBox.LostFocus += delegate
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                    return;

                textBox.Text = placeholderText;
                textBox.ForeColor = UIColors.TextMuted;
            };
        }
    }
}
