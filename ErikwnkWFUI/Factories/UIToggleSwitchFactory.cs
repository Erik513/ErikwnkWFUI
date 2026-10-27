using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIToggleSwitchFactory
    {
        private static readonly Size DefaultSize = new Size(45, 25);

        public static ToggleSwitch CreateStandard(
            bool isChecked = true,
            string tooltipChecked = null,
            string tooltipUnchecked = null,
            UIColorSet colors = null,
            Size? size = null)
        {
            ToggleSwitch toggle = CreateToggleSwitch(
                isChecked,
                size ?? DefaultSize,
                tooltipChecked,
                tooltipUnchecked);
            toggle.Colors = colors;

            return toggle;
        }

        // The set is asked for again each time: the primary set follows the accent.
        public static ToggleSwitch CreatePrimary(
            bool isChecked = true,
            string tooltipChecked = null,
            string tooltipUnchecked = null,
            Size? size = null)
        {
            ToggleSwitch toggle = CreateToggleSwitch(
                isChecked,
                size ?? DefaultSize,
                tooltipChecked,
                tooltipUnchecked);
            toggle.FollowColors(() => UIColors.PrimarySet);

            return toggle;
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
