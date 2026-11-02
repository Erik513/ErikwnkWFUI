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
/// The context menu of the tab control: adding and closing tabs and what is selected after.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TabControlContextMenuActionTests : TabControlContextMenuTestBase
{
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

    // ---- per tab: close ----

    [Fact]
    public void ALockedTab_CannotBeClosed_ItsMenuEntryIsDisabled()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SetTabClosable(tabs.TabPages[1], false);

        RightClick(tabs, 1);
        Open(tabs);
        Assert.False(CloseItem(tabs).Enabled);
        CloseItem(tabs).PerformClick();

        Assert.Equal(3, tabs.TabCount);
    }

    [Fact]
    public void TheOtherTabs_CanStillBeClosed()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SetTabClosable(tabs.TabPages[1], false);

        RightClick(tabs, 2);
        Open(tabs);
        Assert.True(CloseItem(tabs).Enabled);
        CloseItem(tabs).PerformClick();

        Assert.Equal(2, tabs.TabCount);
    }

    [Fact]
    public void UnlockingATab_MakesItClosableAgain()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SetTabClosable(tabs.TabPages[1], false);
        tabs.SetTabClosable(tabs.TabPages[1], true);

        RightClick(tabs, 1);
        Open(tabs);

        Assert.True(CloseItem(tabs).Enabled);
    }

    [Fact]
    public void ALockedTab_IsStillAddedBehindAndRenamedLikeAnyOther()
    {
        using WfuiTabControl tabs = CreateTabs();
        tabs.SetTabClosable(tabs.TabPages[0], false);

        RightClick(tabs, 0);
        Open(tabs);
        AddItem(tabs).PerformClick();

        Assert.Equal("New tab", tabs.TabPages[1].Text);
        Assert.True(tabs.IsTabRenamable(tabs.TabPages[0]));
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
}
