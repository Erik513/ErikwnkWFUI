using System;
using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UISlimProgressBarFactory
    {
        // Neutral gray fill by default; reassign ForeColor for any other
        // color. With a color set the bar is filled in the main color of the
        // set: its Hover color (see UIProgressBarFactory.MainColorOf). Matches the
        // CreateStandard = neutral gray convention used for buttons and sliders;
        // CreateGreen/CreatePrimary are CreateStandard with the green and the
        // accent set.
        public static SlimProgressBar CreateStandard(UIColorSet colors = null)
        {
            return Create(UIProgressBarFactory.MainColorOf(ColorSetSource.Of(colors)));
        }

        public static SlimProgressBar CreateGreen()
        {
            return CreateStandard(UIStyles.Colors.Sets.Green);
        }

        public static SlimProgressBar CreatePrimary()
        {
            return Create(UIProgressBarFactory.MainColorOf(() => UIColors.PrimarySet));
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

            return bar;
        }

        private static SlimProgressBar Create(Func<Color> foreColor)
        {
            SlimProgressBar bar = new SlimProgressBar
            {
                Minimum = 0,
                Maximum = 100
            };

            // The main color of the set; the bar moves it against its track (which follows the panel it lies on).
            bar.SetDefaultFill(foreColor);

            return bar;
        }
    }
}
