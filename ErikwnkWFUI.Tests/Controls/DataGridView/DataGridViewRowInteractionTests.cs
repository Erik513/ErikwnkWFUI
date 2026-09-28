using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// OnRowLeave (cancels an untouched "type here to add a row" placeholder
/// when the user leaves it without typing) and OnCellMouseDown (right-click
/// row capture/selection for the context menu) - both invoked through
/// TestableDataGridView, which exists specifically to drive the latter
/// (RaiseCellMouseDown) without a real mouse.
/// </summary>
public class DataGridViewRowInteractionTests
{
    // OnRowLeave's own cancellation is deferred via BeginInvoke (see its
    // own remarks on why) - Application.DoEvents() pumps this thread's
    // message queue once, which is enough to run a callback BeginInvoke
    // already posted to it (no Application.Run/real message loop needed
    // for that - the queue exists the moment this control's handle does).
    [Fact]
    public void OnRowLeave_LeavingAnUntouchedPlaceholder_CancelsThePendingAdd()
    {
        // The cleanup's own ClearSelection()/CurrentCell = null (see
        // OnRowLeave's remarks on why) isn't asserted here - confirmed live
        // that pumping the rest of this thread's queued messages afterward
        // (Application.DoEvents processes everything queued, not just the
        // one BeginInvoke callback) lets WinForms' own post-removal
        // auto-pick of a new CurrentCell run too, which still lands back on
        // the first cell of the remaining real row either way. The part
        // that actually matters - and is reliably observable - is that the
        // untouched pending add itself gets cancelled.
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        int placeholderIndex = grid.Rows.Count - 1;
        grid.CurrentCell = grid.Rows[placeholderIndex].Cells[0];
        Assert.Equal(2, items.Count); // the pending add really happened

        grid.CurrentCell = grid.Rows[0].Cells[0]; // leaves the placeholder row
        Application.DoEvents();

        Assert.Single(items);
        Assert.Equal("A", items[0].Name);
    }

    [Fact]
    public void OnRowLeave_LeavingAnOrdinaryRow_DoesNothing()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        grid.CurrentCell = grid.Rows[0].Cells[0];
        grid.CurrentCell = grid.Rows[1].Cells[0]; // leaves row 0, an ordinary real row
        Application.DoEvents();

        Assert.Equal(2, items.Count);
        Assert.NotNull(grid.CurrentCell);
        Assert.Equal(1, grid.CurrentCell!.RowIndex);
    }

    // Regression test: OnCellMouseDown used to check IsRowSelected AFTER
    // base.OnCellMouseDown already ran - but this control's SelectionMode
    // is CellSelect, whose own native mouse-down handling (run by that same
    // base call) already selects the single clicked cell as ITS OWN default
    // behavior first. That made the just-clicked row's own IsRowSelected
    // always already true by the time it was checked, silently skipping
    // SelectRow below for EVERY previously-unselected row and leaving just
    // the one clicked cell selected instead of the whole row. Fixed by
    // capturing IsRowSelected BEFORE the base call instead.
    [Fact]
    public void OnCellMouseDown_RightClickOnUnselectedRow_ReplacesSelectionWithThatRow()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
        GridTestHelpers.SelectRow(grid, 0);

        grid.RaiseCellMouseDown(1, 1, MouseButtons.Right);

        Assert.Equal(1, grid.GetPrivateField<int>("_contextMenuRowIndex"));
        Assert.Equal(1, grid.GetPrivateField<int>("_contextMenuColumnIndex"));
        Assert.False(grid.GetPrivateField<bool>("_contextMenuRowWasPlaceholder"));
        Assert.True(grid.Rows[1].Cells[0].Selected);
        Assert.True(grid.Rows[1].Cells[1].Selected);
        Assert.False(grid.Rows[0].Cells[0].Selected); // the old selection was replaced
        Assert.False(grid.Rows[0].Cells[1].Selected);
        Assert.Equal(1, grid.CurrentCell!.ColumnIndex); // the actually-clicked column, not just the first one
    }

    [Fact]
    public void OnCellMouseDown_RightClickWithinAnExistingMultiRowSelection_LeavesItIntact()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
        GridTestHelpers.SelectRows(grid, 0, 2); // row 1 deliberately left out

        grid.RaiseCellMouseDown(0, 0, MouseButtons.Right); // right-clicking row 0, already part of the selection

        Assert.True(grid.Rows[0].Cells[0].Selected);
        Assert.True(grid.Rows[2].Cells[0].Selected);
        Assert.False(grid.Rows[1].Cells[0].Selected); // never touched, confirms the selection wasn't rebuilt
    }

    [Fact]
    public void OnCellMouseDown_RightClickOnASystemColumn_DoesNotChangeSelection()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
        grid.ShowDeleteRowColumn = true;
        GridTestHelpers.SelectRow(grid, 0);
        int deleteColumnIndex = grid.Columns["__deleteRow"]!.Index;

        grid.RaiseCellMouseDown(deleteColumnIndex, 1, MouseButtons.Right);

        Assert.True(grid.Rows[0].Cells[0].Selected); // the old selection is left alone
        Assert.False(grid.Rows[1].Cells[0].Selected);
    }

    [Fact]
    public void OnCellMouseDown_RightClickOnThePlaceholderRow_MarksItAsSuch()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
        int placeholderIndex = grid.Rows.Count - 1;

        grid.RaiseCellMouseDown(0, placeholderIndex, MouseButtons.Right);

        Assert.True(grid.GetPrivateField<bool>("_contextMenuRowWasPlaceholder"));
        Assert.Equal(placeholderIndex, grid.GetPrivateField<int>("_contextMenuRowIndex"));
    }

    [Fact]
    public void OnCellMouseDown_LeftClick_DoesNotUpdateContextMenuState()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
        grid.SetPrivateField("_contextMenuRowIndex", 5);
        grid.SetPrivateField("_contextMenuColumnIndex", 5);

        grid.RaiseCellMouseDown(0, 0, MouseButtons.Left);

        Assert.Equal(5, grid.GetPrivateField<int>("_contextMenuRowIndex"));
        Assert.Equal(5, grid.GetPrivateField<int>("_contextMenuColumnIndex"));
    }
}
