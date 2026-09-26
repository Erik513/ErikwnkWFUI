using System.Drawing;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// DataGridView builds its own right-click ContextMenuStrip once, in the
/// constructor (see BuildContextMenu) - ContextMenuSelectionColor's setter
/// has to re-push a later change into that already-built instance, since
/// UIDataGridViewFactory.CreatePrimary sets it via an object initializer,
/// which runs after the constructor already built the menu - same pattern
/// as ListViewContextMenuTests.
/// [Collection] (see AccentColorTestCollection) - these read
/// UIColors.Primary/BorderLight, which UIColorsTests changes via SetAccent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class DataGridViewContextMenuTests
{
    [Fact]
    public void ContextMenuSelectionColor_DefaultsToBorderLight()
    {
        using WfuiDataGridView grid = new WfuiDataGridView();

        Assert.Equal(UIColors.BorderLight, grid.ContextMenuSelectionColor);
    }

    [Fact]
    public void BuiltInContextMenu_SelectionBackColor_MatchesContextMenuSelectionColor()
    {
        using WfuiDataGridView grid = new WfuiDataGridView();
        WfuiContextMenuStrip menu = Assert.IsType<WfuiContextMenuStrip>(grid.ContextMenuStrip);

        Assert.Equal(UIColors.BorderLight, menu.SelectionBackColor);
    }

    [Fact]
    public void BuiltInContextMenu_HidesImageMargin()
    {
        using WfuiDataGridView grid = new WfuiDataGridView();
        WfuiContextMenuStrip menu = Assert.IsType<WfuiContextMenuStrip>(grid.ContextMenuStrip);

        Assert.False(menu.ShowImageMargin);
    }

    [Fact]
    public void ContextMenuSelectionColor_Set_PropagatesLiveToTheAlreadyBuiltMenu()
    {
        using WfuiDataGridView grid = new WfuiDataGridView();

        grid.ContextMenuSelectionColor = Color.Red;

        WfuiContextMenuStrip menu = Assert.IsType<WfuiContextMenuStrip>(grid.ContextMenuStrip);
        Assert.Equal(Color.Red, menu.SelectionBackColor);
    }

    [Fact]
    public void CreatePrimary_SetsContextMenuSelectionColorToAccent()
    {
        using WfuiDataGridView grid = (WfuiDataGridView)UIStyles.DataGridViews.CreatePrimary();

        Assert.Equal(UIColors.Primary, grid.ContextMenuSelectionColor);
    }

    [Fact]
    public void CreatePrimary_BuiltInContextMenu_UsesAccentSelectionColor()
    {
        using WfuiDataGridView grid = (WfuiDataGridView)UIStyles.DataGridViews.CreatePrimary();
        WfuiContextMenuStrip menu = Assert.IsType<WfuiContextMenuStrip>(grid.ContextMenuStrip);

        Assert.Equal(UIColors.Primary, menu.SelectionBackColor);
    }

    [Fact]
    public void CreateStandard_KeepsContextMenuSelectionColorGray()
    {
        using WfuiDataGridView grid = (WfuiDataGridView)UIStyles.DataGridViews.CreateStandard();

        Assert.Equal(UIColors.BorderLight, grid.ContextMenuSelectionColor);
    }
}
