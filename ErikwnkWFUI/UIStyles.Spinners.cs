using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// A rotating <see cref="Controls.Spinner"/> for "loading …" states (only
        /// animates while visible). Plain variants are neutral grey, fixed green,
        /// or the theme accent; the <c>CreateProgress*</c> versions also show a
        /// percentage in the centre - raise <see cref="Spinner.Progress"/> as the
        /// load advances - and <see cref="CreateProgressStatus"/> additionally
        /// blends the arc red → yellow → green by that value, like
        /// <see cref="SlimProgressBars"/>.
        /// </summary>
        public static class Spinners
        {
            /// <summary>A neutral-grey spinner; set <see cref="Spinner.ArcColor"/> for any other colour.</summary>
            /// <param name="size">Width and height in px (the spinner is always square).</param>
            /// <param name="thickness">Stroke width in px; 0 scales it to <paramref name="size"/>.</param>
            public static Spinner CreateStandard(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateStandard(size, thickness);
            }

            /// <summary>A fixed green spinner.</summary>
            public static Spinner CreateGreen(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateGreen(size, thickness);
            }

            /// <summary>The arc follows the theme accent.</summary>
            public static Spinner CreatePrimary(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreatePrimary(size, thickness);
            }

            /// <summary>A grey spinner that also shows a percentage in the centre (starts at 0); set <see cref="Spinner.ArcColor"/> for any other colour.</summary>
            public static Spinner CreateProgressStandard(int size = 24, int thickness = 0)
            {
                return UISpinnerFactory.CreateProgressStandard(size, thickness);
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
