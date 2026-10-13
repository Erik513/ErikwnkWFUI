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
