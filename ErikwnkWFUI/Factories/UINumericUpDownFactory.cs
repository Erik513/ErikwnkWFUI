using System;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UINumericUpDownFactory
    {
        public static System.Windows.Forms.NumericUpDown CreateStandard(
            decimal minimum = 0,
            decimal maximum = 100,
            decimal increment = 1,
            decimal value = 0,
            UIColorSet colors = null)
        {
            Controls.NumericUpDown numericUpDown = Create(minimum, maximum, increment, value);
            numericUpDown.Colors = colors;

            return numericUpDown;
        }

        // The Standard one with the accent: the arrow fields in the primary color set.
        public static System.Windows.Forms.NumericUpDown CreatePrimary(
            decimal minimum = 0,
            decimal maximum = 100,
            decimal increment = 1,
            decimal value = 0)
        {
            Controls.NumericUpDown numericUpDown = Create(minimum, maximum, increment, value);
            numericUpDown.FollowColors(() => UIColors.PrimarySet);

            return numericUpDown;
        }

        // The border is the standard control's FixedSingle one, which the control paints over in the library's frame
        // (see Controls.NumericUpDown); the colors of the field are the one background of the input fields (dimmed when
        // disabled) and the text color, both followed live.
        private static Controls.NumericUpDown Create(decimal minimum, decimal maximum, decimal increment, decimal value)
        {
            Controls.NumericUpDown numericUpDown = new Controls.NumericUpDown
            {
                Minimum = minimum,
                Maximum = maximum,
                Increment = increment,
                Value = Math.Min(maximum, Math.Max(minimum, value)),
                Font = UIStyles.Fonts.Normal,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };

            Controls.InputField.FollowBackground(numericUpDown);
            Controls.ThemeWatcher.FollowForeColor(numericUpDown, () => UIStyles.Colors.TextPrimary);

            return numericUpDown;
        }
    }
}
