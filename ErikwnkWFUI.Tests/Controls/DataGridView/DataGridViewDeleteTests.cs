using System.ComponentModel;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>DeleteRows/DeleteItem, invoked via reflection - only ever reached from the Delete key, the context menu, or the pinned delete-row column in real use.</summary>
public class DataGridViewDeleteTests
{
    [Fact]
    public void DeleteRows_RemovesExactlyTheGivenRows()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(
            ("A", 0), ("B", 1), ("C", 2), ("D", 3), ("E", 4));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        grid.InvokePrivate("DeleteRows", new[] { 1, 3 });

        Assert.Equal(3, items.Count);
        Assert.Equal("A", items[0].Name);
        Assert.Equal("C", items[1].Name);
        Assert.Equal("E", items[2].Name);
    }

    [Fact]
    public void DeleteRows_SkipsThePlaceholderRow()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        Assert.True(grid.AllowUserToAddRows);
        int placeholderIndex = grid.Rows.Count - 1;

        grid.InvokePrivate("DeleteRows", new[] { 0, placeholderIndex });

        Assert.Single(items);
        Assert.Equal("B", items[0].Name);
    }

    [Fact]
    public void DeleteRows_PlaceholderBecameCurrentCellAsPartOfSelection_DoesNotLeaveAStrayRow()
    {
        // Regression test: moving CurrentCell onto the placeholder - which
        // a real multi-row selection spanning down to it does
        // automatically, no typing required - already makes WinForms call
        // IBindingList.AddNew() on the bound list on its own (see
        // InsertBlankRow's own comment on this). Deleting a selection that
        // includes it used to just skip that row (correctly - nothing
        // real to delete there) but never cancelled the pending add,
        // leaving a permanent, empty stray row behind in the data source.
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        int placeholderIndex = grid.Rows.Count - 1;

        grid.CurrentCell = grid.Rows[placeholderIndex].Cells[0];
        Assert.Equal(4, items.Count); // confirms the pending add actually happened

        grid.InvokePrivate("DeleteRows", new[] { 0, 1, placeholderIndex });

        Assert.Single(items);
        Assert.Equal("C", items[0].Name);
    }

    [Fact]
    public void DeleteRows_PendingAddFromDirectAddNew_DoesNotLeaveAStrayRow()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        ((IBindingList)items).AddNew();
        int pendingIndex = items.Count - 1;
        Assert.Equal(4, items.Count);

        grid.InvokePrivate("DeleteRows", new[] { 0, 1, pendingIndex });

        Assert.Single(items);
        Assert.Equal("C", items[0].Name);
    }

    [Fact]
    public void DeleteRows_DuplicateReferenceInList_RemovesOnlyTheTargetedIndex()
    {
        // Regression test: a first attempt at making DeleteRows a single
        // O(n) pass filtered by a HashSet<object> of items-to-remove
        // instead of by row index - which is wrong when the same object
        // reference is bound to two different rows, since removing "this
        // item" then removes every row bound to it, not just the one that
        // was actually selected.
        TestItem duplicate = new TestItem("Dup", 1);
        BindingList<TestItem> items = new BindingList<TestItem> { new TestItem("A", 0), duplicate, duplicate };
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        grid.InvokePrivate("DeleteRows", new[] { 1 });

        Assert.Equal(2, items.Count);
        Assert.Equal("A", items[0].Name);
        Assert.Same(duplicate, items[1]);
    }

    [Fact]
    public void DeleteItem_RemovesTheCapturedItem_EvenAfterAnotherDeleteShiftedTheList()
    {
        // Regression test: the delete-glyph column used to defer deletion
        // (via BeginInvoke) using a raw row index captured at click time.
        // Two rapid clicks on different rows could queue two deferred
        // deletes, and the second one would resolve against the list AFTER
        // the first delete had already shifted it, deleting the wrong row.
        // DeleteItem instead takes the actual bound item captured at click
        // time, so it isn't affected by any shift that happens in between.
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2), ("C", 3));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        object capturedItem = grid.Rows[1].DataBoundItem!;
        Assert.Same(items[1], capturedItem);

        // A second, faster click's delete completes first, shifting the list.
        grid.InvokePrivate("DeleteItem", items[0]);
        Assert.Equal(2, items.Count);

        // The first click's deferred delete must still remove the item it
        // actually captured ("B"), not whatever now sits at its old index.
        grid.InvokePrivate("DeleteItem", capturedItem);

        Assert.Single(items);
        Assert.Equal("C", items[0].Name);
    }

    [Fact]
    public void DeleteItem_NullItem_DoesNothing()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);

        grid.InvokePrivate("DeleteItem", (object?)null);

        Assert.Single(items);
    }
}
