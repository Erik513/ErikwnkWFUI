using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// A thin, fully custom-drawn <see cref="SlimProgressBar"/> - no
        /// native Win32 control underneath (unlike <see cref="ProgressBars"/>),
        /// so no border/theming quirks to work around. For slim status-strip-
        /// style progress indicators rather than a prominent, full-size bar.
        /// </summary>
        public static class SlimProgressBars
        {
            /// <summary>Neutral gray fill by default, or the color of a color set; set <see cref="Control.ForeColor"/> for any other color.</summary>
            /// <param name="colors">A color set for the fill: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>. The bar is filled in the main color of the set (its <c>Hover</c> color: Green is the usual green, Primary the accent, a set made of a color has that color); null (the default) is the neutral gray.</param>
            public static SlimProgressBar CreateStandard(UIColorSet colors = null)
            {
                return UISlimProgressBarFactory.CreateStandard(colors);
            }

            /// <summary>The same as <c>CreateStandard(UIStyles.Colors.Sets.Green)</c>: a fixed green fill.</summary>
            public static SlimProgressBar CreateGreen()
            {
                return UISlimProgressBarFactory.CreateGreen();
            }

            /// <summary>The same as <c>CreateStandard(UIStyles.Colors.Sets.Primary)</c>: the fill follows the app-wide accent.</summary>
            public static SlimProgressBar CreatePrimary()
            {
                return UISlimProgressBarFactory.CreatePrimary();
            }

            /// <summary>Fill color follows Value instead of being fixed - red at Minimum, yellow at the midpoint, green at Maximum, blending smoothly between them. For a "status/health" bar where color communicates good/bad.</summary>
            public static SlimProgressBar CreateStatus()
            {
                return UISlimProgressBarFactory.CreateStatus();
            }
        }
    }
}
