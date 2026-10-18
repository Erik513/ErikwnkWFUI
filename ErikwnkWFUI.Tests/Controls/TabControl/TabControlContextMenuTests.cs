using System.ComponentModel;
using System.Linq;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using ErikwnkWFUI.Localization;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;
using WfuiTabControl = ErikwnkWFUI.Controls.TabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// The context menu of the tab control: when it exists, what it shows, the right-click and the color.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TabControlContextMenuTests : TabControlContextMenuTestBase
{
    // ---- when the menu exists ----

    [Fact]
    public void ByDefault_TheEditableControlAllowsBoth_WithTheLibraryMenu()
    {
        using WfuiTabControl tabs = new WfuiTabControl();

        Assert.True(tabs.AllowUserToAddTabs);
        Assert.True(tabs.AllowUserToCloseTabs);
        Assert.IsType<WfuiContextMenuStrip>(tabs.ContextMenuStrip);
        Assert.False(Menu(tabs).ShowImageMargin);
    }

    [Fact]
    public void TheReadOnlyControl_HasNoMenuAndNoEditing()
    {
        using ErikwnkWFUI.Controls.ReadOnlyTabControl tabs = new ErikwnkWFUI.Controls.ReadOnlyTabControl();

        Assert.Null(tabs.ContextMenuStrip);
    }

    [Fact]
    public void TurningBothOff_RemovesTheMenuAgain()
    {
        using WfuiTabControl tabs = new WfuiTabControl();

        tabs.AllowUserToAddTabs = false;
        tabs.AllowUserToCloseTabs = false;
        Assert.NotNull(tabs.ContextMenuStrip);

        tabs.AllowUserToRenameTabs = false;
        Assert.Null(tabs.ContextMenuStrip);
    }

    [Fact]
    public void TurningEditingBackOn_PutsTheMenuBack()
    {
        using WfuiTabControl tabs = new WfuiTabControl { AllowUserToAddTabs = false, AllowUserToCloseTabs = false, AllowUserToRenameTabs = false };

        tabs.AllowUserToCloseTabs = true;

        Assert.IsType<WfuiContextMenuStrip>(tabs.ContextMenuStrip);
    }

    [Fact]
    public void AMenuTheApplicationAssigned_IsNeverReplaced()
    {
        using WfuiTabControl tabs = new WfuiTabControl { AllowUserToAddTabs = false, AllowUserToCloseTabs = false, AllowUserToRenameTabs = false };
        System.Windows.Forms.ContextMenuStrip own = new System.Windows.Forms.ContextMenuStrip();
        tabs.ContextMenuStrip = own;

        tabs.AllowUserToAddTabs = true;
        tabs.AllowUserToAddTabs = false;

        Assert.Same(own, tabs.ContextMenuStrip);
    }

    // ---- what the menu shows ----

    [Fact]
    public void Menu_ShowsOnlyTheAllowedEntries()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.AllowUserToCloseTabs = false;
        Open(tabs);
        Assert.True(AddItem(tabs).Available);
        Assert.False(CloseItem(tabs).Available);

        tabs.AllowUserToCloseTabs = true;
        tabs.AllowUserToAddTabs = false;
        Open(tabs);
        Assert.False(AddItem(tabs).Available);
        Assert.True(CloseItem(tabs).Available);
    }

    [Fact]
    public void CloseTab_IsOnlyEnabled_WhenTheRightClickWasOnATab()
    {
        using WfuiTabControl tabs = CreateTabs();

        RightClick(tabs, 1);
        Open(tabs);
        Assert.True(CloseItem(tabs).Enabled);

        // No right-click on a tab since: the empty strip (or a keypress).
        Open(tabs);
        Assert.False(CloseItem(tabs).Enabled);
    }

    // The real message path: right-clicks on the strip and on a page's
    // content, through the window messages a mouse would produce.
    [Fact]
    public void RealRightClicks_OnATab_TargetThatTab_ElsewhereTargetNothing()
    {
        StaThread.Run(() =>
        {
            using WfuiTabControl tabs = new WfuiTabControl { Dock = DockStyle.Fill };

            for (int i = 0; i < 3; i++)
            {
                tabs.TabPages.Add("Tab " + i);
            }

            using Form host = new Form
            {
                ClientSize = new Size(300, 100),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-3000, -3000),
                ShowInTaskbar = false
            };

            host.Controls.Add(tabs);
            host.Show();
            Application.DoEvents();

            System.Collections.Generic.List<int> targets = new System.Collections.Generic.List<int>();
            Menu(tabs).Opening += (s, e) =>
            {
                targets.Add(tabs.GetPrivateField<int>("_menuTargetIndex"));
                e.Cancel = true;
            };

            // Right button down + up on tab 2: the up makes the window open
            // the context menu.
            Rectangle tab = tabs.GetTabRect(2);
            System.IntPtr onTab = (System.IntPtr)((tab.Left + 10) | ((tab.Top + 8) << 16));
            SendMessage(tabs.Handle, 0x204, (System.IntPtr)2, onTab);
            SendMessage(tabs.Handle, 0x205, System.IntPtr.Zero, onTab);

            // The same on empty strip, to the right of the last tab.
            System.IntPtr onStrip = (System.IntPtr)(290 | ((tab.Top + 8) << 16));
            SendMessage(tabs.Handle, 0x204, (System.IntPtr)2, onStrip);
            SendMessage(tabs.Handle, 0x205, System.IntPtr.Zero, onStrip);

            // And on a page's content (WM_CONTEXTMENU sent to the page).
            TabPage page = tabs.SelectedTab!;
            Point onPage = page.PointToScreen(new Point(50, 50));
            SendMessage(page.Handle, 0x7B, page.Handle, (System.IntPtr)((onPage.X & 0xFFFF) | (onPage.Y << 16)));

            Assert.Equal(new[] { 2, -1, -1 }, targets);
        });
    }

    [Fact]
    public void TabAdding_SettingNoPage_AddsNothing()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.TabAdding += (s, e) => e.TabPage = null!;

        AddItem(tabs).PerformClick();

        Assert.Equal(3, tabs.TabCount);
    }

    [Fact]
    public void ACancelledNewPage_IsDisposed()
    {
        using WfuiTabControl tabs = CreateTabs();
        TabPage? offered = null;
        tabs.TabAdding += (s, e) =>
        {
            offered = e.TabPage;
            e.Cancel = true;
        };

        AddItem(tabs).PerformClick();

        Assert.True(offered!.IsDisposed);
    }

    // ---- a right-click selects the tab ----

    [Fact]
    public void RightClickOnATab_SelectsIt_ByDefault()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SelectedIndex = 0;

        RightClick(tabs, 2);

        Assert.True(tabs.SelectTabOnRightClick);
        Assert.Equal(2, tabs.SelectedIndex);
    }

    [Fact]
    public void RightClickOnATab_LeavesTheSelectionAlone_WhenSwitchedOff()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SelectTabOnRightClick = false;
        tabs.SelectedIndex = 0;

        RightClick(tabs, 2);

        Assert.Equal(0, tabs.SelectedIndex);
    }

    [Fact]
    public void RightClickOffTheTabs_DoesNotChangeTheSelection()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SelectedIndex = 1;
        Rectangle rect = tabs.GetTabRect(1);

        tabs.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, 290, rect.Top + 8, 0));

        Assert.Equal(1, tabs.SelectedIndex);
    }

    [Fact]
    public void TabRightClickHandlers_AlreadySeeTheTabSelected()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SelectedIndex = 0;
        int? selectedInHandler = null;
        tabs.TabRightClick += (s, e) => selectedInHandler = tabs.SelectedIndex;

        RightClick(tabs, 2);

        Assert.Equal(2, selectedInHandler);
    }

    [Fact]
    public void ClosingAfterARightClick_ClosesTheTabThatWasClickedAndSelected()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SelectedIndex = 0;

        RightClick(tabs, 2);
        Open(tabs);
        CloseItem(tabs).PerformClick();

        // Tab 2 was selected by the click, so the one before it follows.
        Assert.Equal(2, tabs.TabCount);
        Assert.Equal(1, tabs.SelectedIndex);
    }

    [Fact]
    public void ARightClickThatIsVetoed_StillTargetsTheClickedTab()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SelectedIndex = 0;
        tabs.Selecting += (s, e) => e.Cancel = true;

        RightClick(tabs, 2);
        Open(tabs);

        Assert.Equal(0, tabs.SelectedIndex);
        Assert.True(CloseItem(tabs).Enabled);
    }

    // ---- right-click on the strip ----

    [Fact]
    public void RightMouseDown_RemembersTheTabUnderTheMouse_LeftDoesNot()
    {
        using WfuiTabControl tabs = CreateTabs();
        Rectangle rect = tabs.GetTabRect(2);

        tabs.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, rect.Left + 8, rect.Top + 8, 0));
        Assert.Equal(-1, tabs.GetPrivateField<int>("_rightClickedTabIndex"));

        RightClick(tabs, 2);
        Assert.Equal(2, tabs.GetPrivateField<int>("_rightClickedTabIndex"));
    }

    // ---- color ----

    [Fact]
    public void ContextMenuSelectionColor_DefaultsToBorderLight_AndPropagatesToTheMenu()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        Assert.Equal(UIColors.BorderLight, tabs.ContextMenuSelectionColor);

        tabs.ContextMenuSelectionColor = Color.Red;

        Assert.Equal(Color.Red, Menu(tabs).SelectionBackColor);
    }

    [Fact]
    public void CreatePrimary_ColorsTheMenuSelectionInTheAccent()
    {
        using WfuiTabControl tabs = UITabControlFactory.CreatePrimary();

        Assert.Equal(UIColors.Primary, tabs.ContextMenuSelectionColor);
    }
}
