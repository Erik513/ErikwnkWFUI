using System;
using System.Windows.Forms;
using static ErikwnkWFUI.UIStyles;

namespace ErikwnkWFUI.Factories
{
    internal static class UINumericUpDownFactory
    {
        public static NumericUpDown CreateStandard(
            decimal minimum = 0,
            decimal maximum = 100,
            decimal increment = 1,
            decimal value = 0)
        {
            NumericUpDown numericUpDown = new NumericUpDown
            {
                Minimum = minimum,
                Maximum = maximum,
                Increment = increment,
                Value = Math.Min(maximum, Math.Max(minimum, value)),
                Font = Fonts.Normal,
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Center
            };

            Controls.ThemeWatcher.FollowBackColor(numericUpDown, () => Colors.BackgroundLight);
            Controls.ThemeWatcher.FollowForeColor(numericUpDown, () => Colors.TextPrimary);

            return numericUpDown;
        }
    }
}