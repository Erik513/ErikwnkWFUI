using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIToolTipFactory
    {
        public static ToolTip CreateToolTip(
            string text = "")
        {
            return new ToolTip
            {
                ToolTipTitle = text ?? "",
                BackColor = UIColors.BackgroundMedium,
                ForeColor = UIColors.TextPrimary,
                AutoPopDelay = 10000,
                InitialDelay = 1000,
                ReshowDelay = 300,
                UseAnimation = true,
                UseFading = true
            };
        }

        // The plain tooltip a control shows for itself on hover: standard
        // look, short delay, and kept working after the window loses and
        // regains focus.
        public static ToolTip CreateHoverToolTip(Control owner)
        {
            ToolTip toolTip = new ToolTip
            {
                InitialDelay = 500,
                ReshowDelay = 100,
                AutoPopDelay = 5000
            };

            toolTip.ReviveOnFormActivate(owner);
            return toolTip;
        }
    }
}