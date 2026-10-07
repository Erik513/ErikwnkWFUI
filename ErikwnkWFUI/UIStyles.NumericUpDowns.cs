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
        /// <summary>A themed <see cref="NumericUpDown"/>. Note: the up/down spinner buttons themselves stay system-colored - only the field's own colors follow the theme.</summary>
        public static class NumericUpDowns
        {
            public static NumericUpDown CreateStandard(
                decimal minimum = 0,
                decimal maximum = 100,
                decimal increment = 1,
                decimal value = 0)
            {
                return UINumericUpDownFactory.CreateStandard(
                    minimum,
                    maximum,
                    increment,
                    value);
            }
        }
    }
}
