using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Buttons;

/// <summary>
/// The shift that puts the visible part of a symbol in the middle of its button: it is found by drawing the symbol and
/// looking at the pixels. The result must be the one a slow pixel-by-pixel reading gives.
/// </summary>
public class SymbolButtonInkShiftTests
{
    private const TextFormatFlags Flags =
        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;

    // The reference: the same drawing, read with GetPixel.
    private static Point Reference(Button button)
    {
        int width = button.Width, height = button.Height;
        using Bitmap bitmap = new Bitmap(width, height);
        using Graphics g = Graphics.FromImage(bitmap);
        g.Clear(Color.Black);
        TextRenderer.DrawText(g, button.Text, button.Font, new Rectangle(0, 0, width, height), Color.White, Flags);

        int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color pixel = bitmap.GetPixel(x, y);

                if (pixel.R + pixel.G + pixel.B <= 60 * 3)
                    continue;

                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }

        if (maxX < 0)
            return Point.Empty;

        return new Point(
            (int)Math.Round(((minX + maxX + 1) - width) / 2.0, MidpointRounding.AwayFromZero),
            (int)Math.Round(((minY + maxY + 1) - height) / 2.0, MidpointRounding.AwayFromZero));
    }

    [Theory]
    [InlineData(30, 30)]
    [InlineData(24, 24)]
    [InlineData(50, 30)]
    [InlineData(48, 48)]
    [InlineData(61, 37)]
    public void TheShift_IsTheOneOfTheSlowReading_ForEverySymbol(int width, int height)
    {
        foreach (string symbol in new[] { UIStyles.Symbols.Add, UIStyles.Symbols.Delete, UIStyles.Symbols.Folder, UIStyles.Symbols.Back, UIStyles.Symbols.Heart, UIStyles.Symbols.Document, UIStyles.Symbols.Play, UIStyles.Symbols.Menu })
        {
            using Button button = UIStyles.Buttons.CreateSymbol(symbol, "", new Size(width, height));

            Assert.Equal(Reference(button), button.InvokePrivate<Point>("GetInkShift"));
        }
    }

    [Fact]
    public void ASymbolThatDrawsNothing_HasNoShift_AndAButtonWithoutASizeToo()
    {
        using Button blank = UIStyles.Buttons.CreateSymbol(" ", "", new Size(30, 30));
        using Button tiny = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Add, "", new Size(1, 1));

        Assert.Equal(Point.Empty, blank.InvokePrivate<Point>("GetInkShift"));
        Assert.Equal(Reference(tiny), tiny.InvokePrivate<Point>("GetInkShift"));
    }
}
