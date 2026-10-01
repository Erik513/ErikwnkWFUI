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
            bool isChecked = true)
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
                Size = Controls.CheckBox.ComputePreferredSize(text, font)
            };

            return checkBox;
        }

        public static CheckBox CreateCompact(
            bool isChecked = true)
        {
            CheckBox checkBox = CreateStandard(
                "",
                isChecked);

            checkBox.Size = new Size(25, 25);

            return checkBox;
        }
    }
}
