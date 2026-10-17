using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>The color a control's text stands on: its own background, or - when that is transparent - the first one behind it.</summary>
    internal static class SurfaceColor
    {
        public static Color Of(Control control)
        {
            for (Control current = control; current != null; current = current.Parent)
            {
                if (current.BackColor.A == 255)
                    return current.BackColor;
            }

            return UIColors.BackgroundMedium;
        }
    }
}
