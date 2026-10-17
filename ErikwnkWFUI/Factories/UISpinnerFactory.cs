using System;
using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UISpinnerFactory
    {
        // Plain rotating spinners - the color variants mirror UIProgressBarFactory: neutral gray by default, the main color of a color
        // set (its Hover color) with one, the accent for Primary, and Green is the green set.

        public static Spinner CreateStandard(int size = 24, int thickness = 0, UIColorSet colors = null)
        {
            Spinner spinner = Configure(size, thickness);
            spinner.Colors = colors;

            return spinner;
        }

        public static Spinner CreateGreen(int size = 24, int thickness = 0)
        {
            return CreateStandard(size, thickness, UIStyles.Colors.Sets.Green);
        }

        // The set is asked for again each time: the primary set follows the accent.
        public static Spinner CreatePrimary(int size = 24, int thickness = 0)
        {
            Spinner spinner = Configure(size, thickness);
            spinner.FollowColors(() => UIColors.PrimarySet);

            return spinner;
        }

        // The arc is colored red -> yellow -> green by the Value (raise it as the load advances), without a number in the centre.
        public static Spinner CreateStatus(int size = 24, int thickness = 0)
        {
            Spinner spinner = Configure(size, thickness);
            spinner.UseStatusGradient = true;

            return spinner;
        }

        // Same, plus the percentage in the centre (40 px by default: smaller rings leave the digits out as not legible; starts at 0 - raise Value as the load advances). The status one also colors the
        // arc red -> yellow -> green by the value (CreateStatus is the same without the number).

        public static Spinner CreateProgressStandard(int size = 40, int thickness = 0, UIColorSet colors = null)
        {
            return WithPercentage(CreateStandard(size, thickness, colors));
        }

        public static Spinner CreateProgressGreen(int size = 40, int thickness = 0)
        {
            return WithPercentage(CreateGreen(size, thickness));
        }

        public static Spinner CreateProgressPrimary(int size = 40, int thickness = 0)
        {
            return WithPercentage(CreatePrimary(size, thickness));
        }

        public static Spinner CreateProgressStatus(int size = 40, int thickness = 0)
        {
            return WithPercentage(CreateStatus(size, thickness));
        }

        private static Spinner Configure(int size, int thickness)
        {
            return new Spinner
            {
                Size = new Size(size, size),
                Thickness = thickness
            };
        }

        private static Spinner WithPercentage(Spinner spinner)
        {
            spinner.Value = 0;
            spinner.ShowPercentage = true;

            return spinner;
        }
    }
}
