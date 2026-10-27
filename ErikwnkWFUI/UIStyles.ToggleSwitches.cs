using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>iOS-style on/off <see cref="Controls.ToggleSwitch"/>es in three sizes. Colors can be overridden per-instance - see <see cref="Controls.ToggleSwitch.CheckedBackColor"/>.</summary>
        public static class ToggleSwitches
        {
            public static ToggleSwitch CreateStandard(
                bool isChecked = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null)
            {
                return UIToggleSwitchFactory.CreateStandard(
                    isChecked,
                    tooltipChecked,
                    tooltipUnchecked);
            }

            public static ToggleSwitch CreateSmall(
                bool isChecked = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null)
            {
                return UIToggleSwitchFactory.CreateSmall(
                    isChecked,
                    tooltipChecked,
                    tooltipUnchecked);
            }

            public static ToggleSwitch CreateLarge(
                bool isChecked = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null)
            {
                return UIToggleSwitchFactory.CreateLarge(
                    isChecked,
                    tooltipChecked,
                    tooltipUnchecked);
            }
        }
    }
}
