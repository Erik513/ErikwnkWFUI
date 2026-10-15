using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UICheckBoxFactory
    {
        public static CheckBox CreateStandard(
            string text = "",
            bool isChecked = true,
            UIColorSet colors = null)
        {
            return Create(text, isChecked, colors);
        }

        // The Standard one in the current accent - the primary color set.
        public static CheckBox CreatePrimary(
            string text = "",
            bool isChecked = true)
        {
            Controls.CheckBox checkBox = Create(text, isChecked, null);
            checkBox.FollowColors(() => UIColors.PrimarySet);

            return checkBox;
        }

        public static CheckBox CreateStandardCompact(
            bool isChecked = true,
            UIColorSet colors = null)
        {
            return CreateCompact(isChecked, colors);
        }

        // The compact one in the current accent - the primary color set.
        public static CheckBox CreatePrimaryCompact(
            bool isChecked = true)
        {
            Controls.CheckBox checkBox = CreateCompact(isChecked, null);
            checkBox.FollowColors(() => UIColors.PrimarySet);

            return checkBox;
        }

        private static Controls.CheckBox Create(string text, bool isChecked, UIColorSet colors)
        {
            text = text ?? "";
            Font font = UIFonts.Normal;

            Controls.CheckBox checkBox = new Controls.CheckBox
            {
                Text = text,
                Checked = isChecked,
                Font = font,
                FlatStyle = FlatStyle.Flat,
                AutoSize = false,
                Colors = colors
            };

            // Sized for the screen's scaling, which the control knows once it exists.
            checkBox.Size = Controls.CheckBox.ComputePreferredSize(text, font, checkBox.DeviceDpi);

            return checkBox;
        }

        // A box without a label: a square of 25 pixels at 96 dpi.
        private static Controls.CheckBox CreateCompact(bool isChecked, UIColorSet colors)
        {
            Controls.CheckBox checkBox = Create("", isChecked, colors);

            int side = Dpi.Scale(checkBox, 25);
            checkBox.Size = new Size(side, side);

            return checkBox;
        }
    }
}
