using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>The sort glyph and the insertion line of a column drag, which the list and the grid draw the same way.</summary>
public class ColumnHeaderPaintingTests
{
    private static readonly Rectangle Cell = new Rectangle(10, 0, 80, 24);

    private static Bitmap Blank()
    {
        Bitmap picture = new Bitmap(100, 24);
        using Graphics graphics = Graphics.FromImage(picture);
        graphics.Clear(Color.White);

        return picture;
    }

    private static int DarkPixels(Bitmap picture)
    {
        int count = 0;

        for (int x = 0; x < picture.Width; x++)
            for (int y = 0; y < picture.Height; y++)
                if (picture.GetPixel(x, y).R < 200)
                    count++;

        return count;
    }

    private static int DarkPixelsAfterGlyph(SortOrder order)
    {
        using Bitmap picture = Blank();
        using Font font = new Font("Segoe UI", 9f);
        using (Graphics graphics = Graphics.FromImage(picture))
            ColumnHeaderPainting.DrawSortGlyph(graphics, Cell, font, Color.Black, order);

        return DarkPixels(picture);
    }

    [Fact]
    public void TheSortGlyph_IsDrawnForBothDirections_AndNotWithoutASort()
    {
        Assert.Equal(0, DarkPixelsAfterGlyph(SortOrder.None));
        Assert.True(DarkPixelsAfterGlyph(SortOrder.Ascending) > 0);
        Assert.True(DarkPixelsAfterGlyph(SortOrder.Descending) > 0);
    }

    [Fact]
    public void TheSortGlyph_SitsAtTheRightEdgeOfTheCell()
    {
        using Bitmap picture = Blank();
        using Font font = new Font("Segoe UI", 9f);
        using (Graphics graphics = Graphics.FromImage(picture))
            ColumnHeaderPainting.DrawSortGlyph(graphics, Cell, font, Color.Black, SortOrder.Ascending);

        int left = int.MaxValue;
        for (int x = 0; x < picture.Width; x++)
            for (int y = 0; y < picture.Height; y++)
                if (picture.GetPixel(x, y).R < 200)
                    left = Math.Min(left, x);

        Assert.True(left > Cell.Left + Cell.Width / 2, "the glyph starts at " + left);
    }

    private static Bitmap DrawLine(int displayIndex, int count, int before)
    {
        Bitmap picture = Blank();
        using Graphics graphics = Graphics.FromImage(picture);
        ColumnHeaderPainting.DrawColumnDragInsertionLine(graphics, Cell, displayIndex, count, before, Color.Black);

        return picture;
    }

    [Fact]
    public void TheInsertionLine_IsAtTheLeftEdgeOfTheColumnThatTheDragLandsBefore()
    {
        using Bitmap picture = DrawLine(1, 3, 1);

        Assert.Equal(Color.Black.ToArgb(), picture.GetPixel(Cell.Left, 5).ToArgb());
        Assert.Equal(Color.Black.ToArgb(), picture.GetPixel(Cell.Left + 1, 5).ToArgb());
        Assert.NotEqual(Color.Black.ToArgb(), picture.GetPixel(Cell.Left + 2, 5).ToArgb());
    }

    [Fact]
    public void TheInsertionLine_AfterTheLastColumn_IsAtTheRightEdgeOfTheLastOne()
    {
        using Bitmap picture = DrawLine(2, 3, 3);

        Assert.Equal(Color.Black.ToArgb(), picture.GetPixel(Cell.Right - 1, 5).ToArgb());
        Assert.Equal(Color.Black.ToArgb(), picture.GetPixel(Cell.Right - 2, 5).ToArgb());
    }

    [Fact]
    public void TheInsertionLine_IsNotDrawnForTheOtherColumns_OrWithoutADrag()
    {
        using Bitmap other = DrawLine(0, 3, 1);
        using Bitmap none = DrawLine(1, 3, -1);
        using Bitmap middle = DrawLine(1, 3, 3);

        Assert.Equal(0, DarkPixels(other));
        Assert.Equal(0, DarkPixels(none));
        Assert.Equal(0, DarkPixels(middle));
    }
}
