using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;
using WfuiReadOnlyDataGridView = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// Column resizing is hand-rolled (OnCellMouseDown/OnCellMouseMove) instead
/// of using AllowUserToResizeColumns - DataGridView's own native resize-drag
/// was confirmed live to only move a guideline and apply the real width
/// once, on mouse-up, unlike ListView's (backed by comctl32's own Header
/// control, which repaints continuously on its own). The actual drag reads
/// the live screen cursor position (Cursor.Position) and isn't practical to
/// drive headlessly, same as the reorder drag in
/// DataGridViewColumnReorderTests - these tests instead cover the
/// resizable-column API the drag is gated behind.
///
/// Is/Set/AllowColumnResizable are thin wrappers over the shared
/// ColumnFeatureSwitch, and TryGetColumnAtBorder over the shared
/// ColumnLayoutMath (see ColumnFeatureSwitchTests/ColumnLayoutMathTests, in
/// the parent Controls test folder) - the full scenario matrix for both is
/// covered there once instead of per control; this file keeps just one
/// test each confirming this control's own methods actually delegate to
/// them.
/// </summary>
public class DataGridViewColumnResizeTests
{
    [Fact]
    public void SetColumnResizable_False_MakesOnlyThatColumnNotResizable()
    {
        // Tracked separately from the native DataGridViewColumn.Resizable -
        // see AllowColumnResizing's own remarks on why that property can't
        // be reused here the way ListView reuses nothing native at all.
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);

        grid.SetColumnResizable(0, false);

        Assert.False(grid.IsColumnResizable(0));
        Assert.True(grid.IsColumnResizable(1));

        // Reversible, same as ListView's own SetColumnResizable.
        grid.SetColumnResizable(0, true);
        Assert.True(grid.IsColumnResizable(0));
    }

    [Fact]
    public void NativeAllowUserToResizeColumns_StaysFalse()
    {
        // Resizing is entirely hand-rolled so it can repaint live while
        // dragging (see AllowColumnResizing's own remarks) - the native
        // switch must stay off, or WinForms' own non-live resize would
        // kick in as well/instead.
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);

        Assert.False(grid.AllowUserToResizeColumns);
    }

    [Fact]
    public void ResizeInProgress_SuppressesTheHeaderClickSortThatWouldOtherwiseFollow()
    {
        // Regression test: DataGridView's own click detection still saw a
        // MouseDown followed by a MouseUp on the same header cell after a
        // resize (hand-rolled - see AllowColumnResizing's own remarks),
        // since nothing ever told it that gesture was actually a resize -
        // releasing the mouse after resizing a column used to also cycle
        // that column's sort as a side effect.
        //
        // _sortedColumnIndex, not HeaderCell.SortGlyphDirection, is the
        // actual source of truth read back here - the glyph OnCellPainting
        // draws comes from that field (SortGlyphDirection is only ever set
        // for AT/screen-reader purposes, see CycleSort's own remarks on it).
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("B", 2), ("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);

        grid.SetPrivateField("_suppressNextHeaderClickSort", true);
        grid.InvokePrivate("OnColumnHeaderMouseClick",
            new DataGridViewCellMouseEventArgs(0, -1, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)));

        Assert.Equal(-1, grid.GetPrivateField<int>("_sortedColumnIndex"));
        // The flag is consumed by the one click it was meant to suppress -
        // an unrelated later click must still sort normally.
        Assert.False(grid.GetPrivateField<bool>("_suppressNextHeaderClickSort"));
    }

    [Fact]
    public void OrdinaryHeaderClick_StillSortsNormally()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("B", 2), ("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);

        grid.InvokePrivate("OnColumnHeaderMouseClick",
            new DataGridViewCellMouseEventArgs(0, -1, 0, 0, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)));

        Assert.Equal(0, grid.GetPrivateField<int>("_sortedColumnIndex"));
        Assert.Equal(SortOrder.Ascending, grid.GetPrivateField<SortOrder>("_sortOrder"));
    }

    [Fact]
    public void DeleteRowColumn_IsNotResizable()
    {
        // The delete-row column has always fixed its own width - confirms
        // ShowDeleteRowColumn's SetColumnResizable(..., false) call keeps
        // it locked under the hand-rolled resize too.
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

        grid.ShowDeleteRowColumn = true;

        Assert.False(grid.IsColumnResizable(grid.Columns["__deleteRow"]!.Index));
        Assert.True(grid.IsColumnResizable(grid.Columns["Name"]!.Index));
    }

    // TryGetColumnAtBorder itself takes a plain logical x, unlike the
    // handlers that call it (OnMouseDown/OnMouseMove, both read the live
    // Cursor.Position - see the class remarks on why those aren't tested
    // here) - so the actual border-detection math IS practical to drive
    // headlessly, via reflection.
    [Fact]
    public void TryGetColumnAtBorder_WithinGripToleranceOfAColumnsRightEdge_ReturnsThatColumn()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.Columns["Name"]!.Width = 100;

        object?[] args = { 100, null };
        bool found = grid.InvokePrivate<bool>("TryGetColumnAtBorder", args);

        Assert.True(found);
        Assert.Same(grid.Columns["Name"], args[1]);
    }

    [Fact]
    public void EndColumnResize_ResetsResizeStateAndReleasesCaptureAndCursor()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiReadOnlyDataGridView grid = GridTestHelpers.CreateReadOnlyGrid(items);
        grid.SetPrivateField("_isResizingColumn", true);
        grid.SetPrivateField("_resizeColumnIndex", 0);
        grid.Capture = true;
        grid.Cursor = Cursors.VSplit;

        grid.InvokePrivate("EndColumnResize");

        Assert.False(grid.GetPrivateField<bool>("_isResizingColumn"));
        Assert.Equal(-1, grid.GetPrivateField<int>("_resizeColumnIndex"));
        Assert.False(grid.Capture);
        Assert.Equal(Cursors.Default, grid.Cursor);
    }
}
