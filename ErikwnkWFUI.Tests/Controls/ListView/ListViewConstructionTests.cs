using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
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

    // CreateStandard/CreatePrimary's "gray until a consumer opts into the
    // accent" split (see UIListViewFactory) - already locked down for
    // ListBox/DataGridView.

    // An alpha-130 tint of BorderLight, not the color itself - see the
    // field's own comment in ListView.cs for why it's translucent.
    [Fact]
    public void SelectionOverlayColor_DefaultsToTranslucentBorderLight()
    {
        using WfuiListView listView = new WfuiListView();

        Assert.Equal(Color.FromArgb(130, UIColors.BorderLight), listView.SelectionOverlayColor);
    }

    [Fact]
    public void ColumnReorderIndicatorColor_DefaultsToTextSecondary()
    {
        using WfuiListView listView = new WfuiListView();

        Assert.Equal(UIColors.TextSecondary, listView.ColumnReorderIndicatorColor);
    }

    [Fact]
    public void BorderColor_DefaultsToBorderMedium()
    {
        using WfuiListView listView = new WfuiListView();

        Assert.Equal(UIColors.BorderMedium, listView.BorderColor);
    }

    [Fact]
    public void CreatePrimary_SetsSelectionOverlayColorToAccent()
    {
        using WfuiListView listView = (WfuiListView)UIStyles.ListViews.CreatePrimary();

        Assert.Equal(UIColors.Selection, listView.SelectionOverlayColor);
    }

    [Fact]
    public void CreatePrimary_SetsColumnReorderIndicatorColorToAccent()
    {
        using WfuiListView listView = (WfuiListView)UIStyles.ListViews.CreatePrimary();

        Assert.Equal(UIColors.Primary, listView.ColumnReorderIndicatorColor);
    }

    [Fact]
    public void CreatePrimary_SetsBorderColorToAccent()
    {
        using WfuiListView listView = (WfuiListView)UIStyles.ListViews.CreatePrimary();

        Assert.Equal(UIColors.Primary, listView.BorderColor);
    }

    [Fact]
    public void CreateStandard_KeepsColorsGray()
    {
        using WfuiListView listView = (WfuiListView)UIStyles.ListViews.CreateStandard();

        Assert.Equal(Color.FromArgb(130, UIColors.BorderLight), listView.SelectionOverlayColor);
        Assert.Equal(UIColors.TextSecondary, listView.ColumnReorderIndicatorColor);
        Assert.Equal(UIColors.BorderMedium, listView.BorderColor);
    }
}
