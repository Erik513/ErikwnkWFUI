using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// Themed <see cref="RadioButton"/>s, drawn by the library: <see cref="CreateStandard"/> (neutral gray when selected) and
        /// <see cref="CreatePrimary"/> (the accent color), each also as a label-less <see cref="CreateStandardCompact"/> /
        /// <see cref="CreatePrimaryCompact"/>. A color set can be given to any of the standard ones. Hover, pressed, keyboard
        /// focus and disabled are built in. Radio buttons with the same parent form one group: put each group in its own panel.
        /// </summary>
        public static class RadioButtons
        {
            /// <param name="text">The label next to the button.</param>
            /// <param name="isChecked">Whether the button starts selected (the others of its group are cleared).</param>
            /// <param name="colors">
            /// The colors of the selected button (rest, hovered, pressed), for example one of <c>UIStyles.Colors.Sets</c>; left out,
            /// the neutral gray. The dot and the hover and pressed looks follow from the set.
            /// </param>
            public static System.Windows.Forms.RadioButton CreateStandard(
                string text = "",
                bool isChecked = false,
                UIColorSet colors = null)
            {
                return UIRadioButtonFactory.CreateStandard(text, isChecked, colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but with the button filled in the current accent instead of the neutral gray - mirrors <see cref="CheckBoxes.CreatePrimary"/>.</summary>
            public static System.Windows.Forms.RadioButton CreatePrimary(
                string text = "",
                bool isChecked = false)
            {
                return UIRadioButtonFactory.CreatePrimary(text, isChecked);
            }

            /// <summary>A button without a label, 25 x 25 (for table cells and the like).</summary>
            /// <inheritdoc cref="CreateStandard"/>
            public static System.Windows.Forms.RadioButton CreateStandardCompact(
                bool isChecked = false,
                UIColorSet colors = null)
            {
                return UIRadioButtonFactory.CreateStandardCompact(isChecked, colors);
            }

            /// <summary>Like <see cref="CreateStandardCompact"/> but in the current accent - the compact counterpart to <see cref="CreatePrimary"/>.</summary>
            public static System.Windows.Forms.RadioButton CreatePrimaryCompact(
                bool isChecked = false)
            {
                return UIRadioButtonFactory.CreatePrimaryCompact(isChecked);
            }
        }
    }
}
