using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>iOS-style on/off <see cref="Controls.ToggleSwitch"/>es: <c>CreateStandard</c> is gray while it is on, <c>CreatePrimary</c> follows the accent, and <c>colors:</c> takes the colors of a set.</summary>
        public static class ToggleSwitches
        {
            /// <summary>A switch that is gray while it is on, or in the colors of a color set.</summary>
            /// <param name="isChecked">Whether it starts on.</param>
            /// <param name="tooltipChecked">The tooltip while it is on (none when left out).</param>
            /// <param name="tooltipUnchecked">The tooltip while it is off (none when left out).</param>
            /// <param name="colors">A color set for the switch while it is on: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="Color"/>; its <c>Hover</c> color is the main color. Null (the default) is gray.</param>
            /// <param name="size">The size of the switch; 45 x 25 when left out (35 x 20 is small, 55 x 30 large).</param>
            public static ToggleSwitch CreateStandard(
                bool isChecked = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null,
                UIColorSet colors = null,
                Size? size = null)
            {
                return UIToggleSwitchFactory.CreateStandard(
                    isChecked,
                    tooltipChecked,
                    tooltipUnchecked,
                    colors,
                    size);
            }

            /// <summary>Same, but the switch follows the theme accent while it is on (the other parameters as there).</summary>
            public static ToggleSwitch CreatePrimary(
                bool isChecked = true,
                string tooltipChecked = null,
                string tooltipUnchecked = null,
                Size? size = null)
            {
                return UIToggleSwitchFactory.CreatePrimary(
                    isChecked,
                    tooltipChecked,
                    tooltipUnchecked,
                    size);
            }
        }
    }
}
