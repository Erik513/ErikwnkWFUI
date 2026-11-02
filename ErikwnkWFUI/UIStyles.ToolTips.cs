using System.Windows.Forms;
using ErikwnkWFUI.Factories;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>Creates a plain <see cref="ToolTip"/> component - note it isn't owned by any control, so dispose it yourself if you're not letting it live for the app's whole lifetime.</summary>
        public static class ToolTips
        {
            /// <summary>A tooltip in the colors of the theme (they follow a change of it; a color set by hand stays).</summary>
            /// <param name="text">The title of the tooltip (empty: none).</param>
            public static ToolTip CreateToolTip(
                string text = "")
            {
                return UIToolTipFactory.CreateToolTip(text);
            }
        }
    }
}
