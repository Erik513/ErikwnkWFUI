using System.Drawing;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// ListView builds its own right-click ContextMenuStrip once, in the
/// constructor (see BuildContextMenu) - ContextMenuSelectionColor's setter
/// has to re-push a later change into that already-built instance, since
/// UIListViewFactory.CreatePrimary sets it via an object initializer, which
/// runs after the constructor already built the menu.
/// [Collection] (see AccentColorTestCollection) - these read
/// UIColors.Primary/BorderLight, which UIColorsTests changes via SetAccent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ListViewContextMenuTests
{
    [Fact]
    public void ContextMenuSelectionColor_DefaultsToBorderLight()
    {
        using WfuiListView listView = new WfuiListView();

        Assert.Equal(UIColors.BorderLight, listView.ContextMenuSelectionColor);
    }

    [Fact]
    public void BuiltInContextMenu_SelectionBackColor_MatchesContextMenuSelectionColor()
    {
        using WfuiListView listView = new WfuiListView();
        WfuiContextMenuStrip menu = Assert.IsType<WfuiContextMenuStrip>(listView.ContextMenuStrip);

        Assert.Equal(UIColors.BorderLight, menu.SelectionBackColor);
    }

    [Fact]
    public void BuiltInContextMenu_HidesImageMargin()
    {
        using WfuiListView listView = new WfuiListView();
        WfuiContextMenuStrip menu = Assert.IsType<WfuiContextMenuStrip>(listView.ContextMenuStrip);

        Assert.False(menu.ShowImageMargin);
    }

    [Fact]
    public void ContextMenuSelectionColor_Set_PropagatesLiveToTheAlreadyBuiltMenu()
    {
        using WfuiListView listView = new WfuiListView();

        listView.ContextMenuSelectionColor = Color.Red;

        WfuiContextMenuStrip menu = Assert.IsType<WfuiContextMenuStrip>(listView.ContextMenuStrip);
        Assert.Equal(Color.Red, menu.SelectionBackColor);
    }

    [Fact]
    public void CreatePrimary_SetsContextMenuSelectionColorToAccent()
    {
        using WfuiListView listView = (WfuiListView)UIStyles.ListViews.CreatePrimary();

        Assert.Equal(UIColors.Primary, listView.ContextMenuSelectionColor);
    }

    [Fact]
    public void CreatePrimary_BuiltInContextMenu_UsesAccentSelectionColor()
    {
        using WfuiListView listView = (WfuiListView)UIStyles.ListViews.CreatePrimary();
        WfuiContextMenuStrip menu = Assert.IsType<WfuiContextMenuStrip>(listView.ContextMenuStrip);

        Assert.Equal(UIColors.Primary, menu.SelectionBackColor);
    }

    [Fact]
    public void CreateStandard_KeepsContextMenuSelectionColorGray()
    {
        using WfuiListView listView = (WfuiListView)UIStyles.ListViews.CreateStandard();

        Assert.Equal(UIColors.BorderLight, listView.ContextMenuSelectionColor);
    }
}
