using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// OnColumnClick, invoked via reflection with a real ColumnClickEventArgs -
/// only ever reached from a click on the native header in real use. Same
/// design as ReadOnlyDataGridView's own CycleSort, adapted for this
/// control's Items collection instead of a DataSource.
/// </summary>
public class ListViewSortTests
{
    [Fact]
    public void HeaderClick_CyclesAscendingDescendingThenOriginalOrder()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100));
        listView.Items.Add(new ListViewItem("B"));
        listView.Items.Add(new ListViewItem("A"));
        listView.Items.Add(new ListViewItem("C"));

        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(0));
        Assert.Equal("A", listView.Items[0].Text);
        Assert.Equal("B", listView.Items[1].Text);
        Assert.Equal("C", listView.Items[2].Text);
        Assert.Equal(0, listView.GetPrivateField<int>("_sortedColumnIndex"));
        Assert.Equal(SortOrder.Ascending, listView.GetPrivateField<SortOrder>("_sortOrder"));

        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(0));
        Assert.Equal("C", listView.Items[0].Text);
        Assert.Equal("B", listView.Items[1].Text);
        Assert.Equal("A", listView.Items[2].Text);
        Assert.Equal(SortOrder.Descending, listView.GetPrivateField<SortOrder>("_sortOrder"));

        // Back to the order items were in the first time this was ever
        // called (B, A, C) - not re-sorted ascending again.
        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(0));
        Assert.Equal("B", listView.Items[0].Text);
        Assert.Equal("A", listView.Items[1].Text);
        Assert.Equal("C", listView.Items[2].Text);
        Assert.Equal(-1, listView.GetPrivateField<int>("_sortedColumnIndex"));
    }

    [Fact]
    public void HeaderClick_DifferentColumn_StartsThatColumnFreshAtAscending()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100), ("Status", 100));
        listView.Items.Add(new ListViewItem(new[] { "B", "Y" }));
        listView.Items.Add(new ListViewItem(new[] { "A", "X" }));

        // Column 0 twice - ascending, then descending.
        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(0));
        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(0));
        Assert.Equal(SortOrder.Descending, listView.GetPrivateField<SortOrder>("_sortOrder"));

        // Switching to column 1 must start it fresh at ascending, not
        // continue wherever column 0's own cycle left off.
        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(1));
        Assert.Equal(1, listView.GetPrivateField<int>("_sortedColumnIndex"));
        Assert.Equal(SortOrder.Ascending, listView.GetPrivateField<SortOrder>("_sortOrder"));
    }

    [Fact]
    public void HeaderClick_UsesNaturalSortOrder_Row2SortsBeforeRow10()
    {
        // A plain string/numeric comparer would put "Row 10" right after
        // "Row 1" and before "Row 2" - StrCmpLogicalW shouldn't.
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100));
        listView.Items.Add(new ListViewItem("Row 1"));
        listView.Items.Add(new ListViewItem("Row 10"));
        listView.Items.Add(new ListViewItem("Row 2"));

        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(0));

        Assert.Equal("Row 1", listView.Items[0].Text);
        Assert.Equal("Row 2", listView.Items[1].Text);
        Assert.Equal("Row 10", listView.Items[2].Text);
    }

    // CompareItemText's own direction multiplier (ascending vs descending)
    // is only ever exercised indirectly above via a full ascending-then-
    // descending cycle - this checks it directly, still using natural sort
    // order (Row 2 before Row 10) so a descending pass has to flip that
    // ordering too, not just plain alphabetical order.
    [Fact]
    public void HeaderClick_Descending_UsesNaturalSortOrderInReverse()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100));
        listView.Items.Add(new ListViewItem("Row 1"));
        listView.Items.Add(new ListViewItem("Row 10"));
        listView.Items.Add(new ListViewItem("Row 2"));

        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(0));
        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(0));

        Assert.Equal("Row 10", listView.Items[0].Text);
        Assert.Equal("Row 2", listView.Items[1].Text);
        Assert.Equal("Row 1", listView.Items[2].Text);
    }

    [Fact]
    public void SortingEnabled_False_HeaderClickDoesNotSort()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100));
        listView.SortingEnabled = false;
        listView.Items.Add(new ListViewItem("B"));
        listView.Items.Add(new ListViewItem("A"));

        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(0));

        Assert.Equal("B", listView.Items[0].Text);
        Assert.Equal("A", listView.Items[1].Text);
    }

    [Fact]
    public void SetColumnSortable_False_HeaderClickDoesNotSortThatColumn()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100));
        listView.SetColumnSortable(0, false);
        listView.Items.Add(new ListViewItem("B"));
        listView.Items.Add(new ListViewItem("A"));

        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(0));

        Assert.Equal("B", listView.Items[0].Text);
        Assert.Equal(-1, listView.GetPrivateField<int>("_sortedColumnIndex"));
    }

    [Fact]
    public void SetSortComparer_OverridesTheDefaultComparison()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100));

        // Reverse-alpha - ascending here should sort Z..A instead of A..Z,
        // proving the registered comparer is actually the one driving it.
        listView.SetSortComparer(
            listView.Columns[0],
            Comparer<ListViewItem>.Create((a, b) => string.CompareOrdinal(b.Text, a.Text)));

        listView.Items.Add(new ListViewItem("A"));
        listView.Items.Add(new ListViewItem("B"));

        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(0));

        Assert.Equal("B", listView.Items[0].Text);
        Assert.Equal("A", listView.Items[1].Text);
    }

    [Fact]
    public void SuppressNextColumnClickSort_SkipsExactlyOneClick()
    {
        // A finished column-reorder drag still counts as a click on
        // whatever header cell it started on - this flag suppresses the
        // sort that would otherwise trigger.
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100));
        listView.Items.Add(new ListViewItem("B"));
        listView.Items.Add(new ListViewItem("A"));

        listView.SetPrivateField("_suppressNextColumnClickSort", true);
        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(0));

        Assert.Equal("B", listView.Items[0].Text);
        Assert.Equal(-1, listView.GetPrivateField<int>("_sortedColumnIndex"));
        Assert.False(listView.GetPrivateField<bool>("_suppressNextColumnClickSort"));

        // The flag only ever suppresses the one click it was meant for -
        // an unrelated later click must still sort normally.
        listView.InvokePrivate("OnColumnClick", new ColumnClickEventArgs(0));
        Assert.Equal("A", listView.Items[0].Text);
    }
}
