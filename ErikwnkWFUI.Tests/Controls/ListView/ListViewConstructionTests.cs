using System.Windows.Forms;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>Locks down the constructor's defaults, the same reasoning as DataGridViewConstructionTests.</summary>
public class ListViewConstructionTests
{
    [Fact]
    public void Defaults_MatchTheDocumentedBehavior()
    {
        using WfuiListView listView = new WfuiListView();

        Assert.Equal(View.Details, listView.View);
        Assert.True(listView.FullRowSelect);
        Assert.True(listView.HideSelection);
        Assert.True(listView.MultiSelect);
        Assert.True(listView.OwnerDraw);

        // Native reorder is off - reordering is hand-rolled instead (see
        // AllowColumnReordering).
        Assert.False(listView.AllowColumnReorder);

        // Clickable, not Nonclickable - ColumnClick has to actually fire
        // for header-click sorting (see SortingEnabled).
        Assert.Equal(ColumnHeaderStyle.Clickable, listView.HeaderStyle);
    }

    [Fact]
    public void SortingEnabled_DefaultsToTrue()
    {
        using WfuiListView listView = new WfuiListView();

        Assert.True(listView.SortingEnabled);
    }

    [Fact]
    public void AllowColumnReordering_DefaultsToTrue()
    {
        using WfuiListView listView = new WfuiListView();

        Assert.True(listView.AllowColumnReordering);
    }

    [Fact]
    public void AllowColumnResizing_DefaultsToTrue()
    {
        using WfuiListView listView = new WfuiListView();

        Assert.True(listView.AllowColumnResizing);
    }
}
