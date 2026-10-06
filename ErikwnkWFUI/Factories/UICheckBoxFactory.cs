using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
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
            text = text ?? "";
            Font font = UIFonts.Normal;

            CheckBox checkBox = new Controls.CheckBox
            {
                Text = text,
                Checked = isChecked,
                Font = font,
                FlatStyle = FlatStyle.Flat,
                AutoSize = false,
                Size = Controls.CheckBox.ComputePreferredSize(text, font),
                Colors = colors
            };

            return checkBox;
        }

        // The Standard one in the current accent - the primary color set.
        public static CheckBox CreatePrimary(
            string text = "",
            bool isChecked = true)
        {
            return CreateStandard(
                text,
                isChecked,
                UIColors.PrimarySet);
        }

        public static CheckBox CreateStandardCompact(
            bool isChecked = true,
            UIColorSet colors = null)
        {
            CheckBox checkBox = CreateStandard(
                "",
                isChecked,
                colors);

            checkBox.Size = new Size(25, 25);

            return checkBox;
        }

        // The compact one in the current accent - the primary color set.
        public static CheckBox CreatePrimaryCompact(
            bool isChecked = true)
        {
            return CreateStandardCompact(
                isChecked,
                UIColors.PrimarySet);
        }

    }
}
