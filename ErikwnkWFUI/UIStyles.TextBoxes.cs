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

            public static TextBox CreateBorderstyleNone(
                string text = "",
                string placeholder = "")
            {
                return UITextBoxFactory.CreateBorderstyleNone(
                    text,
                    placeholder);
            }
        }
    }
}
