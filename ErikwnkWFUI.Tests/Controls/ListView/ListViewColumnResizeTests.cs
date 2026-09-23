using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// Column resize is hand-rolled in HeaderInputSubclass (mouse deltas via
/// SetCapture on the header hwnd), same as the reorder drag - not
/// practical to drive headlessly. Any width change still goes through
/// OnColumnWidthChanging's validation/clamping, so that's what these tests
/// cover, invoking it directly via reflection.
/// </summary>
public class ListViewColumnResizeTests
{
    [Fact]
    public void IsColumnResizable_DefaultsToTrueForEveryColumn()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Item", 100), ("Status", 100));

        Assert.True(listView.IsColumnResizable(0));
        Assert.True(listView.IsColumnResizable(1));
    }

    [Fact]
    public void AllowColumnResizing_False_MakesEveryColumnNotResizable()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Item", 100), ("Status", 100));

        listView.AllowColumnResizing = false;

        Assert.False(listView.IsColumnResizable(0));
        Assert.False(listView.IsColumnResizable(1));
    }

    [Fact]
    public void SetColumnResizable_False_MakesOnlyThatColumnNotResizable()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Item", 100), ("Status", 100));

        listView.SetColumnResizable(0, false);

        Assert.False(listView.IsColumnResizable(0));
        Assert.True(listView.IsColumnResizable(1));

        listView.SetColumnResizable(0, true);
        Assert.True(listView.IsColumnResizable(0));
    }

    [Fact]
    public void ColumnWidthChanging_BelowMinimum_ClampsToMinimumColumnWidth()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("A", 100));
        listView.MinimumColumnWidth = 40;

        ColumnWidthChangingEventArgs args = new ColumnWidthChangingEventArgs(0, 5);
        listView.InvokePrivate("OnColumnWidthChanging", listView, args);

        Assert.True(args.Cancel);
        Assert.Equal(40, args.NewWidth);
    }

    [Fact]
    public void ColumnWidthChanging_AboveMinimum_IsLeftAlone()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("A", 100));
        listView.MinimumColumnWidth = 40;

        ColumnWidthChangingEventArgs args = new ColumnWidthChangingEventArgs(0, 120);
        listView.InvokePrivate("OnColumnWidthChanging", listView, args);

        Assert.False(args.Cancel);
        Assert.Equal(120, args.NewWidth);
    }

    [Fact]
    public void ColumnWidthChanging_ColumnNotResizable_AlwaysCancelledBackToCurrentWidth()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("A", 100), ("B", 100));
        listView.SetColumnResizable(0, false);

        ColumnWidthChangingEventArgs args = new ColumnWidthChangingEventArgs(0, 150);
        listView.InvokePrivate("OnColumnWidthChanging", listView, args);

        Assert.True(args.Cancel);
        Assert.Equal(100, args.NewWidth);
    }
}
