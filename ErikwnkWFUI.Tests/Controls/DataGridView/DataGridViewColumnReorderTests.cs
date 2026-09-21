using System.ComponentModel;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;
using WfuiReadOnlyDataGridView = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// Column reordering is hand-rolled (mirroring ListView's own - see
/// ReadOnlyDataGridView.AllowColumnReordering) via a MouseDown/MouseMove
/// threshold check that starts a real DoDragDrop once exceeded - that part
/// needs an actual OLE drag and isn't practical to drive headlessly. These
/// tests instead cover the pieces that don't need a live drag: the target-
/// index math (MoveColumnToDisplayIndex, invoked directly via reflection,
/// same as PasteFromClipboard/DeleteRows elsewhere in this project) and the
/// public reorderable-column API it's gated behind.
/// </summary>
public class DataGridViewColumnReorderTests
{
    [Fact]
    public void MoveColumnToDisplayIndex_MovesColumnToTheEnd()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);

        Assert.Equal(0, grid.Columns["Name"]!.DisplayIndex);
        Assert.Equal(1, grid.Columns["Value"]!.DisplayIndex);

        // Moving column 0 ("Name") to "insert before display index 2" (i.e.
        // past the last column) puts it after "Value".
        grid.InvokePrivate("MoveColumnToDisplayIndex", 0, 2);

        Assert.Equal(1, grid.Columns["Name"]!.DisplayIndex);
        Assert.Equal(0, grid.Columns["Value"]!.DisplayIndex);
    }

    [Fact]
    public void MoveColumnToDisplayIndex_MovesColumnToTheFront()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);

        grid.InvokePrivate("MoveColumnToDisplayIndex", 1, 0);

        Assert.Equal(1, grid.Columns["Name"]!.DisplayIndex);
        Assert.Equal(0, grid.Columns["Value"]!.DisplayIndex);
    }

    [Fact]
    public void MoveColumnToDisplayIndex_SameTargetAsCurrent_IsANoOp()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);

        // "Name" is already at display index 0 - inserting it "before
        // display index 0" is the same position it's already at.
        grid.InvokePrivate("MoveColumnToDisplayIndex", 0, 0);

        Assert.Equal(0, grid.Columns["Name"]!.DisplayIndex);
        Assert.Equal(1, grid.Columns["Value"]!.DisplayIndex);
    }

    [Fact]
    public void IsColumnReorderable_DefaultsToTrueForEveryColumn()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);

        Assert.True(grid.IsColumnReorderable(0));
        Assert.True(grid.IsColumnReorderable(1));
    }

    [Fact]
    public void SetColumnReorderable_False_MakesOnlyThatColumnNotReorderable()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);

        grid.SetColumnReorderable(0, false);

        Assert.False(grid.IsColumnReorderable(0));
        Assert.True(grid.IsColumnReorderable(1));

        // Reversible, same as ListView's own SetColumnReorderable.
        grid.SetColumnReorderable(0, true);
        Assert.True(grid.IsColumnReorderable(0));
    }

    [Fact]
    public void AllowColumnReordering_False_OverridesEveryColumnRegardlessOfPerColumnSetting()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);

        grid.AllowColumnReordering = false;

        Assert.False(grid.IsColumnReorderable(0));
        Assert.False(grid.IsColumnReorderable(1));
    }

    [Fact]
    public void NativeAllowUserToOrderColumns_StaysFalse()
    {
        // Reordering is entirely hand-rolled so the drag feedback can use
        // this control's own accent color (see AllowColumnReordering's own
        // remarks) - the native switch must stay off, or WinForms' own
        // unthemeable drag would kick in as well/instead.
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);

        Assert.False(grid.AllowUserToOrderColumns);
    }

    [Fact]
    public void DeleteRowColumn_IsNotReorderable()
    {
        // Regression test: the delete-row column is pinned rightmost
        // (OnColumnAdded) regardless of what's added afterward - dragging
        // it to reorder would just fight that pinning right back on the
        // very next repaint/column-added, so it's excluded up front.
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

        grid.ShowDeleteRowColumn = true;

        Assert.False(grid.IsColumnReorderable(grid.Columns["__deleteRow"]!.Index));
        Assert.True(grid.IsColumnReorderable(grid.Columns["Name"]!.Index));
    }
}
