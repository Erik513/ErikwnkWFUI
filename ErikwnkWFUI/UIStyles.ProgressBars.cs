using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// Progress bars in the colors of the theme (continuous by default, not blocky like the standard control; Blocks and Marquee can be set). Cast the
        /// result to <c>ErikwnkWFUI.Controls.ProgressBar</c> for what the standard control does not have: <c>BorderStyle</c> (the thin border
        /// around the bar, on by default), a <c>BackColor</c> of <see cref="System.Drawing.Color.Transparent"/> (the track then takes the
        /// color of the panel the bar lies on, also when it changes, and in both themes; independent of the border), <c>Text</c> and
        /// <c>ShowPercentage</c> written on the bar (<c>TextAlign</c> and <c>PercentageAlign</c> place them), <c>TextScroll</c> for a text that
        /// is too long, <c>ShowFullTextToolTip</c>, and the colors of the border and of the text (<c>BorderColor</c>, <c>TextColor</c>,
        /// <c>TextOnFillColor</c>; the fill and the track are <c>ForeColor</c> and <c>BackColor</c>).
        /// </summary>
        public static class ProgressBars
        {
            /// <summary>Neutral gray fill by default, or the color of a color set; set <see cref="Control.ForeColor"/> for any other color.</summary>
            /// <param name="colors">A color set for the fill: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>. The bar is filled in the main color of the set (its <c>Hover</c> color: Green is the usual green, Primary the accent, a set made of a color has that color); null (the default) is the neutral gray.</param>
            public static ProgressBar CreateStandard(UIColorSet colors = null)
            {
                return UIProgressBarFactory.CreateStandard(colors);
            }

            /// <summary>The same as <c>CreateStandard(UIStyles.Colors.Sets.Green)</c>: a fixed green fill.</summary>
            public static ProgressBar CreateGreen()
            {
                return UIProgressBarFactory.CreateGreen();
            }

            /// <summary>The same as <c>CreateStandard(UIStyles.Colors.Sets.Primary)</c>: the fill follows the app-wide accent.</summary>
            public static ProgressBar CreatePrimary()
            {
                return UIProgressBarFactory.CreatePrimary();
            }

            /// <summary>Fill color follows Value instead of being fixed - red at Minimum, yellow at the midpoint, green at Maximum, blending smoothly between them. For a "status/health" bar where color communicates good/bad.</summary>
            public static ProgressBar CreateStatus()
            {
                return UIProgressBarFactory.CreateStatus();
            }
        }
    }
}
