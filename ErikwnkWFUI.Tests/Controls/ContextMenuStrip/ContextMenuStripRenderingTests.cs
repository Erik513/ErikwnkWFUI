using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;

namespace ErikwnkWFUI.Tests.Controls.ContextMenuStrip;

/// <summary>
/// The ColorTable ContextMenuStrip's ThemedRenderer builds reads UIColors/
/// SelectionBackColor LIVE on every access rather than snapshotting at
/// construction (see ContextMenuStrip.ThemedColorTable) - re-reading a
/// property after changing SelectionBackColor is itself the test that this
/// stayed live instead of frozen. GetForeColor is invoked directly via
/// reflection rather than through a real paint, which would need actual
/// Graphics/rendering infrastructure this doesn't have.
/// [Collection] (see AccentColorTestCollection) - these read UIColors.Primary/
/// BorderMedium/BackgroundMediumElevated/TextPrimary/TextDisabled, which
/// UIColorsTests changes via SetAccent/ApplyTheme.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ContextMenuStripRenderingTests
{
    private static ToolStripProfessionalRenderer GetRenderer(WfuiContextMenuStrip menu)
    {
        return Assert.IsAssignableFrom<ToolStripProfessionalRenderer>(menu.Renderer);
    }

    [Fact]
    public void RoundedEdges_IsDisabled()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip();

        Assert.False(GetRenderer(menu).RoundedEdges);
    }

    [Fact]
    public void ColorTable_BackgroundMatchesBackgroundMediumElevated()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip();

        Assert.Equal(UIColors.BackgroundMediumElevated, GetRenderer(menu).ColorTable.ToolStripDropDownBackground);
    }

    [Fact]
    public void ColorTable_MenuBorderMatchesBorderMedium()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip();

        Assert.Equal(UIColors.BorderMedium, GetRenderer(menu).ColorTable.MenuBorder);
    }

    [Fact]
    public void ColorTable_SelectedGradientMatchesSelectionBackColor()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip
        {
            SelectionBackColor = Color.FromArgb(11, 22, 33)
        };

        Assert.Equal(Color.FromArgb(11, 22, 33), GetRenderer(menu).ColorTable.MenuItemSelectedGradientBegin);
        Assert.Equal(Color.FromArgb(11, 22, 33), GetRenderer(menu).ColorTable.MenuItemSelectedGradientEnd);
    }

    [Fact]
    public void ColorTable_ReadsSelectionBackColorLive_AfterConstruction()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip();
        ToolStripProfessionalRenderer renderer = GetRenderer(menu);

        menu.SelectionBackColor = Color.Lime;

        Assert.Equal(Color.Lime, renderer.ColorTable.MenuItemSelectedGradientBegin);
    }

    [Fact]
    public void ColorTable_PressedGradientIsDarkenedSelectionColor()
    {
        Color selection = Color.FromArgb(100, 150, 200);
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip
        {
            SelectionBackColor = selection
        };

        Color expected = UIColors.Darken(selection, 30);

        Assert.Equal(expected, GetRenderer(menu).ColorTable.MenuItemPressedGradientBegin);
        Assert.Equal(expected, GetRenderer(menu).ColorTable.MenuItemPressedGradientMiddle);
        Assert.Equal(expected, GetRenderer(menu).ColorTable.MenuItemPressedGradientEnd);
    }

    [Fact]
    public void ColorTable_CheckSelectedBackgroundMatchesSelectionBackColor()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip
        {
            SelectionBackColor = Color.FromArgb(9, 8, 7)
        };

        Assert.Equal(Color.FromArgb(9, 8, 7), GetRenderer(menu).ColorTable.CheckSelectedBackground);
    }

    [Fact]
    public void GetForeColor_ReturnsTextDisabled_ForDisabledItem()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip();
        ToolStripMenuItem item = new ToolStripMenuItem("Item") { Enabled = false };

        Color result = menu.Renderer.InvokePrivate<Color>("GetForeColor", item);

        Assert.Equal(UIColors.TextDisabled, result);
    }

    [Fact]
    public void GetForeColor_ReturnsTextPrimary_ForEnabledUnselectedItem()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip();
        ToolStripMenuItem item = new ToolStripMenuItem("Item");

        Color result = menu.Renderer.InvokePrivate<Color>("GetForeColor", item);

        Assert.Equal(UIColors.TextPrimary, result);
    }
}
