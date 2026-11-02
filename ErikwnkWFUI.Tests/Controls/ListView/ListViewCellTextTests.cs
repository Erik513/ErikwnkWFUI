using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>Where the text of a cell goes, which is also what the tooltip for a cut-off text measures against.</summary>
public class ListViewCellTextTests
{
    private static Rectangle TextRectangle(WfuiListView list, Rectangle cell, int column) =>
        list.InvokePrivate<Rectangle>("GetCellTextRectangle", cell, column);

    private static bool Truncated(WfuiListView list, string text, int column) =>
        list.InvokePrivate<bool>("IsTextTruncated", text, list.Font, list.Columns[column]);

    [Fact]
    public void TheTextOfACell_StartsSixPixelsFromTheLeft_AndEndsThreeBeforeTheRight()
    {
        using WfuiListView list = new WfuiListView { DpiOverride = 96 };

        Rectangle text = TextRectangle(list, new Rectangle(10, 4, 100, 20), 1);

        Assert.Equal(new Rectangle(16, 4, 91, 20), text);
    }

    [Fact]
    public void TheRoomAroundTheText_GrowsWithTheScreen()
    {
        using WfuiListView list = new WfuiListView { DpiOverride = 192 };

        Rectangle text = TextRectangle(list, new Rectangle(10, 4, 100, 20), 1);

        Assert.Equal(new Rectangle(22, 4, 100 - 12 - 6, 20), text);
    }

    [Fact]
    public void TheTextOfTheFirstCell_OfAListWithCheckBoxes_StartsBehindTheBox()
    {
        using WfuiListView list = new WfuiListView { CheckBoxes = true, DpiOverride = 96 };
        Rectangle cell = new Rectangle(0, 0, 100, 20);

        Rectangle first = TextRectangle(list, cell, 0);
        Rectangle second = TextRectangle(list, cell, 1);

        Assert.True(first.Left >= 2 + 16 + 6, "inside the box: " + first);
        Assert.Equal(6, second.Left);
        Assert.Equal(cell.Right - 3, first.Right);
    }

    [Fact]
    public void ATextThatFitsTheColumn_ButNotBehindTheBox_GetsATooltipOnlyInTheFirstColumn()
    {
        using WfuiListView list = new WfuiListView { CheckBoxes = true };
        list.Columns.Add("A", 120);
        list.Columns.Add("B", 120);
        int width = TextRenderer.MeasureText("A text of some length", list.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
        list.Columns[0].Width = list.Columns[1].Width = width + 10;

        Assert.False(Truncated(list, "A text of some length", 1));
        Assert.True(Truncated(list, "A text of some length", 0));
    }
}
