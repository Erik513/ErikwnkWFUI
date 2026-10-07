using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;

namespace ErikwnkWFUI.Tests.Controls.ContextMenuStrip;

/// <summary>
/// Odd uses of the menu: filling it (and its submenus) on demand while it opens, opening it
/// again and again, handlers that disable or dispose the menu, changing the colors later.
/// [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ContextMenuStripEdgeCaseTests
{
    private static void RaiseOpening(WfuiContextMenuStrip menu)
    {
        menu.InvokePrivate("OnOpening", new CancelEventArgs());
    }

    private static ToolStripProfessionalRenderer RendererOf(ToolStripDropDown dropDown)
    {
        return Assert.IsAssignableFrom<ToolStripProfessionalRenderer>(dropDown.Renderer);
    }

    [Fact]
    public void ASubmenuAddedInTheOpeningEvent_GetsTheMarginsOfTheMenu()
    {
        // Applications fill a menu when it is about to show.
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard(UIStyles.Colors.Sets.Green);
        menu.ShowImageMargin = false;
        ToolStripMenuItem parent = new ToolStripMenuItem("Recent");
        menu.Items.Add(parent);
        menu.Opening += (sender, e) => parent.DropDownItems.Add("File 1");

        RaiseOpening(menu);

        ToolStripDropDownMenu dropDown = Assert.IsType<ToolStripDropDownMenu>(parent.DropDown);
        Assert.False(dropDown.ShowImageMargin);
        Assert.Equal(menu.SelectionBackColor, RendererOf(dropDown).ColorTable.MenuItemSelectedGradientBegin);
    }

    [Fact]
    public void ASubmenuFilledWhenItDropsDown_HasTheMarginsAlready()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        menu.ShowImageMargin = false;
        ToolStripMenuItem parent = new ToolStripMenuItem("Lazy");
        menu.Items.Add(parent);
        RaiseOpening(menu);

        // The submenu is empty when the menu opens and gets its items later.
        parent.DropDownItems.Add("Late child");

        ToolStripDropDownMenu dropDown = Assert.IsType<ToolStripDropDownMenu>(parent.DropDown);
        Assert.False(dropDown.ShowImageMargin);
    }

    [Fact]
    public void OpeningManyTimes_KeepsTheLook_AndTheItems()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        ToolStripMenuItem parent = new ToolStripMenuItem("Parent");
        parent.DropDownItems.Add("Child");
        menu.Items.Add(parent);

        for (int i = 0; i < 5; i++)
            RaiseOpening(menu);

        ToolStripDropDownMenu dropDown = Assert.IsType<ToolStripDropDownMenu>(parent.DropDown);
        Assert.Equal(menu.Renderer.GetType(), dropDown.Renderer.GetType());
        Assert.Single(parent.DropDownItems);
        Assert.Equal(menu.SelectionBackColor, RendererOf(dropDown).ColorTable.MenuItemSelectedGradientBegin);
    }

    [Fact]
    public void ACancelledOpening_AppliesNothingToTheSubmenus()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        menu.ShowImageMargin = false;
        ToolStripMenuItem parent = new ToolStripMenuItem("Parent");
        parent.DropDownItems.Add("Child");
        menu.Items.Add(parent);
        menu.Opening += (sender, e) => e.Cancel = true;
        CancelEventArgs args = new CancelEventArgs();

        menu.InvokePrivate("OnOpening", args);

        Assert.True(args.Cancel);
        Assert.True(((ToolStripDropDownMenu)parent.DropDown).ShowImageMargin);
    }

    [Fact]
    public void AClickHandlerThatDisablesTheMenu_LeavesTheOtherItemsQuiet()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        ToolStripMenuItem off = new ToolStripMenuItem("Disable me");
        ToolStripMenuItem other = new ToolStripMenuItem("Other");
        menu.Items.Add(off);
        menu.Items.Add(other);
        int otherClicks = 0;
        off.Click += delegate { menu.Enabled = false; };
        other.Click += delegate { otherClicks++; };

        off.PerformClick();
        other.PerformClick();

        // The menu is off: its items do not click any more.
        Assert.Equal(0, otherClicks);
        Assert.False(other.Enabled);
    }

    [Fact]
    public void AClickHandlerThatDisposesTheMenu_LeavesNothingBroken()
    {
        WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        ToolStripMenuItem kill = new ToolStripMenuItem("Dispose me");
        menu.Items.Add(kill);
        kill.Click += delegate { menu.Dispose(); };

        Exception? thrown = Record.Exception(() => kill.PerformClick());

        Assert.Null(thrown);
        Assert.True(menu.IsDisposed);
    }

    [Fact]
    public void TheSet_CanBeTakenAwayAgain_AndTheMenuIsGrayAsBefore()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard(UIStyles.Colors.Sets.Green);
        ToolStripProfessionalRenderer renderer = RendererOf(menu);
        Assert.NotEqual(UIColors.BorderLight, renderer.ColorTable.MenuItemSelectedGradientBegin);

        menu.Colors = null;

        Assert.Equal(UIColors.BorderLight, renderer.ColorTable.MenuItemSelectedGradientBegin);
        Assert.Equal(UIColors.BorderLight, renderer.ColorTable.MenuBorder);
    }

    [Fact]
    public void ARemovedAndReAddedSubmenuItem_IsThemedAgainAtTheNextOpening()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        menu.ShowImageMargin = false;
        ToolStripMenuItem parent = new ToolStripMenuItem("Parent");
        parent.DropDownItems.Add("Child");
        menu.Items.Add(parent);
        RaiseOpening(menu);

        menu.Items.Remove(parent);
        menu.Items.Add(parent);
        RaiseOpening(menu);

        Assert.False(((ToolStripDropDownMenu)parent.DropDown).ShowImageMargin);
    }
}
