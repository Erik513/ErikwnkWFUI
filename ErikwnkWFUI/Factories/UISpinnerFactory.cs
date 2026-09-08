using System;
using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UISpinnerFactory
    {
        // Plain rotating spinners - colour variants mirror UISlimProgressBarFactory.

        public static Spinner CreateGreen(int size = 24, int thickness = 0)
        {
            return Configure(size, thickness, delegate(Spinner s) { s.ArcColor = UIColors.Green; });
        }

        public static Spinner CreatePrimary(int size = 24, int thickness = 0)
        {
            return Configure(size, thickness, delegate(Spinner s) { s.Accent = true; });
        }

        // Same, plus a percentage in the centre (starts at 0 - raise it as the
        // load advances). The status one also colours the arc red -> yellow ->
        // green by that value; a plain "status" spinner without a number makes
        // no sense, so there is no non-progress CreateStatus.

        public static Spinner CreateProgressGreen(int size = 24, int thickness = 0)
        {
            return WithProgress(CreateGreen(size, thickness));
        }

        public static Spinner CreateProgressPrimary(int size = 24, int thickness = 0)
        {
            return WithProgress(CreatePrimary(size, thickness));
        }

        public static Spinner CreateProgressStatus(int size = 24, int thickness = 0)
        {
            return WithProgress(Configure(size, thickness,
                delegate(Spinner s) { s.UseStatusGradient = true; }));
        }

        private static Spinner Configure(int size, int thickness, Action<Spinner> apply)
        {
            Spinner spinner = new Spinner
            {
                Size = new Size(size, size),
                Thickness = thickness
            };
            apply(spinner);
            return spinner;
        }

        private static Spinner WithProgress(Spinner spinner)
        {
            spinner.Progress = 0;
            return spinner;
        }
    }
}
