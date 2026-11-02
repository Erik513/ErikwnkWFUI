using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.CheckBox;

/// <summary>
/// The box of a check box - outline, fill, checkmark, dash - drawn on its own (what the list view uses for its check boxes): which parts each state
/// has, at the scale of the screen, and the graphics is left as it was.
/// </summary>
public class CheckGlyphTests
{
    private static readonly Color Background = Color.Black;
    private static readonly Color Outline = Color.FromArgb(200, 200, 200);
    private static readonly Color Fill = Color.FromArgb(0, 90, 158);
    private static readonly Color Mark = Color.White;

    private static Bitmap Draw(CheckState state, int dpi = 96, int side = 16)
    {
        Bitmap bitmap = new Bitmap(side + 8, side + 8);

        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(Background);
            global::ErikwnkWFUI.Controls.CheckGlyph.Draw(g, new Rectangle(4, 4, side, side), state, Outline, Fill, Mark, dpi);
        }

        return bitmap;
    }

    private static bool Near(Color a, Color b, int tolerance = 70)
    {
        return Math.Abs(a.R - b.R) <= tolerance && Math.Abs(a.G - b.G) <= tolerance && Math.Abs(a.B - b.B) <= tolerance;
    }

    private static int Count(Bitmap bitmap, Func<Color, bool> match)
    {
        int count = 0;

        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (match(bitmap.GetPixel(x, y)))
                    count++;

        return count;
    }

    [Fact]
    public void AnEmptyBox_HasAnOutline_AndNothingInside()
    {
        using Bitmap box = Draw(CheckState.Unchecked);

        Assert.True(Near(box.GetPixel(12, 12), Background, 10), "the inside is not empty: " + box.GetPixel(12, 12));
        Assert.True(Count(box, color => Near(color, Outline, 60)) > 20, "no outline");
        Assert.Equal(0, Count(box, color => Near(color, Fill, 25)));
    }

    [Fact]
    public void ACheckedBox_IsFilled_AndHasTheMarkOnIt()
    {
        using Bitmap box = Draw(CheckState.Checked);

        Assert.True(Near(box.GetPixel(6, 17), Fill, 30), "the fill is missing: " + box.GetPixel(6, 17));
        Assert.True(Count(box, color => Near(color, Mark, 40)) > 8, "no checkmark");
    }

    [Fact]
    public void AnUndeterminedBox_IsFilled_AndHasADashInTheMiddle_NotACheckmark()
    {
        using Bitmap box = Draw(CheckState.Indeterminate);
        using Bitmap checkedBox = Draw(CheckState.Checked);

        Assert.True(Near(box.GetPixel(6, 17), Fill, 30));
        Assert.True(Near(box.GetPixel(12, 12), Mark, 40), "no dash in the middle: " + box.GetPixel(12, 12));
        Assert.False(Near(box.GetPixel(10, 18), Mark, 40), "a checkmark where the dash is");
        Assert.NotEqual(Count(checkedBox, color => Near(color, Mark, 40)), Count(box, color => Near(color, Mark, 40)));
    }

    [Fact]
    public void OnABigScreen_TheLinesAreThicker()
    {
        using Bitmap small = Draw(CheckState.Checked, dpi: 96, side: 16);
        using Bitmap big = Draw(CheckState.Checked, dpi: 192, side: 32);

        // The mark covers a share of the box: with doubled lines on a doubled box it is about the same share.
        double smallShare = Count(small, color => Near(color, Mark, 40)) / (double)(16 * 16);
        double bigShare = Count(big, color => Near(color, Mark, 40)) / (double)(32 * 32);

        Assert.InRange(bigShare / smallShare, 0.6, 1.5);
    }

    [Fact]
    public void TheGraphics_IsLeftAsItWas()
    {
        using Bitmap bitmap = new Bitmap(30, 30);
        using Graphics g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
        System.Drawing.Drawing2D.SmoothingMode before = g.SmoothingMode;

        global::ErikwnkWFUI.Controls.CheckGlyph.Draw(g, new Rectangle(4, 4, 16, 16), CheckState.Checked, Outline, Fill, Mark, 96);

        Assert.Equal(before, g.SmoothingMode);
        Assert.NotEqual(System.Drawing.Drawing2D.SmoothingMode.AntiAlias, g.SmoothingMode);
    }
}
