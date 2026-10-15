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
                bool checkedState = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null)
            {
                return UIToggleSwitchFactory.CreateStandard(
                    checkedState,
                    tooltipChecked,
                    tooltipUnchecked);
            }

            public static ToggleSwitch CreateSmall(
                bool checkedState = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null)
            {
                return UIToggleSwitchFactory.CreateSmall(
                    checkedState,
                    tooltipChecked,
                    tooltipUnchecked);
            }

            public static ToggleSwitch CreateLarge(
                bool checkedState = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null)
            {
                return UIToggleSwitchFactory.CreateLarge(
                    checkedState,
                    tooltipChecked,
                    tooltipUnchecked);
            }
        }
    }
}
