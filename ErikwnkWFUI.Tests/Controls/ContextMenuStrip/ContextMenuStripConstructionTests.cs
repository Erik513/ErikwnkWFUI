using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;

namespace ErikwnkWFUI.Tests.Controls.ContextMenuStrip;

/// <summary>
/// The defaults of a new menu: the neutral gray (not the accent) for CreateStandard, the primary
/// set for CreatePrimary - the same split as every other control (see UIContextMenuStripFactory).
/// [Collection] (see AccentColorTestCollection) - these read UIColors.Primary/
/// BorderLight, which UIColorsTests changes via SetAccent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ContextMenuStripConstructionTests
{
    [Fact]
    public void Constructor_ShowImageMarginDefaultsToTrue()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip();

        Assert.True(menu.ShowImageMargin);
    }

    [Fact]
    public void Constructor_UsesToolStripProfessionalRenderer()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip();

        Assert.IsAssignableFrom<ToolStripProfessionalRenderer>(menu.Renderer);
    }

    [Fact]
    public void SelectionBackColor_DefaultsToBorderLight()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip();

        Assert.Equal(UIColors.BorderLight, menu.SelectionBackColor);
    }

    [Fact]
    public void SelectionBackColor_CanBeSetExplicitly()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip
        {
            SelectionBackColor = Color.Red
        };

        Assert.Equal(Color.Red, menu.SelectionBackColor);
    }

    [Fact]
    public void CreateStandard_KeepsSelectionColorGray()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();

        Assert.Equal(UIColors.BorderLight, menu.SelectionBackColor);
    }

    [Fact]
    public void CreatePrimary_UsesThePrimarySet()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreatePrimary();

        Assert.Equal(UIColors.PrimarySet.Hover, menu.Colors!.Hover);
        Assert.Equal(UIColors.GetReadableFill(UIColors.PrimarySet.Hover, UIColors.PrimarySet.HoverText), menu.SelectionBackColor);
    }
}
