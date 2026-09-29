using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// ActionConfirmation messages for DataGridView's own editing operations
/// (cut, paste, clear, insert row, delete row/rows) - see
/// ReadOnlyDataGridViewCopyConfirmationTests for the base "copy" behavior
/// these build on. TestableDataGridView.ActionConfirmationMessages spies on
/// OnActionConfirmation (see its own comment) so these assert exactly what
/// got shown, without needing a real Form for ToastForm to attach to.
/// Cut and Paste touch the real clipboard, so this carries the same
/// [Collection(ClipboardTestCollection.Name)] every other clipboard-touching
/// test class does (see ClipboardTestCollection).
/// </summary>
[Collection(ClipboardTestCollection.Name)]
public class DataGridViewActionConfirmationTests
{
    [Fact]
    public void ClearSelectedCellValues_SingleCell_ShowsSingularMessage()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
        GridTestHelpers.SelectCells(grid, (0, 0));

        grid.InvokePrivate("ClearSelectedCellValues");

        Assert.Equal(new[] { "Cell cleared" }, grid.ActionConfirmationMessages);
    }

    [Fact]
    public void ClearSelectedCellValues_MultipleCells_ShowsFormattedPluralMessage()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
        GridTestHelpers.SelectCells(grid, (0, 0), (0, 1), (1, 0), (1, 1));

        grid.InvokePrivate("ClearSelectedCellValues");

        Assert.Equal(new[] { "4 cells cleared" }, grid.ActionConfirmationMessages);
    }

    [Fact]
    public void ClearSelectedCellValues_ReadOnlyColumnOnly_ShowsNoMessage()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
        grid.Columns["Name"]!.ReadOnly = true;
        GridTestHelpers.SelectCells(grid, (0, 0));

        grid.InvokePrivate("ClearSelectedCellValues");

        Assert.Empty(grid.ActionConfirmationMessages);
    }

    // CutSelectionToClipboard = copy + clear, but must show neither
    // "copied" nor "cleared" - only its own single "cut" message. This is
    // the actual behavior the wording question in this session was about.
    [Fact]
    public void Cut_SingleCell_ShowsOnlyASingularCutMessage_NotCopiedOrCleared()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0));

            grid.InvokePrivate("CutSelectionToClipboard");

            Assert.Equal(new[] { "Cell cut" }, grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void Cut_MultipleCells_ShowsOnlyAFormattedPluralCutMessage()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0), (0, 1), (1, 0), (1, 1));

            grid.InvokePrivate("CutSelectionToClipboard");

            Assert.Equal(new[] { "4 cells cut" }, grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void CutRows_PutsTheWholeRowOnTheClipboard_RemovesIt_AndShowsOnlyACutMessage()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            // Only the first cell of row 0 selected - the whole row must
            // still end up on the clipboard.
            GridTestHelpers.SelectCells(grid, (0, 0));

            grid.InvokePrivate("CutSelectedRows");

            Assert.Equal("A	1", Clipboard.GetText().Trim());
            Assert.Single(items);
            Assert.Equal("B", items[0].Name);
            Assert.Equal(new[] { "Row cut" }, grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void CutRows_MultipleRows_ShowsAFormattedPluralMessage()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0), (1, 0));

            grid.InvokePrivate("CutSelectedRows");

            Assert.Single(items);
            Assert.Equal(new[] { "2 rows cut" }, grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void Cut_NoSelection_ShowsNoMessage()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            grid.ClearSelection();

            grid.InvokePrivate("CutSelectionToClipboard");

            Assert.Empty(grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void Cut_ReadOnlyColumnOnly_FallsBackToAPlainCopyWithItsOwnConfirmation()
    {
        // Nothing can actually be cleared (the only selected cell is
        // read-only), so there's no "cut" to confirm - but the values
        // still land on the clipboard exactly like a plain Ctrl+C would,
        // which must not happen silently.
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            grid.Columns["Name"]!.ReadOnly = true;
            GridTestHelpers.SelectCells(grid, (0, 0));

            grid.InvokePrivate("CutSelectionToClipboard");

            Assert.Equal(new[] { "Cell copied" }, grid.ActionConfirmationMessages);
            Assert.Equal("A", items[0].Name); // untouched, nothing was cleared
        });
    }

    [Fact]
    public void PasteFromClipboard_SingleRow_ShowsSingularRowPastedMessage()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0));
            Clipboard.SetText("Pasted\t99");

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Equal(new[] { "Row pasted" }, grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void PasteFromClipboard_MultipleRows_ShowsFormattedPluralRowsPastedMessage()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0));
            Clipboard.SetText("X\t10\nY\t20");

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Equal(new[] { "2 rows pasted" }, grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void PasteFromClipboard_FillIntoMultiCellSelection_ShowsCellsPastedMessage()
    {
        // The "fill" path (FillCellsWithValue): a single copied cell pasted
        // into a larger selection - a different message ("cells pasted",
        // always plural since fill only ever applies to more than one
        // target cell) than the row-anchored paste above.
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0), (2, 0));
            Clipboard.SetText("Filled");

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Equal(new[] { "2 cells pasted" }, grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void PasteFromClipboard_NoClipboardText_ShowsNoMessage()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0));
            Clipboard.Clear();

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Empty(grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void InsertBlankRow_ShowsRowInsertedMessage()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        grid.InvokePrivate("InsertBlankRow", 0, true);

        Assert.Equal(new[] { "Row inserted" }, grid.ActionConfirmationMessages);
    }

    [Fact]
    public void DeleteItem_ShowsRowDeletedMessage()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        grid.InvokePrivate("DeleteItem", items[0]);

        Assert.Equal(new[] { "Row deleted" }, grid.ActionConfirmationMessages);
    }

    [Fact]
    public void DeleteItem_NullItem_ShowsNoMessage()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        grid.InvokePrivate("DeleteItem", (object?)null);

        Assert.Empty(grid.ActionConfirmationMessages);
    }

    [Fact]
    public void DeleteRows_SingleRow_ShowsSingularRowDeletedMessage()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        grid.InvokePrivate("DeleteRows", new[] { 0 });

        Assert.Equal(new[] { "Row deleted" }, grid.ActionConfirmationMessages);
    }

    [Fact]
    public void DeleteRows_MultipleRows_ShowsFormattedPluralRowsDeletedMessage()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 0), ("B", 1), ("C", 2), ("D", 3));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        grid.InvokePrivate("DeleteRows", new[] { 1, 3 });

        Assert.Equal(new[] { "2 rows deleted" }, grid.ActionConfirmationMessages);
    }

    [Fact]
    public void DeleteRows_OnlyThePlaceholder_CancelsItButShowsNoMessage()
    {
        // No real row was actually removed - just a pending add cancelled -
        // so there's nothing to confirm as "deleted".
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
        int placeholderIndex = grid.Rows.Count - 1;

        grid.InvokePrivate("DeleteRows", new[] { placeholderIndex });

        Assert.Empty(grid.ActionConfirmationMessages);
    }
}
