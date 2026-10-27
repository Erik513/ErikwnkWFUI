using System.Windows.Forms;
using ErikwnkWFUI.Factories;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>Themed <see cref="TextBox"/>es.</summary>
        public static class TextBoxes
        {
            public static TextBox CreateStandard(
                string text = "",
                string placeholder = "")
            {
                return UITextBoxFactory.CreateStandard(
                    text,
                    placeholder);
            }

            /// <summary>A text box without a frame (the fill and the text follow the theme as on <see cref="CreateStandard"/>).</summary>
            public static TextBox CreateBorderless(
                string text = "",
                string placeholder = "")
            {
                return UITextBoxFactory.CreateBorderless(
                    text,
                    placeholder);
            }

            /// <summary>The old name of <see cref="CreateBorderless"/>; it is removed once the projects that use it have been changed.</summary>
            [System.Obsolete("Use CreateBorderless (the same text box, a name that says what it is).")]
            public static TextBox CreateBorderstyleNone(
                string text = "",
                string placeholder = "")
            {
                return CreateBorderless(
                    text,
                    placeholder);
            }
        }
    }
}
