using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;
using WfuiReadOnlyDataGridView = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// A grab-bag of smaller, previously-untested pieces from the same
/// coverage audit as the other DataGridView*Tests files in this project:
/// HandleContextMenuMouseDown (right-click into empty space),
/// TryGetSelectedRowIndexRange (the "insert row above/below" range
/// computation), SetSortComparer's custom-comparer path, and
/// OnApplyingBatchedDataSourceChange's stale-hover-index reset.
/// </summary>
public class DataGridViewMiscCoverageTests
{
    [Fact]
    public void HandleContextMenuMouseDown_RightClickInEmptySpace_ResetsContextMenuState()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.SetPrivateField("_contextMenuRowIndex", 3);
        grid.SetPrivateField("_contextMenuColumnIndex", 3);

        // Far past every row/column this tiny grid actually has - HitTest
        // resolves this to DataGridViewHitTestType.None, the same as a
        // real right-click in the empty space below the last row.
        grid.InvokePrivate("HandleContextMenuMouseDown", grid, new MouseEventArgs(MouseButtons.Right, 1, 10_000, 10_000, 0));

        Assert.Equal(-1, grid.GetPrivateField<int>("_contextMenuRowIndex"));
        Assert.Equal(-1, grid.GetPrivateField<int>("_contextMenuColumnIndex"));
    }

    [Fact]
    public void HandleContextMenuMouseDown_LeftClickInEmptySpace_LeavesContextMenuStateAlone()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.SetPrivateField("_contextMenuRowIndex", 3);
        grid.SetPrivateField("_contextMenuColumnIndex", 3);

        grid.InvokePrivate("HandleContextMenuMouseDown", grid, new MouseEventArgs(MouseButtons.Left, 1, 10_000, 10_000, 0));

        Assert.Equal(3, grid.GetPrivateField<int>("_contextMenuRowIndex"));
        Assert.Equal(3, grid.GetPrivateField<int>("_contextMenuColumnIndex"));
    }

    [Fact]
    public void HandleContextMenuMouseDown_RightClickOnARealCell_LeavesContextMenuStateAlone()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.SetPrivateField("_contextMenuRowIndex", 3);
        grid.SetPrivateField("_contextMenuColumnIndex", 3);

        grid.InvokePrivate("HandleContextMenuMouseDown", grid, new MouseEventArgs(MouseButtons.Right, 1, 2, 2, 0));

        Assert.Equal(3, grid.GetPrivateField<int>("_contextMenuRowIndex"));
        Assert.Equal(3, grid.GetPrivateField<int>("_contextMenuColumnIndex"));
    }

    [Fact]
    public void TryGetSelectedRowIndexRange_ReturnsMinAndMaxOfSelectedRealRows()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3), ("D", 4));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        GridTestHelpers.SelectCells(grid, (1, 0), (3, 0));

        object?[] args = { 0, 0 };
        bool found = grid.InvokePrivate<bool>("TryGetSelectedRowIndexRange", args);

        Assert.True(found);
        Assert.Equal(1, args[0]);
        Assert.Equal(3, args[1]);
    }

    [Fact]
    public void TryGetSelectedRowIndexRange_OnlySystemColumnsAndPlaceholderSelected_ReturnsFalse()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.ShowDeleteRowColumn = true;
        int deleteColumnIndex = grid.Columns["__deleteRow"]!.Index;
        int placeholderIndex = grid.Rows.Count - 1;
        GridTestHelpers.SelectCells(grid, (0, deleteColumnIndex), (placeholderIndex, 0));

        object?[] args = { 0, 0 };
        bool found = grid.InvokePrivate<bool>("TryGetSelectedRowIndexRange", args);

        Assert.False(found);
    }

    [Fact]
    public void TryGetSelectedRowIndexRange_NothingSelected_ReturnsFalse()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.ClearSelection();

        object?[] args = { 0, 0 };
        bool found = grid.InvokePrivate<bool>("TryGetSelectedRowIndexRange", args);

        Assert.False(found);
    }

    // SetSortComparer's registered IComparer, exercised end-to-end through
    // a real header click (OnColumnHeaderMouseClick - see
    // DataGridViewColumnResizeTests' own use of it for the same reason: no
    // real mouse needed for a click, unlike a drag). Sorts by absolute
    // value, which orders these rows differently than the default
    // value-based comparison would.
    [Fact]
    public void SetSortComparer_CustomComparer_IsUsedInsteadOfTheDefaultOne()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 3), ("B", -5), ("C", -1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        IComparer absoluteValueComparer = Comparer<object>.Create((x, y) => Math.Abs((int)x).CompareTo(Math.Abs((int)y)));
        grid.SetSortComparer(grid.Columns["Value"]!, absoluteValueComparer);

        grid.InvokePrivate("OnColumnHeaderMouseClick",
            new DataGridViewCellMouseEventArgs(grid.Columns["Value"]!.Index, -1, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)));

        // Ascending by absolute value: C (1), A (3), B (5) - not the
        // default ascending-by-value order (B, C, A).
        Assert.Equal("C", items[0].Name);
        Assert.Equal("A", items[1].Name);
        Assert.Equal("B", items[2].Name);
    }

    [Fact]
    public void SetSortComparer_NullComparer_RemovesItAndRestoresDefaultSorting()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 3), ("B", -5), ("C", -1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        DataGridViewColumn valueColumn = grid.Columns["Value"]!;
        grid.SetSortComparer(valueColumn, Comparer<object>.Create((x, y) => Math.Abs((int)x).CompareTo(Math.Abs((int)y))));

        grid.SetSortComparer(valueColumn, null);
        grid.InvokePrivate("OnColumnHeaderMouseClick",
            new DataGridViewCellMouseEventArgs(valueColumn.Index, -1, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)));

        // Back to plain ascending-by-value: B (-5), C (-1), A (3).
        Assert.Equal("B", items[0].Name);
        Assert.Equal("C", items[1].Name);
        Assert.Equal("A", items[2].Name);
    }

    [Fact]
    public void OnApplyingBatchedDataSourceChange_ResetsTheHoveredDeleteRowIndex()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.SetPrivateField("_hoveredDeleteRowIndex", 3);

        grid.InvokePrivate("OnApplyingBatchedDataSourceChange");

        Assert.Equal(-1, grid.GetPrivateField<int>("_hoveredDeleteRowIndex"));
    }
}
