using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;

namespace ErikwnkWFUI.Tests.Controls.ContextMenuStrip;

/// <summary>
/// A submenu is a drop-down of its own that takes the renderer of its owner but not the image and
/// check margins of the parent menu: ContextMenuStrip.OnOpening walks the whole item tree and
/// sets them every time the menu is about to show (see ApplyThemeToSubmenus). The opening is
/// raised through reflection, not by showing a menu on screen.
/// </summary>
public class ContextMenuStripSubmenuTests
{
    private static void RaiseOpening(WfuiContextMenuStrip menu)
    {
        menu.InvokePrivate("OnOpening", new CancelEventArgs());
    }

    [Fact]
    public void OnOpening_AppliesRendererToDirectSubmenu()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip();
        ToolStripMenuItem parent = new ToolStripMenuItem("Parent");
        parent.DropDownItems.Add(new ToolStripMenuItem("Child"));
        menu.Items.Add(parent);

        RaiseOpening(menu);

        ToolStripDropDownMenu dropDown = Assert.IsType<ToolStripDropDownMenu>(parent.DropDown);
        Assert.NotNull(dropDown.Renderer);
        Assert.Equal(menu.Renderer.GetType(), dropDown.Renderer.GetType());
    }

    [Fact]
    public void OnOpening_PropagatesShowImageMarginToSubmenu()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip
        {
            ShowImageMargin = false
        };
        ToolStripMenuItem parent = new ToolStripMenuItem("Parent");
        parent.DropDownItems.Add(new ToolStripMenuItem("Child"));
        menu.Items.Add(parent);

        RaiseOpening(menu);

        ToolStripDropDownMenu dropDown = Assert.IsType<ToolStripDropDownMenu>(parent.DropDown);
        Assert.False(dropDown.ShowImageMargin);
    }

    [Fact]
    public void OnOpening_AppliesThemeRecursively_ToNestedSubmenu()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip
        {
            ShowImageMargin = false
        };
        ToolStripMenuItem parent = new ToolStripMenuItem("Parent");
        ToolStripMenuItem child = new ToolStripMenuItem("Child");
        child.DropDownItems.Add(new ToolStripMenuItem("Grandchild"));
        parent.DropDownItems.Add(child);
        menu.Items.Add(parent);

        RaiseOpening(menu);

        ToolStripDropDownMenu grandchildDropDown = Assert.IsType<ToolStripDropDownMenu>(child.DropDown);
        Assert.False(grandchildDropDown.ShowImageMargin);
        Assert.NotNull(grandchildDropDown.Renderer);
    }

    [Fact]
    public void OnOpening_DoesNotThrow_WhenNoItemHasASubmenu()
    {
        using WfuiContextMenuStrip menu = new WfuiContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Plain item"));

        Exception? exception = Record.Exception(() => RaiseOpening(menu));

        Assert.Null(exception);
    }
}
