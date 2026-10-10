using System;
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
            return Create(() => UIColors.DisabledGray);
        }

        public static SlimProgressBar CreateGreen()
        {
            return Create(() => UIColors.Green);
        }

        public static SlimProgressBar CreatePrimary()
        {
            return Create(() => UIColors.Primary);
        }

        // Fill color follows Value instead of ForeColor - see
        // SlimProgressBar.UseStatusGradient.
        public static SlimProgressBar CreateStatus()
        {
            SlimProgressBar bar = new SlimProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                UseStatusGradient = true
            };

            ThemeWatcher.FollowBackColor(bar, () => UIColors.BackgroundMedium);

            return bar;
        }

        private static SlimProgressBar Create(Func<Color> foreColor)
        {
            SlimProgressBar bar = new SlimProgressBar
            {
                Minimum = 0,
                Maximum = 100
            };

            ThemeWatcher.FollowForeColor(bar, foreColor);
            ThemeWatcher.FollowBackColor(bar, () => UIColors.BackgroundMedium);

            return bar;
        }
    }
}
