using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// Column reordering is a real DoDragDrop started from a mouse threshold
/// on the header's native hwnd (see HeaderInputSubclass) - not practical
/// to drive headlessly, same as DataGridView's equivalent.
///
/// MoveColumnToDisplayIndex delegates to the shared ColumnLayoutMath, and
/// Is/Set/AllowColumnReorderable to the shared ColumnFeatureSwitch (see
/// ColumnLayoutMathTests/ColumnFeatureSwitchTests, in the parent Controls
/// test folder) - the full scenario matrix for both is covered there once
/// instead of per control; this file keeps just the two tests below
/// confirming this control's own methods actually delegate to them.
/// </summary>
public class ListViewColumnReorderTests
{
    [Fact]
    public void MoveColumnToDisplayIndex_MovesColumnToTheEnd()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Item", 100), ("Status", 100));

        Assert.Equal(0, listView.Columns[0].DisplayIndex);
        Assert.Equal(1, listView.Columns[1].DisplayIndex);

        // Moving column 0 ("Item") to "insert before display index 2" (i.e.
        // past the last column) puts it after "Status".
        listView.InvokePrivate("MoveColumnToDisplayIndex", 0, 2);

        Assert.Equal(1, listView.Columns[0].DisplayIndex);
        Assert.Equal(0, listView.Columns[1].DisplayIndex);
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
}
