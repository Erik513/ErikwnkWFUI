using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;

namespace ErikwnkWFUI.Tests.Controls.ContextMenuStrip;

/// <summary>
/// Locks down CreateStandard's SelectionBackColor defaulting to a neutral
/// gray (not the live accent) and CreatePrimary explicitly overriding it
/// back to the accent - the same CreateStandard/CreatePrimary split every
/// other control in this library already has (see UIContextMenuStripFactory).
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
        using WfuiContextMenuStrip menu = UIStyles.ContextMenus.CreateStandard();

        Assert.Equal(UIColors.BorderLight, menu.SelectionBackColor);
    }

    [Fact]
    public void CreatePrimary_SetsSelectionColorToAccent()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenus.CreatePrimary();

        Assert.Equal(UIColors.Primary, menu.SelectionBackColor);
    }
}
