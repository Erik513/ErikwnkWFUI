using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// Themed <see cref="GroupBox"/>es, drawn by the library: a thin rounded frame with the title in a gap of its upper edge.
        /// <see cref="CreateStandard"/> has the quiet line of the theme (or the color of a set), <see cref="CreatePrimary"/> a frame in the
        /// current accent. The surface is the one of the parent; the children are added as to any group box.
        /// </summary>
        public static class GroupBoxes
        {
            /// <param name="text">The title in the upper edge.</param>
            /// <param name="colors">
            /// The color of the frame, for example one of <c>UIStyles.Colors.Sets</c> (its hover color, moved until it is seen against the
            /// surface); left out, the quiet line of the theme.
            /// </param>
            public static System.Windows.Forms.GroupBox CreateStandard(
                string text = "",
                UIColorSet colors = null)
            {
                return UIGroupBoxFactory.CreateStandard(text, colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but with the frame in the current accent - mirrors <see cref="CheckBoxes.CreatePrimary"/>.</summary>
            public static System.Windows.Forms.GroupBox CreatePrimary(
                string text = "")
            {
                return UIGroupBoxFactory.CreatePrimary(text);
            }
        }
    }
}
