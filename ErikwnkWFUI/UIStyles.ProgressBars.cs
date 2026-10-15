using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A themed <see cref="ProgressBar"/> (continuous style, not the native blocky one).</summary>
        public static class ProgressBars
        {
            /// <summary>Neutral gray fill by default, or the color of a color set; set <see cref="Control.ForeColor"/> for any other color.</summary>
            /// <param name="colors">A color set for the fill: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>. The bar is filled in the main color of the set (its <c>Hover</c> color: Green is the usual green, Primary the accent, a set made of a color has that color); null (the default) is the neutral gray.</param>
            public static ProgressBar CreateStandard(UIColorSet colors = null)
            {
                return UIProgressBarFactory.CreateStandard(colors);
            }

            /// <summary>Like <see cref="CreateStandard"/> but without the border, and BackColor left for you to match your own panel.</summary>
            /// <param name="colors">A color set for the fill: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>. The bar is filled in the main color of the set (its <c>Hover</c> color: Green is the usual green, Primary the accent, a set made of a color has that color); null (the default) is the neutral gray.</param>
            public static ProgressBar CreateStandardBorderless(UIColorSet colors = null)
            {
                return UIProgressBarFactory.CreateStandardBorderless(colors);
            }

            /// <summary>The same as <c>CreateStandard(UIStyles.Colors.Sets.Green)</c>: a fixed green fill.</summary>
            public static ProgressBar CreateGreen()
            {
                return UIProgressBarFactory.CreateGreen();
            }

            /// <summary>Like <see cref="CreateGreen"/> but without the border, and BackColor left for you to match your own panel - the closest a ProgressBar can get to a transparent background.</summary>
            public static ProgressBar CreateGreenBorderless()
            {
                return UIProgressBarFactory.CreateGreenBorderless();
            }

            /// <summary>The same as <c>CreateStandard(UIStyles.Colors.Sets.Primary)</c>: the fill follows the app-wide accent.</summary>
            public static ProgressBar CreatePrimary()
            {
                return UIProgressBarFactory.CreatePrimary();
            }

            /// <summary>Like <see cref="CreatePrimary"/> but without the border, and BackColor left for you to match your own panel - the accent-following equivalent of <see cref="CreateGreenBorderless"/>.</summary>
            public static ProgressBar CreatePrimaryBorderless()
            {
                return UIProgressBarFactory.CreatePrimaryBorderless();
            }

            /// <summary>Fill color follows Value instead of being fixed - red at Minimum, yellow at the midpoint, green at Maximum, blending smoothly between them. For a "status/health" bar where color communicates good/bad.</summary>
            public static ProgressBar CreateStatus()
            {
                return UIProgressBarFactory.CreateStatus();
            }

            /// <summary>Like <see cref="CreateStatus"/> but without the border, and BackColor left for you to match your own panel - the status-gradient equivalent of <see cref="CreateGreenBorderless"/>.</summary>
            public static ProgressBar CreateStatusBorderless()
            {
                return UIProgressBarFactory.CreateStatusBorderless();
            }
        }
    }
}
