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
/// Renaming a tab: what ends an edit, clicks that do not take the focus, the double-click and the menu.
/// </summary>
[Collection(FocusTestCollection.Name)]
public class TabControlRenameInputTests : TabControlRenameTestBase
{
    // ---- what ends an edit ----

    [Fact]
    public void SelectingAnotherTab_ClosesTheBox_AndKeepsTheTypedName()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();
            Box(tabs).Text = "Plan";

            tabs.SelectedIndex = 2;
            Pump();

            Assert.False(tabs.IsRenamingTab);
            Assert.Equal("Plan", tabs.TabPages[1].Text);
        });
    }

    [Fact]
    public void ResizingTheControl_ClosesTheBox()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();

            host.ClientSize = new Size(300, 200);
            Pump();

            Assert.False(tabs.IsRenamingTab);
        });
    }

    [Fact]
    public void TurningRenamingOff_WhileEditing_DropsTheEdit()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();
            Box(tabs).Text = "Plan";

            tabs.AllowUserToRenameTabs = false;

            Assert.False(tabs.IsRenamingTab);
            Assert.Equal("Tab 1", tabs.TabPages[1].Text);
        });
    }

    [Fact]
    public void BeginningASecondRename_AppliesTheFirstOne()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(0);
            Pump();
            Box(tabs).Text = "First";

            tabs.BeginRenameTab(2);
            Pump();

            Assert.Equal("First", tabs.TabPages[0].Text);
            Assert.True(tabs.IsRenamingTab);
        });
    }

    // ---- clicking somewhere that does not take the focus ----

    [Fact]
    public void ClickingALabelOnThePage_AppliesTheName_AndEndsTheEdit()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowWithAPageLabel(out WfuiTabControl tabs, out Label label);
            tabs.BeginRenameTab(1);
            Pump();
            Type("Plan");

            PostMessage(label.Handle, WM_LBUTTONDOWN, (System.IntPtr)1, (System.IntPtr)(5 | (5 << 16)));
            Pump();

            Assert.False(tabs.IsRenamingTab);
            Assert.Equal("Plan", tabs.TabPages[1].Text);
        });
    }

    [Fact]
    public void ClickingTheEmptyAreaOfThePage_EndsTheEdit()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowWithAPageLabel(out WfuiTabControl tabs, out Label label);
            tabs.BeginRenameTab(1);
            Pump();
            Box(tabs).Text = "Plan";

            PostMessage(tabs.TabPages[1].Handle, WM_LBUTTONDOWN, (System.IntPtr)1, (System.IntPtr)(200 | (80 << 16)));
            Pump();

            Assert.False(tabs.IsRenamingTab);
            Assert.Equal("Plan", tabs.TabPages[1].Text);
        });
    }

    [Fact]
    public void ARightClickOnThePage_EndsTheEditToo()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowWithAPageLabel(out WfuiTabControl tabs, out Label label);
            tabs.BeginRenameTab(1);
            Pump();

            PostMessage(tabs.TabPages[1].Handle, WM_RBUTTONDOWN, (System.IntPtr)2, (System.IntPtr)(200 | (80 << 16)));
            Pump();

            Assert.False(tabs.IsRenamingTab);
        });
    }

    [Fact]
    public void ClickingTheTabStripOutsideTheBox_EndsTheEdit()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();
            Rectangle tab = tabs.GetTabRect(1);

            // The empty strip to the right of the last tab.
            PostMessage(tabs.Handle, WM_LBUTTONDOWN, (System.IntPtr)1, (System.IntPtr)(390 | ((tab.Top + 8) << 16)));
            Pump();

            Assert.False(tabs.IsRenamingTab);
        });
    }

    [Fact]
    public void ClickingInsideTheBox_DoesNotEndTheEdit()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();

            PostMessage(Box(tabs).Handle, WM_LBUTTONDOWN, (System.IntPtr)1, (System.IntPtr)(5 | (5 << 16)));
            Pump();

            Assert.True(tabs.IsRenamingTab);
        });
    }

    [Fact]
    public void ThePageClickThatEndedTheEdit_IsNotSwallowed()
    {
        StaThread.Run(() =>
        {
            using Form host = ShowWithAPageLabel(out WfuiTabControl tabs, out Label label);
            int clicks = 0;
            label.MouseDown += (s, e) => clicks++;
            tabs.BeginRenameTab(1);
            Pump();

            PostMessage(label.Handle, WM_LBUTTONDOWN, (System.IntPtr)1, (System.IntPtr)(5 | (5 << 16)));
            Pump();

            Assert.Equal(1, clicks);
        });
    }

    [Fact]
    public void AfterTheEdit_NothingIsWatchingTheMouseAnymore()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();
            Assert.NotNull(tabs.GetPrivateField<object?>("_renameClickFilter"));

            Press(0x1B);
            Pump();

            Assert.Null(tabs.GetPrivateField<object?>("_renameClickFilter"));
        });
    }

    // ---- double-click ----

    [Fact]
    public void DoubleClickOnATab_StartsRenamingIt()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);

            DoubleClick(tabs, 1);

            Assert.True(tabs.IsRenamingTab);
            Assert.Equal("Tab 1", Box(tabs).Text);
        });
    }

    [Fact]
    public void DoubleClick_DoesNothing_WhenSwitchedOff()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.RenameTabOnDoubleClick = false;

            DoubleClick(tabs, 1);

            Assert.False(tabs.IsRenamingTab);
        });
    }

    [Fact]
    public void DoubleClickOffTheTabs_DoesNothing()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            Rectangle tab = tabs.GetTabRect(2);
            System.IntPtr at = (System.IntPtr)(390 | ((tab.Top + 8) << 16));

            SendMessage(tabs.Handle, WM_LBUTTONDBLCLK, (System.IntPtr)1, at);
            Pump();

            Assert.False(tabs.IsRenamingTab);
        });
    }

    // ---- the menu ----

    [Fact]
    public void RenameMenuItem_StartsRenamingTheRightClickedTab()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            WfuiContextMenuStrip menu = (WfuiContextMenuStrip)tabs.ContextMenuStrip!;
            Rectangle tab = tabs.GetTabRect(2);
            tabs.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, tab.Left + 8, tab.Top + 8, 0));
            menu.InvokePrivate("OnOpening", new CancelEventArgs());

            ((ToolStripMenuItem)menu.Items[1]).PerformClick();
            Pump();

            Assert.True(tabs.IsRenamingTab);
            Assert.Equal("Tab 2", Box(tabs).Text);
        });
    }

    [Fact]
    public void RenameMenuItem_IsDisabledOffTheTabs_AndHiddenWhenNotAllowed()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            WfuiContextMenuStrip menu = (WfuiContextMenuStrip)tabs.ContextMenuStrip!;
            ToolStripMenuItem rename = (ToolStripMenuItem)menu.Items[1];

            menu.InvokePrivate("OnOpening", new CancelEventArgs());
            Assert.False(rename.Enabled);
            Assert.True(rename.Available);

            tabs.AllowUserToRenameTabs = false;
            menu.InvokePrivate("OnOpening", new CancelEventArgs());
            Assert.False(rename.Available);
        });
    }

    [Fact]
    public void RenamingOnlyAllowed_StillGetsTheMenu()
    {
        using WfuiTabControl tabs = new WfuiTabControl { AllowUserToAddTabs = false, AllowUserToCloseTabs = false };

        Assert.IsType<WfuiContextMenuStrip>(tabs.ContextMenuStrip);
    }
}
