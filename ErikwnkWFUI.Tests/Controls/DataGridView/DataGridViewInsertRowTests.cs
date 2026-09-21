using System.ComponentModel;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>InsertBlankRow, invoked via reflection - only ever reached from the "Insert row above/below" context menu items in real use.</summary>
public class DataGridViewInsertRowTests
{
    [Fact]
    public void InsertBlankRow_Above_InsertsAtTheGivenIndex()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        grid.InvokePrivate("InsertBlankRow", 1, true);

        Assert.Equal(4, items.Count);
        Assert.Equal("A", items[0].Name);
        Assert.Equal("", items[1].Name); // the new blank row
        Assert.Equal("B", items[2].Name);
        Assert.Equal("C", items[3].Name);
    }

    [Fact]
    public void InsertBlankRow_Below_InsertsAfterTheGivenIndex()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        grid.InvokePrivate("InsertBlankRow", 1, false);

        Assert.Equal(4, items.Count);
        Assert.Equal("A", items[0].Name);
        Assert.Equal("B", items[1].Name);
        Assert.Equal("", items[2].Name); // the new blank row
        Assert.Equal("C", items[3].Name);
    }

    [Fact]
    public void InsertBlankRow_ClickOnPlaceholder_DoesNotDoubleInsert()
    {
        // Simulates what a real click on the "type here to add a row"
        // placeholder already does to the bound list on its own, before
        // InsertBlankRow ever runs (see InsertBlankRow's own comment) -
        // right-clicking it, then choosing "Insert row above", must not
        // insert a second row for the one click. _contextMenuRowWasPlaceholder/
        // _contextMenuRowIndex are set directly (see PrivateReflection) rather
        // than through a real OnCellMouseDown - IsNewRow flips to false for
        // the row AddNew() just committed, but the grid then grows a FRESH
        // placeholder after it, so which Rows index actually reports
        // IsNewRow==true after a raw IBindingList.AddNew() call doesn't
        // reliably line up with what a real placeholder click produces.
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        object addedItem = ((IBindingList)items).AddNew()!;
        int insertedIndex = items.Count - 1;

        grid.SetPrivateField("_contextMenuRowWasPlaceholder", true);
        grid.SetPrivateField("_contextMenuRowIndex", insertedIndex);

        grid.InvokePrivate("InsertBlankRow", insertedIndex, true);

        Assert.Equal(3, items.Count);
        Assert.Same(addedItem, items[insertedIndex]);
    }

    [Fact]
    public void InsertBlankRow_RowIndexDiffersFromClickedPlaceholder_StillInserts()
    {
        // Regression test: right-clicking the placeholder while it's part
        // of a larger selection that also includes real rows used to block
        // "Insert row above/below" entirely for the real row the selection
        // actually resolved to (a DIFFERENT row than the one clicked), even
        // though the menu item was shown enabled.
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3), ("D", 4));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        // The placeholder (row 4) is what got right-clicked...
        grid.SetPrivateField("_contextMenuRowWasPlaceholder", true);
        grid.SetPrivateField("_contextMenuRowIndex", 4);

        // ...but "Insert row above" resolves to a DIFFERENT, real row from
        // the selection (row 1) - the guard must not treat that the same
        // as the row that was actually clicked.
        grid.InvokePrivate("InsertBlankRow", 1, true);

        Assert.Equal(5, items.Count);
        Assert.Equal("A", items[0].Name);
        Assert.Equal("", items[1].Name); // the newly inserted blank row
        Assert.Equal("B", items[2].Name);
        Assert.Equal("C", items[3].Name);
        Assert.Equal("D", items[4].Name);
    }
}
