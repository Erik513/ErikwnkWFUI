using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIRadioButtonFactory
    {
        public static RadioButton CreateStandard(
            string text = "",
            bool isChecked = false,
            UIColorSet colors = null)
        {
            return Create(text, isChecked, colors);
        }

        // The Standard one in the current accent - the primary color set.
        public static RadioButton CreatePrimary(
            string text = "",
            bool isChecked = false)
        {
            Controls.RadioButton radioButton = Create(text, isChecked, null);
            radioButton.FollowColors(() => UIColors.PrimarySet);

            return radioButton;
        }

        public static RadioButton CreateStandardCompact(
            bool isChecked = false,
            UIColorSet colors = null)
        {
            return CreateCompact(isChecked, colors);
        }

        // The compact one in the current accent - the primary color set.
        public static RadioButton CreatePrimaryCompact(
            bool isChecked = false)
        {
            Controls.RadioButton radioButton = CreateCompact(isChecked, null);
            radioButton.FollowColors(() => UIColors.PrimarySet);

            return radioButton;
        }

        private static Controls.RadioButton Create(string text, bool isChecked, UIColorSet colors)
        {
            text = text ?? "";
            Font font = UIFonts.Normal;

            Controls.RadioButton radioButton = new Controls.RadioButton
            {
                Text = text,
                Font = font,
                FlatStyle = FlatStyle.Flat,
                AutoSize = false,
                Colors = colors
            };

            // Sized for the screen's scaling, which the control knows once it exists.
            radioButton.Size = Controls.RadioButton.ComputePreferredSize(text, font, radioButton.DeviceDpi);

            // Last: selecting one clears the others of its group, so the starting state is set once the button is complete.
            radioButton.Checked = isChecked;

            return radioButton;
        }

        // A button without a label: a square of 25 pixels at 96 dpi, as the compact check box.
        private static Controls.RadioButton CreateCompact(bool isChecked, UIColorSet colors)
        {
            Controls.RadioButton radioButton = Create("", isChecked, colors);

            int side = Dpi.Scale(radioButton, 25);
            radioButton.Size = new Size(side, side);

            return radioButton;
        }
    }
}
