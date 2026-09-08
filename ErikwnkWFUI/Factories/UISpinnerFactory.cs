using System.Drawing;
using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Factories
{
    internal static class UISpinnerFactory
    {
        public static Spinner CreateStandard(int size = 24)
        {
            return new Spinner
            {
                Size = new Size(size, size)
            };
        }
    }
}
