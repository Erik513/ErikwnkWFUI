using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Tests.Controls.ProgressBars;

/// <summary>The colors a progress bar paints, which are not always the ForeColor it has: the slim bar moves the main color of its set against its track.</summary>
internal static class PaintedColors
{
    public static Color Fill(Control bar)
    {
        return bar is SlimProgressBar slim ? slim.GetFillColor(slim.BackColor) : bar.ForeColor;
    }
}
