using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// A themed <see cref="System.Windows.Forms.NumericUpDown"/>: <see cref="CreateStandard"/> (neutral gray
        /// arrow fields, or those of a color set) and <see cref="CreatePrimary"/> (in the accent). The field takes
        /// the colors of the theme; the two arrow fields are drawn by the library.
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
