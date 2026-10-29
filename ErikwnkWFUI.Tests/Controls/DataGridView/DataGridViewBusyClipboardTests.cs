using System.ComponentModel;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// A cut whose copy did not work (another program holds the clipboard) must clear and remove nothing and say nothing: the cells and rows
/// would be gone without being anywhere else. Copy and paste just do nothing. [Collection]: the clipboard of the tests is shared.
/// </summary>
[Collection(ClipboardTestCollection.Name)]
public class DataGridViewBusyClipboardTests
{
    [Fact]
    public void CutCells_WhileTheClipboardIsBusy_ClearsNothing_AndShowsNoMessage()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0), (1, 0));

            using (BusyClipboard.Use())
                Assert.Null(Record.Exception(() => grid.InvokePrivate("CutSelectionToClipboard")));

            Assert.Equal("A", items[0].Name);
            Assert.Equal("B", items[1].Name);
            Assert.Empty(grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void CutRows_WhileTheClipboardIsBusy_RemovesNoRow_AndShowsNoMessage()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0));

            using (BusyClipboard.Use())
                Assert.Null(Record.Exception(() => grid.InvokePrivate("CutSelectedRows")));

            Assert.Equal(2, items.Count);
            Assert.Empty(grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void PasteAndTheStateOfThePasteItem_WhileTheClipboardIsBusy_DoNothing_AndDoNotThrow()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0));

            using (BusyClipboard.Use())
                Assert.Null(Record.Exception(() => grid.InvokePrivate("PasteFromClipboard")));

            Assert.Equal("A", items[0].Name);
        });
    }

    [Fact]
    public void CopyingTheSelection_WhileTheClipboardIsBusy_DoesNotThrow()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0));

            using (BusyClipboard.Use())
                Assert.Null(Record.Exception(() => grid.InvokePrivate("CopySelectionToClipboard", false)));
        });
    }
}
