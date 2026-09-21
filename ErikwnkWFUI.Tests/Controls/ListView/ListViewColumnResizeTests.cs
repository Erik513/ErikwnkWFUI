using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// Unlike DataGridView's hand-rolled column resize, this control's own
/// resize is still fully native - OnColumnWidthChanging (subscribed to the
/// base ListView's own event) only validates/clamps what comctl32's header
/// already resized live, it doesn't drive the drag itself. That validation
/// is what these tests cover, invoking the private handler directly with a
/// real ColumnWidthChangingEventArgs the same way a live drag would.
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
        // A second column, so column 0 isn't the (rightmost-by-default)
        // fill column itself - ApplyFillColumn stretches that one to
        // whatever space is actually available the moment the handle is
        // created, which would make its own "current width" here whatever
        // that happened to resolve to rather than the exact 100 asked for.
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("A", 100), ("B", 100));
        listView.SetColumnResizable(0, false);

        ColumnWidthChangingEventArgs args = new ColumnWidthChangingEventArgs(0, 150);
        listView.InvokePrivate("OnColumnWidthChanging", listView, args);

        Assert.True(args.Cancel);
        Assert.Equal(100, args.NewWidth);
    }
}
