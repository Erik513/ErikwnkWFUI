using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// PasteFromClipboard, invoked directly via reflection (see
/// PrivateReflection) since it's only ever reached from Ctrl+V or the
/// context menu in real use. Every test runs its clipboard-touching part on
/// an STA thread (see StaThread) - Clipboard access throws off the default
/// MTA thread xUnit runs tests on.
/// </summary>
[Collection(ClipboardTestCollection.Name)]
public class DataGridViewPasteTests
{
    [Fact]
    public void Paste_SingleCellSelected_OverwritesRowInPlace()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

            GridTestHelpers.SelectCells(grid, (0, 0));
            Clipboard.SetText("Pasted\t99");

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Equal(3, items.Count);
            Assert.Equal("Pasted", items[0].Name);
            Assert.Equal(99, items[0].Value);
            Assert.Equal("B", items[1].Name);
            Assert.Equal("C", items[2].Name);
        });
    }

    [Fact]
    public void Paste_TallerThanSelection_InsertsExtraRowsRightAfterSelection()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

            // Only row 0 selected, but the pasted block is 2 rows tall - the
            // second pasted row must be INSERTED right after row 0, not
            // overwrite the existing row 1 ("B").
            GridTestHelpers.SelectCells(grid, (0, 0));
            Clipboard.SetText("X\t10\nY\t20");

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Equal(4, items.Count);
            Assert.Equal("X", items[0].Name);
            Assert.Equal(10, items[0].Value);
            Assert.Equal("Y", items[1].Name);
            Assert.Equal(20, items[1].Value);
            Assert.Equal("B", items[2].Name);
            Assert.Equal("C", items[3].Name);
        });
    }

    [Fact]
    public void Paste_NonContiguousRowSelection_OverwritesOnlyTheSelectedRows()
    {
        StaThread.Run(() =>
        {
            // Regression test: a paste into a non-contiguous selection used
            // to overwrite rows sequentially from the top of the selection
            // (clobbering rows that were never selected) instead of the
            // actual selected rows.
            BindingList<TestItem> items = GridTestHelpers.CreateItems(
                ("R0", 0), ("R1", 1), ("R2", 2), ("R3", 3), ("R4", 4), ("R5", 5));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

            GridTestHelpers.SelectCells(grid, (0, 0), (5, 0));
            Clipboard.SetText("X\t100\nY\t200");

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Equal(6, items.Count);
            Assert.Equal("X", items[0].Name);
            Assert.Equal(100, items[0].Value);
            Assert.Equal("Y", items[5].Name);
            Assert.Equal(200, items[5].Value);

            // Rows 1-4 were never selected - a correct paste must leave
            // them completely untouched.
            Assert.Equal("R1", items[1].Name);
            Assert.Equal(1, items[1].Value);
            Assert.Equal("R2", items[2].Name);
            Assert.Equal("R3", items[3].Name);
            Assert.Equal("R4", items[4].Name);
        });
    }

    [Fact]
    public void Paste_ReadOnlyColumn_IsSkipped()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
            grid.Columns["Name"]!.ReadOnly = true;

            GridTestHelpers.SelectCells(grid, (0, 0));
            Clipboard.SetText("ShouldNotWrite\t42");

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Equal("A", items[0].Name);
            Assert.Equal(42, items[0].Value);
        });
    }

    [Fact]
    public void Paste_TextIntoNonStringColumn_ConvertsToThePropertyType()
    {
        // Regression test: pasted clipboard text is always a raw string,
        // but PropertyInfo.SetValue doesn't convert it to match the bound
        // property's own type on its own - pasting into any non-string
        // column (Value here is an int) used to throw, aborting the paste.
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

            GridTestHelpers.SelectCells(grid, (0, 1));
            Clipboard.SetText("42");

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Equal(42, items[0].Value);
        });
    }

    [Fact]
    public void Paste_TextThatCannotConvert_SkipsThatCellInsteadOfThrowing()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

            GridTestHelpers.SelectCells(grid, (0, 0));
            Clipboard.SetText("Pasted\tnotANumber");

            Exception? thrown = Record.Exception(() => grid.InvokePrivate("PasteFromClipboard"));

            Assert.Null(thrown);
            Assert.Equal("Pasted", items[0].Name);
            Assert.Equal(1, items[0].Value); // left untouched, not crashed
        });
    }

    [Fact]
    public void Paste_SingleCellCopiedIntoMultiCellSelection_FillsEverySelectedCell()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

            // Copying exactly one cell into a selection spanning several
            // (here: rows 0 and 2, same column) fills all of them with
            // that value - standard spreadsheet "fill" behavior - instead
            // of anchoring at the top-left and writing only there.
            GridTestHelpers.SelectCells(grid, (0, 0), (2, 0));
            Clipboard.SetText("Filled");

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Equal(3, items.Count);
            Assert.Equal("Filled", items[0].Name);
            Assert.Equal("B", items[1].Name); // never selected, untouched
            Assert.Equal("Filled", items[2].Name);
        });
    }

    [Fact]
    public void Paste_SingleCellCopiedAcrossDifferentColumns_FillsEachWithConvertedValue()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

            // Both columns of both rows selected - "42" gets converted per
            // column (string for Name, int for Value), same as a normal
            // paste already does.
            GridTestHelpers.SelectCells(grid, (0, 0), (0, 1), (1, 0), (1, 1));
            Clipboard.SetText("42");

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Equal("42", items[0].Name);
            Assert.Equal(42, items[0].Value);
            Assert.Equal("42", items[1].Name);
            Assert.Equal(42, items[1].Value);
        });
    }

    [Fact]
    public void Paste_MultiCellCopyIntoSingleCellSelection_AnchorsAndExtendsAsBefore()
    {
        // Fill only ever applies when exactly one cell was copied - copying
        // more than one and pasting into a single selected cell must keep
        // behaving like a normal anchored paste (extending as needed),
        // never "tiling"/repeating the copied block into a larger area.
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

            GridTestHelpers.SelectCells(grid, (0, 0));
            Clipboard.SetText("X\t10\nY\t20");

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Equal(3, items.Count);
            Assert.Equal("X", items[0].Name);
            Assert.Equal(10, items[0].Value);
            Assert.Equal("Y", items[1].Name);
            Assert.Equal(20, items[1].Value);
            Assert.Equal("B", items[2].Name);
        });
    }

    [Fact]
    public void Paste_FillSelectionIncludesPlaceholder_SkipsItAndCancelsThePendingAdd()
    {
        StaThread.Run(() =>
        {
            // Selecting the placeholder cell (without ever routing
            // CurrentCell through it first) is what a real drag/shift-select
            // reaching down that far produces, and matches the equivalent,
            // already-covered non-fill scenario in
            // DataGridViewPlaceholderExclusionTests - it's enough on its own
            // to make WinForms call IBindingList.AddNew() on the bound list.
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

            int placeholderIndex = grid.Rows.Count - 1;
            GridTestHelpers.SelectCells(grid, (0, 0), (1, 0), (placeholderIndex, 0));
            Clipboard.SetText("Filled");

            grid.InvokePrivate("PasteFromClipboard");

            // Only the two real rows get filled - the placeholder is never
            // turned into a third row.
            Assert.Equal(2, items.Count);
            Assert.Equal("Filled", items[0].Name);
            Assert.Equal("Filled", items[1].Name);
        });
    }

    [Fact]
    public void Paste_NoClipboardText_DoesNothing()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

            GridTestHelpers.SelectCells(grid, (0, 0));
            Clipboard.Clear();

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Single(items);
            Assert.Equal("A", items[0].Name);
        });
    }
}
