using System.ComponentModel;
using System.Windows.Forms;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;

namespace ErikwnkWFUI.Tests;

/// <summary>
/// The placeholder ("type here to add a row") must never be touched by
/// copy/cut/paste/delete/insert, even when it's part of the current
/// selection - these all now go through the single IsPlaceholderRowIndex
/// helper instead of each having its own, sometimes-disagreeing check.
/// </summary>
public class DataGridViewPlaceholderExclusionTests
{
    [Fact]
    public void Copy_PlaceholderPartOfSelection_IsExcludedFromClipboardText()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

        int placeholderIndex = grid.Rows.Count - 1;
        GridTestHelpers.SelectCells(grid, (0, 0), (placeholderIndex, 0));

        DataObject clipboardContent = grid.GetClipboardContent();
        string text = ((string)clipboardContent.GetData(DataFormats.Text)!).Trim();

        // Only "A" - no extra blank line for the placeholder row.
        Assert.Equal("A", text);
    }

    [Fact]
    public void Paste_OnlyPlaceholderSelected_AppendsAtTheEndInsteadOfWritingIntoIt()
    {
        StaThread.Run(() =>
        {
            // Regression test: PasteFromClipboard used to deliberately treat
            // the placeholder's already-grown (but still pending/empty)
            // list slot as a legitimate paste target, once selecting it had
            // triggered IBindingList.AddNew(). That's the opposite of what's
            // wanted: the auto-row is purely a visual way to add rows by
            // typing, and every command should only ever act on real rows -
            // pasting with just the placeholder "selected" now behaves the
            // same as pasting with nothing selected at all (append at the
            // end), not "write into the placeholder".
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

            int placeholderIndex = grid.Rows.Count - 1;
            grid.CurrentCell = grid.Rows[placeholderIndex].Cells[0];
            Assert.Equal(3, items.Count); // confirms the pending add actually happened

            GridTestHelpers.SelectCells(grid, (placeholderIndex, 0));
            Clipboard.SetText("New\t99");

            grid.InvokePrivate("PasteFromClipboard");

            Assert.Equal("A", items[0].Name);
            Assert.Equal("B", items[1].Name);
            Assert.Equal("New", items[2].Name);
            Assert.Equal(99, items[2].Value);
        });
    }

    [Fact]
    public void Paste_RealRowsPlusPlaceholderSelected_OnlyOverwritesTheRealRows()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);

            int placeholderIndex = grid.Rows.Count - 1;
            GridTestHelpers.SelectCells(grid, (0, 0), (1, 0), (placeholderIndex, 0));
            Clipboard.SetText("X\t10\nY\t20");

            grid.InvokePrivate("PasteFromClipboard");

            // The placeholder being part of the selection must not turn
            // into a third pasted/inserted row - only "A" and "B" get
            // overwritten, "C" is untouched, and no stray row appears.
            Assert.Equal(3, items.Count);
            Assert.Equal("X", items[0].Name);
            Assert.Equal("Y", items[1].Name);
            Assert.Equal("C", items[2].Name);
        });
    }
}
