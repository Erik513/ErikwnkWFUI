using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Tests.Controls.ProgressBars;

/// <summary>What the tests of the progress bars all need: a picture of a bar, a look at two pictures, a wait that keeps the window alive.</summary>
internal static class ProgressBarTestSupport
{
    /// <summary>The picture a control draws of itself (its whole window, as DrawToBitmap makes it).</summary>
    public static Bitmap Draw(Control control)
    {
        Bitmap picture = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(picture, new Rectangle(Point.Empty, control.Size));

        return picture;
    }

    /// <summary>Whether two pictures of the same size are the same, pixel for pixel.</summary>
    public static bool SamePicture(Bitmap a, Bitmap b)
    {
        return SamePicture(a, b, new Rectangle(0, 0, a.Width, a.Height));
    }

    /// <summary>Whether two pictures are the same within a region.</summary>
    public static bool SamePicture(Bitmap a, Bitmap b, Rectangle region)
    {
        for (int y = region.Top; y < region.Bottom; y++)
            for (int x = region.Left; x < region.Right; x++)
                if (a.GetPixel(x, y).ToArgb() != b.GetPixel(x, y).ToArgb())
                    return false;

        return true;
    }

    /// <summary>Lets the messages of the windows run for this long (the clocks of the bars tick meanwhile).</summary>
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
