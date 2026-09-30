using System.ComponentModel;
using System.Linq;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;
using WfuiTabControl = ErikwnkWFUI.Controls.TabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// The built-in right-click menu (add tab / close tab). Off unless one of
/// AllowUserToAddTabs/AllowUserToCloseTabs is on, like the standard control
/// that has no such menu. Opened through the menu's own protected
/// OnOpening, same as the other controls' menu tests, since a real
/// right-click isn't practical to drive headlessly.
/// [Collection] (see AccentColorTestCollection): the colors read UIColors.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TabControlContextMenuTests
{
    private static WfuiTabControl CreateTabs(int pageCount = 3)
    {
        WfuiTabControl tabs = new WfuiTabControl { Size = new Size(300, 200) };

        for (int i = 0; i < pageCount; i++)
        {
            tabs.TabPages.Add("Tab " + i);
        }

        _ = tabs.Handle;
        return tabs;
    }

    private static WfuiContextMenuStrip Menu(WfuiTabControl tabs) => (WfuiContextMenuStrip)tabs.ContextMenuStrip!;

    private static ToolStripMenuItem AddItem(WfuiTabControl tabs) => (ToolStripMenuItem)Menu(tabs).Items[0];

    private static ToolStripMenuItem CloseItem(WfuiTabControl tabs) => (ToolStripMenuItem)Menu(tabs).Items[1];

    private static void RightClick(WfuiTabControl tabs, int tabIndex)
    {
        Rectangle rect = tabs.GetTabRect(tabIndex);
        tabs.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, rect.Left + 8, rect.Top + 8, 0));
    }

    private static void Open(WfuiTabControl tabs)
    {
        Menu(tabs).InvokePrivate("OnOpening", new CancelEventArgs());
    }

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
        Assert.NotNull(tabs.ContextMenuStrip);

        tabs.AllowUserToCloseTabs = false;
        Assert.Null(tabs.ContextMenuStrip);
    }

    [Fact]
    public void TurningEditingBackOn_PutsTheMenuBack()
    {
        using WfuiTabControl tabs = new WfuiTabControl { AllowUserToAddTabs = false, AllowUserToCloseTabs = false };

        tabs.AllowUserToCloseTabs = true;

        Assert.IsType<WfuiContextMenuStrip>(tabs.ContextMenuStrip);
    }

    [Fact]
    public void AMenuTheApplicationAssigned_IsNeverReplaced()
    {
        using WfuiTabControl tabs = new WfuiTabControl { AllowUserToAddTabs = false, AllowUserToCloseTabs = false };
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

    [DllImport("user32.dll")]
    private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int message, System.IntPtr wParam, System.IntPtr lParam);

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

    // ---- where a new tab goes ----

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public void AddTab_GoesRightBehindTheRightClickedTab(int clicked, int expectedIndex)
    {
        using WfuiTabControl tabs = CreateTabs();

        RightClick(tabs, clicked);
        Open(tabs);
        AddItem(tabs).PerformClick();

        Assert.Equal(4, tabs.TabCount);
        Assert.Equal("New tab", tabs.TabPages[expectedIndex].Text);
        Assert.Same(tabs.TabPages[expectedIndex], tabs.SelectedTab);
    }

    [Fact]
    public void AddTab_OffTheTabs_GoesToTheEnd()
    {
        using WfuiTabControl tabs = CreateTabs();

        Open(tabs);
        AddItem(tabs).PerformClick();

        Assert.Equal("New tab", tabs.TabPages[3].Text);
    }

    [Fact]
    public void AddTab_KeepsTheOtherTabsInOrder()
    {
        using WfuiTabControl tabs = CreateTabs();

        RightClick(tabs, 0);
        Open(tabs);
        AddItem(tabs).PerformClick();

        Assert.Equal(new[] { "Tab 0", "New tab", "Tab 1", "Tab 2" },
            tabs.TabPages.Cast<TabPage>().Select(p => p.Text).ToArray());
    }

    // ---- what is selected after closing ----

    [Theory]
    [InlineData(2, 1)]
    [InlineData(1, 0)]
    public void ClosingTheSelectedTab_SelectsTheOneBeforeIt(int closed, int expectedSelection)
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SelectedIndex = closed;

        RightClick(tabs, closed);
        Open(tabs);
        CloseItem(tabs).PerformClick();

        Assert.Equal(expectedSelection, tabs.SelectedIndex);
    }

    [Fact]
    public void ClosingTheSelectedFirstTab_SelectsTheOneThatTakesItsPlace()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SelectedIndex = 0;

        RightClick(tabs, 0);
        Open(tabs);
        CloseItem(tabs).PerformClick();

        Assert.Equal(0, tabs.SelectedIndex);
        Assert.Equal("Tab 1", tabs.SelectedTab!.Text);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ClosingATabThatIsNotSelected_KeepsTheSelectedPage(int closed)
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SelectTabOnRightClick = false;
        tabs.SelectedIndex = 2;
        TabPage selected = tabs.TabPages[2];

        RightClick(tabs, closed);
        Open(tabs);
        CloseItem(tabs).PerformClick();

        Assert.Same(selected, tabs.SelectedTab);
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

    // ---- one tab always stays ----

    [Fact]
    public void TheLastTab_CannotBeClosed()
    {
        using WfuiTabControl tabs = CreateTabs(pageCount: 1);

        RightClick(tabs, 0);
        Open(tabs);
        Assert.False(CloseItem(tabs).Enabled);
        CloseItem(tabs).PerformClick();

        Assert.Equal(1, tabs.TabCount);
    }

    [Fact]
    public void ClosingDownToOneTab_StopsThere()
    {
        using WfuiTabControl tabs = CreateTabs(pageCount: 3);

        for (int i = 0; i < 5; i++)
        {
            RightClick(tabs, 0);
            Open(tabs);
            CloseItem(tabs).PerformClick();
        }

        Assert.Equal(1, tabs.TabCount);
    }

    [Fact]
    public void CloseTab_BecomesEnabledAgain_OnceASecondTabExists()
    {
        using WfuiTabControl tabs = CreateTabs(pageCount: 1);

        Open(tabs);
        AddItem(tabs).PerformClick();
        RightClick(tabs, 0);
        Open(tabs);

        Assert.True(CloseItem(tabs).Enabled);
    }

    // ---- adding ----

    [Fact]
    public void AddTab_AppendsANewPage_AndSelectsIt()
    {
        using WfuiTabControl tabs = CreateTabs();

        AddItem(tabs).PerformClick();

        Assert.Equal(4, tabs.TabCount);
        Assert.Equal("New tab", tabs.TabPages[3].Text);
        Assert.Same(tabs.TabPages[3], tabs.SelectedTab);
    }

    [Fact]
    public void AddTab_NewPageGetsTheThemedBackColor()
    {
        using WfuiTabControl tabs = CreateTabs();

        AddItem(tabs).PerformClick();

        Assert.Equal(tabs.PageBackColor, tabs.TabPages[3].BackColor);
    }

    [Fact]
    public void TabAdding_CanCancel()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.TabAdding += (s, e) => e.Cancel = true;

        AddItem(tabs).PerformClick();

        Assert.Equal(3, tabs.TabCount);
    }

    [Fact]
    public void TabAdding_CanSupplyItsOwnPage()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.TabAdding += (s, e) => e.TabPage = new TabPage("Mine");

        AddItem(tabs).PerformClick();

        Assert.Equal("Mine", tabs.TabPages[3].Text);
    }

    // ---- closing ----

    [Fact]
    public void CloseTab_RemovesTheRightClickedTab_AndDisposesIt()
    {
        using WfuiTabControl tabs = CreateTabs();
        TabPage page = tabs.TabPages[1];

        RightClick(tabs, 1);
        Open(tabs);
        CloseItem(tabs).PerformClick();

        Assert.Equal(2, tabs.TabCount);
        Assert.DoesNotContain(page, tabs.TabPages.Cast<TabPage>());
        Assert.True(page.IsDisposed);
    }

    [Fact]
    public void TabClosing_CanCancel_AndCarriesThePage()
    {
        using WfuiTabControl tabs = CreateTabs();
        TabPage? seen = null;
        tabs.TabClosing += (s, e) =>
        {
            seen = e.TabPage;
            e.Cancel = true;
        };
        TabPage page = tabs.TabPages[1];

        RightClick(tabs, 1);
        Open(tabs);
        CloseItem(tabs).PerformClick();

        Assert.Equal(3, tabs.TabCount);
        Assert.Same(page, seen);
        Assert.False(page.IsDisposed);
    }

    [Fact]
    public void CloseTab_WithNoTargetTab_DoesNothing()
    {
        using WfuiTabControl tabs = CreateTabs();

        Open(tabs);
        CloseItem(tabs).PerformClick();

        Assert.Equal(3, tabs.TabCount);
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

/// <summary>
/// Same two checks as the other controls' language tests, with the shared
/// LanguageTestHelper: every key translated, and the menu texts following a
/// language change live.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class TabControlLanguageTests
{
    [Theory]
    [InlineData("TabControl.AddTab")]
    [InlineData("TabControl.CloseTab")]
    [InlineData("TabControl.NewTabTitle")]
    public void Key_IsTranslatedForEveryLanguage(string key)
    {
        LanguageTestHelper.AssertTranslatedForEveryLanguage(key);
    }

    [Fact]
    public void MenuItems_Text_UpdateLiveWhenLanguageChanges()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        WfuiContextMenuStrip menu = (WfuiContextMenuStrip)tabs.ContextMenuStrip!;
        string? addEnglish = menu.Items[0].Text;
        string? closeEnglish = menu.Items[1].Text;

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            Assert.NotEqual(addEnglish, menu.Items[0].Text);
            Assert.NotEqual(closeEnglish, menu.Items[1].Text);
        });
    }

    [Fact]
    public void NewTabTitle_FollowsTheLanguageAtTheTimeOfAdding()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        _ = tabs.Handle;
        WfuiContextMenuStrip menu = (WfuiContextMenuStrip)tabs.ContextMenuStrip!;

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            ((ToolStripMenuItem)menu.Items[0]).PerformClick();
        });

        Assert.Equal("Neuer Tab", tabs.TabPages[0].Text);
    }
}
