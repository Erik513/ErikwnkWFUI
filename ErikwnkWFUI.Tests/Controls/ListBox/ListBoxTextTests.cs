using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>The text of an item: one line, in the middle of the row, and an ellipsis where it is too long.</summary>
public class ListBoxTextTests
{
    // The row of an item, painted the way the list paints it.
    private static Bitmap PaintRow(WfuiListBox listBox, int index, int width, int height)
    {
        Bitmap bitmap = new Bitmap(width, height);

        using (Graphics g = Graphics.FromImage(bitmap))
        {
            DrawItemEventArgs e = new DrawItemEventArgs(g, listBox.Font, new Rectangle(0, 0, width, height), index, DrawItemState.None);
            listBox.InvokePrivate("OnDrawItem", e);
        }

        return bitmap;
    }

    // The smallest rectangle around every pixel that is not the color of the row.
    private static Rectangle InkBounds(Bitmap bitmap, Color row)
    {
        int left = bitmap.Width, top = bitmap.Height, right = -1, bottom = -1;

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) == row)
                    continue;

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return right < 0 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    [Fact]
    public void ALongText_StaysOneLine_InTheMiddleOfTheRow_AndStopsBeforeTheRightMargin()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(80, string.Join(" ", Enumerable.Repeat("a long text that does not fit", 6)));
        using Bitmap row = PaintRow(listBox, 0, 200, 80);
        Rectangle ink = InkBounds(row, row.GetPixel(0, 0));

        Assert.False(ink.IsEmpty);
        Assert.True(ink.Height <= listBox.Font.Height, "more than one line: " + ink);
        Assert.InRange(ink.Top + ink.Height / 2, 30, 50);
        Assert.True(ink.Right <= 200, "the text runs out of the row: " + ink);
    }

    [Fact]
    public void AShortText_StartsAtTheLeftMargin()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(40, "Hi");
        using Bitmap row = PaintRow(listBox, 0, 200, 40);
        Rectangle ink = InkBounds(row, row.GetPixel(0, 0));

        Assert.InRange(ink.Left, 8, 12);
    }
}
