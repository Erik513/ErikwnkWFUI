using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;
using WfuiTabControl = ErikwnkWFUI.Controls.TabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// Renaming a tab: the events, and the per-tab switches.
/// </summary>
[Trait("Focus", "true")]
[Collection(FocusTestCollection.Name)]
public class TabControlRenameEventsTests : TabControlRenameTestBase
{
    // ---- events ----

    [Fact]
    public void TabRenaming_CarriesTheNames_AndCanCancel()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            TabRenamingSnapshot? seen = null;
            tabs.TabRenaming += (s, e) =>
            {
                seen = new TabRenamingSnapshot(e.OldName, e.NewName, e.TabPage);
                e.Cancel = true;
            };
            tabs.BeginRenameTab(1);
            Pump();

            Box(tabs).Text = "Plan";
            Press(0x0D, '\r');
            Pump();

            Assert.Equal("Tab 1", seen!.Old);
            Assert.Equal("Plan", seen.New);
            Assert.Same(tabs.TabPages[1], seen.Page);
            Assert.Equal("Tab 1", tabs.TabPages[1].Text);
        });
    }

    [Fact]
    public void TabRenaming_CanChangeTheName()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.TabRenaming += (s, e) => e.NewName = e.NewName.ToUpperInvariant();
            tabs.BeginRenameTab(1);
            Pump();

            Box(tabs).Text = "plan";
            Press(0x0D, '\r');
            Pump();

            Assert.Equal("PLAN", tabs.TabPages[1].Text);
        });
    }

    [Fact]
    public void TabRenamed_ComesAfterTheNewNameIsSet()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            string? textInHandler = null;
            string? oldInHandler = null;
            tabs.TabRenamed += (s, e) =>
            {
                textInHandler = e.TabPage.Text;
                oldInHandler = e.OldName;
            };
            tabs.BeginRenameTab(1);
            Pump();

            Box(tabs).Text = "Plan";
            Press(0x0D, '\r');
            Pump();

            Assert.Equal("Plan", textInHandler);
            Assert.Equal("Tab 1", oldInHandler);
        });
    }

    // ---- per tab: rename ----

    [Fact]
    public void EveryTab_IsRenamable_UntilSaidOtherwise()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        tabs.TabPages.Add("a");

        Assert.True(tabs.IsTabRenamable(tabs.TabPages[0]));
        Assert.True(tabs.IsTabClosable(tabs.TabPages[0]));
    }

    [Fact]
    public void ALockedTab_CannotBeRenamed_TheOthersStillCan()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.SetTabRenamable(tabs.TabPages[1], false);

            Assert.False(tabs.BeginRenameTab(1));
            Assert.False(tabs.IsRenamingTab);
            Assert.True(tabs.BeginRenameTab(2));
            Assert.True(tabs.IsRenamingTab);
        });
    }

    [Fact]
    public void ALockedTab_IsNotRenamedByADoubleClick()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.SetTabRenamable(tabs.TabPages[1], false);

            DoubleClick(tabs, 1);

            Assert.False(tabs.IsRenamingTab);
        });
    }

    [Fact]
    public void ALockedTab_HasItsMenuEntryDisabled()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.SetTabRenamable(tabs.TabPages[1], false);
            WfuiContextMenuStrip menu = (WfuiContextMenuStrip)tabs.ContextMenuStrip!;
            ToolStripMenuItem rename = (ToolStripMenuItem)menu.Items[1];

            Rectangle locked = tabs.GetTabRect(1);
            tabs.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, locked.Left + 8, locked.Top + 8, 0));
            menu.InvokePrivate("OnOpening", new CancelEventArgs());
            Assert.False(rename.Enabled);

            Rectangle free = tabs.GetTabRect(2);
            tabs.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, free.Left + 8, free.Top + 8, 0));
            menu.InvokePrivate("OnOpening", new CancelEventArgs());
            Assert.True(rename.Enabled);
        });
    }

    [Fact]
    public void UnlockingATab_MakesItRenamableAgain()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.SetTabRenamable(tabs.TabPages[1], false);
            tabs.SetTabRenamable(tabs.TabPages[1], true);

            Assert.True(tabs.BeginRenameTab(1));
        });
    }

    [Fact]
    public void TheControlWideSwitch_StillForbidsEveryTab()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.AllowUserToRenameTabs = false;

            Assert.False(tabs.BeginRenameTab(2));
            Assert.True(tabs.IsTabRenamable(tabs.TabPages[2]));
        });
    }

    // ---- per tab: TabRenameStarting ----

    [Fact]
    public void TabRenameStarting_CanVetoOneTab_BeforeTheBoxAppears()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            TabPage home = tabs.TabPages[0];
            tabs.TabRenameStarting += (s, e) => e.Cancel = e.TabPage == home;

            Assert.False(tabs.BeginRenameTab(0));
            Assert.False(tabs.IsRenamingTab);
            Assert.True(tabs.BeginRenameTab(1));
        });
    }

    [Fact]
    public void TabRenameStarting_CarriesThePageAndItsIndex()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            ErikwnkWFUI.Controls.TabRenameStartingEventArgs? seen = null;
            tabs.TabRenameStarting += (s, e) => seen = e;

            tabs.BeginRenameTab(2);

            Assert.Equal(2, seen!.TabIndex);
            Assert.Same(tabs.TabPages[2], seen.TabPage);
        });
    }

    [Fact]
    public void AVetoThroughTheEvent_AlsoDisablesTheMenuEntry()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.TabRenameStarting += (s, e) => e.Cancel = true;
            WfuiContextMenuStrip menu = (WfuiContextMenuStrip)tabs.ContextMenuStrip!;
            Rectangle tab = tabs.GetTabRect(1);
            tabs.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, tab.Left + 8, tab.Top + 8, 0));

            menu.InvokePrivate("OnOpening", new CancelEventArgs());

            Assert.False(((ToolStripMenuItem)menu.Items[1]).Enabled);
        });
    }

    [Fact]
    public void TheRenameFlagsRejectANullPage()
    {
        using WfuiTabControl tabs = new WfuiTabControl();

        Assert.Throws<System.ArgumentNullException>(() => tabs.SetTabRenamable(null!, false));
        Assert.Throws<System.ArgumentNullException>(() => tabs.IsTabRenamable(null!));
        Assert.Throws<System.ArgumentNullException>(() => tabs.SetTabClosable(null!, false));
        Assert.Throws<System.ArgumentNullException>(() => tabs.IsTabClosable(null!));
    }
}
