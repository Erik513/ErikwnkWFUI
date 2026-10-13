using System;
using System.Windows.Forms;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>Scaling of measures given for 96 dpi to the screen a control is on.</summary>
    internal static class Dpi
    {
        /// <summary>A measure of 96 dpi in the pixels of a screen with <paramref name="dpi"/> dots per inch (halves round away from zero).</summary>
        public static int Scale(int value96, int dpi)
        {
            return (int)Math.Round(value96 * dpi / 96.0, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// The same for the screen of <paramref name="control"/>. <paramref name="dpiOverride"/> is the seam of the tests: a
        /// value above zero pretends another screen.
        /// </summary>
        public static int Scale(Control control, int value96, int dpiOverride = 0)
        {
            return Scale(value96, dpiOverride > 0 ? dpiOverride : control.DeviceDpi);
        }
    }
}
