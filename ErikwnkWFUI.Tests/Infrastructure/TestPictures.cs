using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// What the tests that look at the picture of a control all need: the picture a control draws of itself, a look at two pictures,
/// and a wait that keeps the window alive. Use it with <c>using static</c>.
/// </summary>
internal static class TestPictures
{
    /// <summary>The picture a control draws of itself (its whole window, as DrawToBitmap makes it).</summary>
    public static Bitmap Draw(Control control)
    {
        _ = control.Handle;
        Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));

        return bitmap;
    }

    /// <summary>Whether two pictures are the same, pixel for pixel.</summary>
    public static bool SamePixels(Bitmap first, Bitmap second)
    {
        if (first.Size != second.Size)
            return false;

        return SamePixels(first, second, new Rectangle(0, 0, first.Width, first.Height));
    }

    /// <summary>Whether two pictures are the same within a region.</summary>
    public static bool SamePixels(Bitmap first, Bitmap second, Rectangle region)
    {
        for (int y = region.Top; y < region.Bottom; y++)
            for (int x = region.Left; x < region.Right; x++)
                if (first.GetPixel(x, y).ToArgb() != second.GetPixel(x, y).ToArgb())
                    return false;

        return true;
    }

    /// <summary>Lets the messages of the windows run for this long (the clocks of the controls tick meanwhile).</summary>
    public static void Pump(int milliseconds)
    {
        DateTime end = DateTime.UtcNow.AddMilliseconds(milliseconds);

        while (DateTime.UtcNow < end)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }
}
