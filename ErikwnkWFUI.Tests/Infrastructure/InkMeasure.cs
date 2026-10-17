using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>Measures where the text of a control is drawn, from a picture of it.</summary>
public static class InkMeasure
{
    /// <summary>
    /// The empty rows above and below the light pixels (the text) of a control; <paramref name="rightMargin"/> keeps the
    /// pixels at the right (the arrows of a numeric up/down) out of it.
    /// </summary>
    public static (int Above, int Below) SpaceAboveAndBelow(Control control, int rightMargin = 6)
    {
        using Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
        int top = -1, bottom = -1;

        for (int y = 2; y < control.Height - 2; y++)
            for (int x = 6; x < control.Width - rightMargin; x++)
                if (bitmap.GetPixel(x, y).R > 150)
                {
                    if (top < 0)
                        top = y;

                    bottom = y;
                }

        return (top, control.Height - 1 - bottom);
    }
}
