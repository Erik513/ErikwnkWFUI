using System.Drawing;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;

namespace ErikwnkWFUI.Tests.Controls.ContextMenuStrip;

/// <summary>A color changed while the menu is open is painted at once: the setters ask for a repaint.</summary>
public class ContextMenuStripRepaintTests
{
    [Fact]
    public void ChangingTheSelectionColor_AsksForARepaint()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        _ = menu.Handle;
        int invalidated = 0;
        menu.Invalidated += (sender, e) => invalidated++;

        menu.SelectionBackColor = Color.Orange;

        Assert.True(invalidated > 0);
        Assert.Equal(Color.Orange, menu.SelectionBackColor);
    }

    [Fact]
    public void ChangingTheBorderColor_AsksForARepaint()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        _ = menu.Handle;
        int invalidated = 0;
        menu.Invalidated += (sender, e) => invalidated++;

        menu.BorderColor = Color.Purple;

        Assert.True(invalidated > 0);
        Assert.Equal(Color.Purple, menu.BorderColor);
    }

    [Fact]
    public void AMenuWithoutAWindow_TakesTheColors_WithoutAnError()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();

        menu.SelectionBackColor = Color.Orange;
        menu.BorderColor = Color.Purple;

        Assert.Equal(Color.Orange, menu.SelectionBackColor);
        Assert.Equal(Color.Purple, menu.BorderColor);
    }
}
