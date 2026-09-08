using System.Drawing;
using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Factories
{
    internal static class UISpinnerFactory
    {
        public static Spinner CreateStandard(int size = 24, int thickness = 0)
        {
            return new Spinner
            {
                Size = new Size(size, size),
                Thickness = thickness
            };
        }

        public static Spinner CreatePrimary(int size = 24, int thickness = 0)
        {
            Spinner spinner = CreateStandard(size, thickness);
            spinner.Accent = true;
            return spinner;
        }
    }
}
