using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UISlimProgressBarFactory
    {
        // Neutral grey fill by default; reassign ForeColor for any other
        // colour (CreateGreen/CreatePrimary just preset one). Matches the
        // CreateStandard = neutral grey convention used for buttons and sliders.
        public static SlimProgressBar CreateStandard()
        {
            return Create(UIColors.DisabledGray);
        }

        public static SlimProgressBar CreateGreen()
        {
            return Create(UIColors.Green);
        }

        public static SlimProgressBar CreatePrimary()
        {
            return Create(UIColors.Primary);
        }

        // Fill color follows Value instead of ForeColor - see
        // SlimProgressBar.UseStatusGradient.
        public static SlimProgressBar CreateStatus()
        {
            return new SlimProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                BackColor = UIColors.BackgroundMedium,
                UseStatusGradient = true
            };
        }

        private static SlimProgressBar Create(Color foreColor)
        {
            return new SlimProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                ForeColor = foreColor,
                BackColor = UIColors.BackgroundMedium
            };
        }
    }
}
