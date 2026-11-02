using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// A rotating <see cref="Controls.Spinner"/> for "loading ..." states. Plain variants are neutral gray, the color of a color set,
        /// fixed green or the theme accent; <c>CreateStatus</c> blends the arc red → yellow → green by <see cref="Spinner.Value"/>; the
        /// <c>CreateProgress*</c> versions also write that value as a percentage in the centre (raise it as the load advances). A text next
        /// to the ring, the speed, a known load and the rest are properties of the <see cref="Controls.Spinner"/>.
        /// </summary>
        public static class Spinners
        {
            /// <summary>A neutral-gray spinner, or one in the color of a color set; set <see cref="Spinner.ArcColor"/> for any other color.</summary>
            /// <param name="size">Width and height in px (the spinner is always square).</param>
            /// <param name="thickness">Stroke width in px; 0 scales it to <paramref name="size"/>.</param>
            /// <param name="colors">A color set for the arc: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>. The arc has the main color of the set (its <c>Hover</c> color); null (the default) is the neutral gray.</param>
            public static Spinner CreateStandard(int size = 24, int thickness = 0, UIColorSet colors = null)
            {
                return UISpinnerFactory.CreateStandard(size, thickness, colors);
            }

            /// <summary>The same as <c>CreateStandard(size, thickness, UIStyles.Colors.Sets.Green)</c>: a fixed green spinner.</summary>
            public static Spinner CreateGreen(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateGreen(size, thickness);
            }

            /// <summary>The arc follows the theme accent.</summary>
            public static Spinner CreatePrimary(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreatePrimary(size, thickness);
            }

            /// <summary>The arc is a solid color blended red (0) → yellow → green (100) by <see cref="Spinner.Value"/> (starts at 0 - raise it as the load advances); no number in the centre.</summary>
            public static Spinner CreateStatus(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateStatus(size, thickness);
            }

            /// <summary>A gray spinner (or one in the color of a color set) that also shows a percentage in the centre (starts at 0; 40 px by default - a ring smaller than about 30 px leaves the digits out as not legible).</summary>
            public static Spinner CreateProgressStandard(int size = 40, int thickness = 0, UIColorSet colors = null)
            {
                return UISpinnerFactory.CreateProgressStandard(size, thickness, colors);
            }

            /// <summary>A green spinner that also shows a percentage in the centre (starts at 0).</summary>
            public static Spinner CreateProgressGreen(int size = 40, int thickness = 0)
            {
                return UISpinnerFactory.CreateProgressGreen(size, thickness);
            }

            /// <summary>An accent spinner that also shows a percentage in the centre (starts at 0).</summary>
            public static Spinner CreateProgressPrimary(int size = 40, int thickness = 0)
            {
                return UISpinnerFactory.CreateProgressPrimary(size, thickness);
            }

            /// <summary>Shows a percentage in the centre; the arc is a solid color blended red (0) → yellow → green (100) by that value.</summary>
            public static Spinner CreateProgressStatus(int size = 40, int thickness = 0)
            {
                return UISpinnerFactory.CreateProgressStatus(size, thickness);
            }
        }
    }
}
