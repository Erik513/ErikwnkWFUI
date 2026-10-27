using System.Windows.Forms;
using ErikwnkWFUI.Controls;
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
            ThemedTextBox textBox = Create<ThemedTextBox>(text, placeholder, BorderStyle.FixedSingle);
            textBox.Colors = colors;

            return textBox;
        }

        // The set is asked for again each time: the primary set follows the accent.
        public static TextBox CreatePrimary(
            string text = "",
            string placeholder = "")
        {
            ThemedTextBox textBox = Create<ThemedTextBox>(text, placeholder, BorderStyle.FixedSingle);
            textBox.FollowColors(() => UIColors.PrimarySet);

            return textBox;
        }

        public static TextBox CreateBorderless(
            string text = "",
            string placeholder = "")
        {
            return Create<ThemedTextBox>(text, placeholder, BorderStyle.None);
        }

        // Locked (read-only) until the pencil in the box is used.
        public static LockableTextBox CreateLockable(
            string text = "",
            string placeholder = "",
            bool locked = true,
            UIColorSet colors = null)
        {
            LockableTextBox textBox = Create<LockableTextBox>(text, placeholder, BorderStyle.FixedSingle);
            textBox.Colors = colors;
            textBox.IsLocked = locked;

            return textBox;
        }

        // What is typed is hidden; the eye in the box shows it.
        public static TextBox CreatePassword(
            string text = "",
            string placeholder = "",
            UIColorSet colors = null)
        {
            PasswordTextBox textBox = Create<PasswordTextBox>(text, placeholder, BorderStyle.FixedSingle);
            textBox.Colors = colors;

            return textBox;
        }

        private static T Create<T>(string text, string placeholder, BorderStyle borderStyle) where T : ThemedTextBox, new()
        {
            T textBox = new T
            {
                Text = text ?? "",
                BorderStyle = borderStyle,
                Font = UIFonts.Normal
            };

            Follow(textBox);

            if (!string.IsNullOrWhiteSpace(placeholder))
                textBox.Placeholder = placeholder;

            return textBox;
        }

        // The fill and the text follow the theme as long as they still are the colors the factory gave.
        private static void Follow(ThemedTextBox textBox)
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
    }
}
