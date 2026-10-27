using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIToggleSwitchFactory
    {
        public static ToggleSwitch CreateStandard(
            bool isChecked = true,
            string tooltipChecked = null,
            string tooltipUnchecked = null)
        {
            return CreateToggleSwitch(
                isChecked,
                new Size(45, 25),
                tooltipChecked,
                tooltipUnchecked);
        }

        public static ToggleSwitch CreateSmall(
            bool isChecked = true,
            string tooltipChecked = null,
            string tooltipUnchecked = null)
        {
            return CreateToggleSwitch(
                isChecked,
                new Size(35, 20),
                tooltipChecked,
                tooltipUnchecked);
        }

        public static ToggleSwitch CreateLarge(
            bool isChecked = true,
            string tooltipChecked = null,
            string tooltipUnchecked = null)
        {
            return CreateToggleSwitch(
                isChecked,
                new Size(55, 30),
                tooltipChecked,
                tooltipUnchecked);
        }

        private static ToggleSwitch CreateToggleSwitch(
            bool isChecked,
            Size size,
            string tooltipChecked,
            string tooltipUnchecked)
        {
            return new ToggleSwitch
            {
                Checked = isChecked,
                Size = size,
                ToolTipTextChecked = tooltipChecked,
                ToolTipTextUnchecked = tooltipUnchecked,
                BackColor = UIColors.Transparent
            };
        }
    }
}
