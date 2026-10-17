using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// A rotating <see cref="Controls.Spinner"/> for "loading …" states (only
        /// animates while visible). Plain variants are neutral gray, the colour of a
        /// colour set, fixed green, or the theme accent; the <c>CreateProgress*</c> versions also show a
        /// percentage in the centre - raise <see cref="Spinner.Value"/> as the
        /// load advances - and <see cref="CreateProgressStatus"/> additionally
        /// blends the arc red → yellow → green by that value, like
        /// <see cref="SlimProgressBars"/>.
        /// </summary>
        public static class Spinners
        {
            /// <summary>A neutral-gray spinner, or one in the colour of a colour set; set <see cref="Spinner.ArcColor"/> for any other colour.</summary>
            /// <param name="size">Width and height in px (the spinner is always square).</param>
            /// <param name="thickness">Stroke width in px; 0 scales it to <paramref name="size"/>.</param>
            /// <param name="colors">A colour set for the arc: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>. The arc has the main colour of the set (its <c>Hover</c> colour); null (the default) is the neutral gray.</param>
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

            /// <summary>A gray spinner (or one in the colour of a colour set) that also shows a percentage in the centre (starts at 0).</summary>
            public static Spinner CreateProgressStandard(int size = 24, int thickness = 0, UIColorSet colors = null)
            {
                return UISpinnerFactory.CreateProgressStandard(size, thickness, colors);
            }

            /// <summary>A green spinner that also shows a percentage in the centre (starts at 0).</summary>
            public static Spinner CreateProgressGreen(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateProgressGreen(size, thickness);
            }

            /// <summary>An accent spinner that also shows a percentage in the centre (starts at 0).</summary>
            public static Spinner CreateProgressPrimary(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateProgressPrimary(size, thickness);
            }

            /// <summary>Shows a percentage in the centre; the arc is a solid colour blended red (0) → yellow → green (100) by that value.</summary>
            public static Spinner CreateProgressStatus(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateProgressStatus(size, thickness);
            }
        }
    }
}
