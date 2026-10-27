using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>Themed <see cref="TextBox"/>es.</summary>
        public static class TextBoxes
        {
            /// <summary>A text box with a quiet frame that has the text color while the box has the focus, or the color of a color set.</summary>
            /// <param name="text">The text it starts with.</param>
            /// <param name="placeholder">A hint that is shown, in the muted color, while the box is empty and has not the focus (a text that is given stays).</param>
            /// <param name="colors">The color of the frame while the box has the focus: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>; its <c>Hover</c> color is the main color. Null (the default) is the text color. The fill and the text stay those of the theme.</param>
            public static TextBox CreateStandard(
                string text = "",
                string placeholder = "",
                UIColorSet colors = null)
            {
                return UITextBoxFactory.CreateStandard(
                    text,
                    placeholder,
                    colors);
            }

            /// <summary>Same, but the frame has the theme accent while the box has the focus (the other parameters as there).</summary>
            public static TextBox CreatePrimary(
                string text = "",
                string placeholder = "")
            {
                return UITextBoxFactory.CreatePrimary(
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
