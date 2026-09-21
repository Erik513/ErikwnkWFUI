using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// Column reordering is hand-rolled (mirroring the later DataGridView port
/// of this same design - see ReadOnlyDataGridView.AllowColumnReordering) via
/// a MouseDown/MouseMove threshold check on the header's own native hwnd
/// (see HeaderInputSubclass) that starts a real DoDragDrop once exceeded -
/// that part needs an actual OLE drag on the real header window and isn't
/// practical to drive headlessly, same as DataGridView's own equivalent
/// (see DataGridViewColumnReorderTests). These tests instead cover the
/// target-index math (MoveColumnToDisplayIndex, invoked directly via
/// reflection) and the public reorderable-column API it's gated behind.
/// </summary>
public class ListViewColumnReorderTests
{
    [Fact]
    public void MoveColumnToDisplayIndex_MovesColumnToTheEnd()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Item", 100), ("Status", 100));

        // A genuine move (unlike the SameTargetAsCurrent case below) ends
        // with a BeginInvoke(ApplyFillColumn) call, which throws unless the
        // handle already exists - forced here, and only here, rather than
        // in the shared helper for every test: doing this for every
        // ListView test in the suite was confirmed live to make the whole
        // test host process crash outright once enough handles had been
        // created and disposed within one run (see ListViewTestHelpers'
        // own remarks on why it deliberately doesn't do this itself).
        _ = listView.Handle;

        Assert.Equal(0, listView.Columns[0].DisplayIndex);
        Assert.Equal(1, listView.Columns[1].DisplayIndex);

        // Moving column 0 ("Item") to "insert before display index 2" (i.e.
        // past the last column) puts it after "Status".
        listView.InvokePrivate("MoveColumnToDisplayIndex", 0, 2);

        Assert.Equal(1, listView.Columns[0].DisplayIndex);
        Assert.Equal(0, listView.Columns[1].DisplayIndex);
    }

    [Fact]
    public void MoveColumnToDisplayIndex_MovesColumnToTheFront()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Item", 100), ("Status", 100));
        _ = listView.Handle;

        listView.InvokePrivate("MoveColumnToDisplayIndex", 1, 0);

        Assert.Equal(1, listView.Columns[0].DisplayIndex);
        Assert.Equal(0, listView.Columns[1].DisplayIndex);
    }

    [Fact]
    public void MoveColumnToDisplayIndex_SameTargetAsCurrent_IsANoOp()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Item", 100), ("Status", 100));

        listView.InvokePrivate("MoveColumnToDisplayIndex", 0, 0);

        Assert.Equal(0, listView.Columns[0].DisplayIndex);
        Assert.Equal(1, listView.Columns[1].DisplayIndex);
    }

    [Fact]
    public void IsColumnReorderable_DefaultsToTrueForEveryColumn()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Item", 100), ("Status", 100));

        Assert.True(listView.IsColumnReorderable(0));
        Assert.True(listView.IsColumnReorderable(1));
    }

    [Fact]
    public void SetColumnReorderable_False_MakesOnlyThatColumnNotReorderable()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Item", 100), ("Status", 100));

        listView.SetColumnReorderable(0, false);

        Assert.False(listView.IsColumnReorderable(0));
        Assert.True(listView.IsColumnReorderable(1));

        listView.SetColumnReorderable(0, true);
        Assert.True(listView.IsColumnReorderable(0));
    }

    [Fact]
    public void AllowColumnReordering_False_OverridesEveryColumnRegardlessOfPerColumnSetting()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Item", 100), ("Status", 100));

        listView.AllowColumnReordering = false;

        Assert.False(listView.IsColumnReorderable(0));
        Assert.False(listView.IsColumnReorderable(1));
    }
}
