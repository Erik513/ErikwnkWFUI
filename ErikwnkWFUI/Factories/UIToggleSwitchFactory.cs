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
            string tooltipUnchecked = null,
            UIColorSet colors = null,
            Size? size = null)
        {
            ToggleSwitch toggle = CreateToggleSwitch(
                isChecked,
                size,
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
                size,
                tooltipChecked,
                tooltipUnchecked);
            toggle.FollowColors(() => UIColors.PrimarySet);

            return toggle;
        }

        private static ToggleSwitch CreateToggleSwitch(
            bool isChecked,
            Size? size,
            string tooltipChecked,
            string tooltipUnchecked)
        {
            ToggleSwitch toggle = new ToggleSwitch
            {
                Checked = isChecked,
                ToolTipTextChecked = tooltipChecked,
                ToolTipTextUnchecked = tooltipUnchecked,
                BackColor = UIColors.Transparent
            };

            // Without a size the switch has its own: 45 x 25 at 96 dpi, as big as that on the screen it is on.
            if (size.HasValue)
                toggle.Size = size.Value;

            return toggle;
        }
    }
}
