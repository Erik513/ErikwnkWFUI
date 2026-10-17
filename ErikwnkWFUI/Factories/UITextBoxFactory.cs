using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UITextBoxFactory
    {
        public static TextBox CreateStandard(
            string text = "",
            string placeholder = "")
        {
            TextBox textBox = new TextBox
            {
                Text = text ?? "",
                BorderStyle = BorderStyle.FixedSingle,
                Font = UIFonts.Normal
            };

            Follow(textBox);
            Controls.InputField.FrameTextBox(textBox);

            if (!string.IsNullOrWhiteSpace(placeholder))
                SetPlaceholder(textBox, placeholder);

            return textBox;
        }

        public static TextBox CreateBorderstyleNone(
            string text = "",
            string placeholder = "")
        {
            TextBox textBox = new TextBox
            {
                Text = text ?? "",
                BorderStyle = BorderStyle.None,
                Font = UIFonts.Normal
            };

            Follow(textBox);

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

            textBox.Text = placeholderText;
            textBox.ForeColor = UIColors.TextMuted;

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
