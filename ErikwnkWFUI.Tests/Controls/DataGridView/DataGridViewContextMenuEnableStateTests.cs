using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// BuildContextMenu's own Opening handler - the enable/disable matrix for
/// Cut/Copy/Paste/Clear/"Delete selected rows"/"Insert row above"/"Insert
/// row below", driven directly through the built menu's own protected
/// OnOpening (see ReadOnlyDataGridViewEnumerationTests' own remarks on why -
/// a real right-click isn't practical to drive headlessly). Menu items are
/// read back by their fixed Items[] position (see BuildContextMenu's own
/// Add order: Cut, Copy, Paste, Clear, separator, DeleteRows, separator,
/// InsertAbove, InsertBelow) rather than by field, since those fields are
/// private. Paste's own Clipboard.ContainsText() check means this carries
/// the same [Collection(ClipboardTestCollection.Name)] every other
/// clipboard-touching test class does.
/// </summary>
[Collection(ClipboardTestCollection.Name)]
public class DataGridViewContextMenuEnableStateTests
{
    private static (WfuiDataGridView Grid, WfuiContextMenuStrip Menu) CreateGridWithMenu(BindingList<TestItem> items)
    {
        WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        WfuiContextMenuStrip menu = (WfuiContextMenuStrip)grid.ContextMenuStrip!;
        return (grid, menu);
    }

    private static void Open(WfuiDataGridView grid, WfuiContextMenuStrip menu, int rowIndex, int columnIndex)
    {
        grid.SetPrivateField("_contextMenuRowIndex", rowIndex);
        grid.SetPrivateField("_contextMenuColumnIndex", columnIndex);
        grid.SetPrivateField("_contextMenuRowWasPlaceholder", rowIndex == grid.Rows.Count - 1 && grid.AllowUserToAddRows);
        menu.InvokePrivate("OnOpening", new CancelEventArgs());
    }

    private static ToolStripMenuItem Cut(WfuiContextMenuStrip menu) => (ToolStripMenuItem)menu.Items[0];
    private static ToolStripMenuItem Copy(WfuiContextMenuStrip menu) => (ToolStripMenuItem)menu.Items[1];
    private static ToolStripMenuItem Paste(WfuiContextMenuStrip menu) => (ToolStripMenuItem)menu.Items[2];
    private static ToolStripMenuItem Clear(WfuiContextMenuStrip menu) => (ToolStripMenuItem)menu.Items[3];
    private static ToolStripMenuItem DeleteRows(WfuiContextMenuStrip menu) => (ToolStripMenuItem)menu.Items[5];
    private static ToolStripMenuItem InsertAbove(WfuiContextMenuStrip menu) => (ToolStripMenuItem)menu.Items[7];
    private static ToolStripMenuItem InsertBelow(WfuiContextMenuStrip menu) => (ToolStripMenuItem)menu.Items[8];

    [Fact]
    public void NoSelection_DisablesEverySelectionDependentItem()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            (WfuiDataGridView grid, WfuiContextMenuStrip menu) = CreateGridWithMenu(items);
            // A freshly bound grid auto-selects Rows[0].Cells[0] as its
            // initial CurrentCell - cleared explicitly so this actually
            // tests the "nothing selected" case, not that default.
            grid.ClearSelection();
            Clipboard.Clear();

            Open(grid, menu, 0, 0);

            Assert.False(Cut(menu).Enabled);
            Assert.False(Copy(menu).Enabled);
            Assert.False(Clear(menu).Enabled);
            Assert.False(DeleteRows(menu).Enabled);
            // Insert above/below don't need a selected CELL, just a real row
            // somewhere in the selection - with nothing selected at all,
            // TryGetSelectedRowIndexRange finds none either.
            Assert.False(InsertAbove(menu).Enabled);
            Assert.False(InsertBelow(menu).Enabled);
        });
    }

    [Fact]
    public void SelectionOnARealRow_EnablesEveryRowScopedItem()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            (WfuiDataGridView grid, WfuiContextMenuStrip menu) = CreateGridWithMenu(items);
            GridTestHelpers.SelectCells(grid, (0, 0));
            Clipboard.SetText("x");

            Open(grid, menu, 0, 0);

            Assert.True(Cut(menu).Enabled);
            Assert.True(Copy(menu).Enabled);
            Assert.True(Clear(menu).Enabled);
            Assert.True(DeleteRows(menu).Enabled);
            Assert.True(InsertAbove(menu).Enabled);
            Assert.True(InsertBelow(menu).Enabled);
        });
    }

    [Fact]
    public void ReadOnlyGrid_DisablesCutClearPasteButNotCopy()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            (WfuiDataGridView grid, WfuiContextMenuStrip menu) = CreateGridWithMenu(items);
            grid.ReadOnly = true;
            GridTestHelpers.SelectCells(grid, (0, 0));
            Clipboard.SetText("x");

            Open(grid, menu, 0, 0);

            Assert.False(Cut(menu).Enabled);
            Assert.True(Copy(menu).Enabled); // reading the selection is still fine
            Assert.False(Clear(menu).Enabled);
            Assert.False(Paste(menu).Enabled);
        });
    }

    [Fact]
    public void ClipboardHasText_EnablesPasteRegardlessOfSelection()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            (WfuiDataGridView grid, WfuiContextMenuStrip menu) = CreateGridWithMenu(items);
            Clipboard.SetText("x");

            Open(grid, menu, 0, 0);

            Assert.True(Paste(menu).Enabled);
        });
    }

    [Fact]
    public void ClipboardEmpty_DisablesPaste()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            (WfuiDataGridView grid, WfuiContextMenuStrip menu) = CreateGridWithMenu(items);
            GridTestHelpers.SelectCells(grid, (0, 0));
            Clipboard.Clear();

            Open(grid, menu, 0, 0);

            Assert.False(Paste(menu).Enabled);
        });
    }

    [Fact]
    public void PlaceholderRowClicked_DisablesRowScopedItems_EvenWithSelection()
    {
        // Selecting the placeholder cell already makes WinForms call
        // IBindingList.AddNew() on the bound list (see InsertBlankRow's own
        // remarks) - it still reports IsNewRow == true until something
        // commits it, so it's still "the placeholder" for this check.
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            (WfuiDataGridView grid, WfuiContextMenuStrip menu) = CreateGridWithMenu(items);
            int placeholderIndex = grid.Rows.Count - 1;
            GridTestHelpers.SelectCells(grid, (placeholderIndex, 0));
            Clipboard.SetText("x");

            Open(grid, menu, placeholderIndex, 0);

            Assert.False(Cut(menu).Enabled);
            Assert.False(Copy(menu).Enabled);
            Assert.False(Clear(menu).Enabled);
            Assert.False(DeleteRows(menu).Enabled);
            // No real row anywhere in the selection to insert relative to.
            Assert.False(InsertAbove(menu).Enabled);
            Assert.False(InsertBelow(menu).Enabled);
        });
    }

    [Fact]
    public void PlaceholderClicked_ButSelectionAlsoReachesARealRow_StillOffersInsert()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        (WfuiDataGridView grid, WfuiContextMenuStrip menu) = CreateGridWithMenu(items);
        int placeholderIndex = grid.Rows.Count - 1;
        GridTestHelpers.SelectCells(grid, (0, 0), (placeholderIndex, 0));

        Open(grid, menu, placeholderIndex, 0);

        Assert.False(Cut(menu).Enabled); // the clicked row itself is still the placeholder
        Assert.True(InsertAbove(menu).Enabled); // but a real row IS in the selection
        Assert.True(InsertBelow(menu).Enabled);
    }

    [Fact]
    public void AllowUserToDeleteRowsFalse_KeepsDeleteRowsDisabled()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        (WfuiDataGridView grid, WfuiContextMenuStrip menu) = CreateGridWithMenu(items);
        grid.AllowUserToDeleteRows = false;
        GridTestHelpers.SelectCells(grid, (0, 0));

        Open(grid, menu, 0, 0);

        Assert.False(DeleteRows(menu).Enabled);
    }

    [Fact]
    public void AllowUserToAddRowsFalse_KeepsInsertItemsDisabled()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        (WfuiDataGridView grid, WfuiContextMenuStrip menu) = CreateGridWithMenu(items);
        grid.AllowUserToAddRows = false;
        GridTestHelpers.SelectCells(grid, (0, 0));

        Open(grid, menu, 0, 0);

        Assert.False(InsertAbove(menu).Enabled);
        Assert.False(InsertBelow(menu).Enabled);
    }

    [Fact]
    public void NoRowClicked_CancelsTheMenuEntirely()
    {
        // RowIndex -1 covers both the header and the empty space below the
        // last row - neither has a row-scoped menu to show at all.
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        (WfuiDataGridView grid, WfuiContextMenuStrip menu) = CreateGridWithMenu(items);

        grid.SetPrivateField("_contextMenuRowIndex", -1);
        grid.SetPrivateField("_contextMenuColumnIndex", -1);
        CancelEventArgs args = new CancelEventArgs();
        menu.InvokePrivate("OnOpening", args);

        Assert.True(args.Cancel);
    }

    [Fact]
    public void RightClickOnDeleteColumn_CancelsTheMenuEntirely()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        (WfuiDataGridView grid, WfuiContextMenuStrip menu) = CreateGridWithMenu(items);
        grid.ShowDeleteRowColumn = true;
        int deleteColumnIndex = grid.Columns["__deleteRow"]!.Index;

        grid.SetPrivateField("_contextMenuRowIndex", 0);
        grid.SetPrivateField("_contextMenuColumnIndex", deleteColumnIndex);
        CancelEventArgs args = new CancelEventArgs();
        menu.InvokePrivate("OnOpening", args);

        Assert.True(args.Cancel);
    }
}
