using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// A themed <see cref="System.Windows.Forms.NumericUpDown"/>: <see cref="CreateStandard"/> (neutral gray
        /// arrow fields, or those of a color set) and <see cref="CreatePrimary"/> (in the accent). The field takes
        /// the background of the theme (the same as the text box and the combo box, dimmed when disabled) and a frame;
        /// the two arrow fields are drawn by the library. The control also has a <c>Prefix</c> and a <c>Suffix</c> for
        /// a unit in the field: cast the result to <c>ErikwnkWFUI.Controls.NumericUpDown</c>.
        /// </summary>
        public static class NumericUpDowns
        {
            /// <summary>A themed numeric up/down with neutral gray arrow fields, or with those of a color set.</summary>
            /// <param name="minimum">The least value.</param>
            /// <param name="maximum">The greatest value.</param>
            /// <param name="increment">How much an arrow or the wheel changes the value.</param>
            /// <param name="value">The value it starts with.</param>
            /// <param name="colors">A color set for the frame and the arrow fields instead of the neutral gray (see <c>UIStyles.Colors.Sets</c>).</param>
            public static System.Windows.Forms.NumericUpDown CreateStandard(
                decimal minimum = 0,
                decimal maximum = 100,
                decimal increment = 1,
                decimal value = 0,
                UIColorSet colors = null)
            {
                return UINumericUpDownFactory.CreateStandard(
                    minimum,
                    maximum,
                    increment,
                    value,
                    colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but with the arrow fields in the current accent.</summary>
            public static System.Windows.Forms.NumericUpDown CreatePrimary(
                decimal minimum = 0,
                decimal maximum = 100,
                decimal increment = 1,
                decimal value = 0)
            {
                return UINumericUpDownFactory.CreatePrimary(
                    minimum,
                    maximum,
                    increment,
                    value);
            }
        }
    }
}
