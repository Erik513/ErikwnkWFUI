using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;

namespace ErikwnkWFUI.Tests.Controls.ContextMenuStrip;

/// <summary>
/// The menu is the standard one with another look: each test pins one answer, a part of the
/// standard control that keeps working, or something the library leaves alone on purpose (the
/// decisions are on the class). No menu is shown on screen: clicks are performed on the items
/// and the events raised directly. [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ContextMenuStripBehaviorTests
{
    private static void RaiseOpening(WfuiContextMenuStrip menu)
    {
        menu.InvokePrivate("OnOpening", new CancelEventArgs());
    }

    // ---- what keeps working ----

    [Fact]
    public void AnItem_RaisesItsClick_AndTheMenusItemClicked_OnceEach()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        ToolStripMenuItem item = new ToolStripMenuItem("Open");
        menu.Items.Add(item);
        int clicks = 0;
        List<ToolStripItem> clicked = new List<ToolStripItem>();
        item.Click += delegate { clicks++; };
        menu.ItemClicked += (sender, e) => clicked.Add(e.ClickedItem!);

        item.PerformClick();

        Assert.Equal(1, clicks);
        Assert.Equal(new ToolStripItem[] { item }, clicked.ToArray());
    }

    [Fact]
    public void ADisabledItem_DoesNotClick_AndCheckOnClickToggles()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        ToolStripMenuItem disabled = new ToolStripMenuItem("Off") { Enabled = false };
        ToolStripMenuItem toggle = new ToolStripMenuItem("Details") { CheckOnClick = true };
        menu.Items.Add(disabled);
        menu.Items.Add(toggle);
        int clicks = 0;
        disabled.Click += delegate { clicks++; };

        disabled.PerformClick();
        toggle.PerformClick();
        Assert.Equal(0, clicks);
        Assert.True(toggle.Checked);

        toggle.PerformClick();
        Assert.False(toggle.Checked);
    }

    [Fact]
    public void ACancelledOpening_KeepsTheMenuClosed_AndTheEventsFollow()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        menu.Items.Add("Item");
        int opened = 0;
        menu.Opening += (sender, e) => e.Cancel = true;
        menu.Opened += delegate { opened++; };

        menu.Show(new Point(-4000, -4000));

        Assert.False(menu.Visible);
        Assert.Equal(0, opened);
    }

    [Fact]
    public void ItemsCanBeAddedAfterTheFirstOpening_AndTheNewSubmenuGetsTheLookAtTheNext()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        RaiseOpening(menu);

        ToolStripMenuItem parent = new ToolStripMenuItem("Later");
        parent.DropDownItems.Add("Child");
        menu.Items.Add(parent);
        RaiseOpening(menu);

        ToolStripDropDownMenu dropDown = Assert.IsType<ToolStripDropDownMenu>(parent.DropDown);
        Assert.Equal(menu.Renderer.GetType(), dropDown.Renderer.GetType());
    }

    [Fact]
    public void TheImageAndTheCheckMargin_FollowTheMenuIntoTheSubmenus()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        menu.ShowImageMargin = false;
        menu.ShowCheckMargin = true;
        ToolStripMenuItem parent = new ToolStripMenuItem("Parent");
        parent.DropDownItems.Add("Child");
        menu.Items.Add(parent);

        RaiseOpening(menu);

        ToolStripDropDownMenu dropDown = Assert.IsType<ToolStripDropDownMenu>(parent.DropDown);
        Assert.False(dropDown.ShowImageMargin);
        Assert.True(dropDown.ShowCheckMargin);
    }

    [Fact]
    public void ASubmenu_FollowsTheColorSetOfItsMenu()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard(UIStyles.Colors.Sets.Green);
        ToolStripMenuItem parent = new ToolStripMenuItem("Parent");
        parent.DropDownItems.Add("Child");
        menu.Items.Add(parent);
        RaiseOpening(menu);

        ToolStripDropDownMenu dropDown = Assert.IsType<ToolStripDropDownMenu>(parent.DropDown);
        ToolStripProfessionalRenderer renderer = Assert.IsAssignableFrom<ToolStripProfessionalRenderer>(dropDown.Renderer);

        Assert.Equal(menu.SelectionBackColor, renderer.ColorTable.MenuItemSelectedGradientBegin);
    }

    // ---- left alone on purpose ----

    [Fact]
    public void HostedControls_AreAllowed_AndKeepTheirOwnLook()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        ToolStripComboBox combo = new ToolStripComboBox();
        ToolStripTextBox text = new ToolStripTextBox();
        Color comboBack = combo.ComboBox.BackColor;
        Color textBack = text.TextBox.BackColor;

        Exception? thrown = Record.Exception(() =>
        {
            menu.Items.Add(combo);
            menu.Items.Add(text);
            menu.Items.Add(new ToolStripProgressBar());
            menu.Items.Add(new ToolStripButton("Button"));
            RaiseOpening(menu);
        });

        Assert.Null(thrown);
        Assert.Equal(comboBack, combo.ComboBox.BackColor);
        Assert.Equal(textBack, text.TextBox.BackColor);
    }

    [Fact]
    public void ARendererSetByTheApplication_ReplacesTheLibrarysLook()
    {
        using WfuiContextMenuStrip menu = UIStyles.ContextMenuStrips.CreateStandard();
        ToolStripSystemRenderer own = new ToolStripSystemRenderer();

        menu.Renderer = own;
        menu.Items.Add("Item");
        RaiseOpening(menu);

        Assert.Same(own, menu.Renderer);
    }
}
