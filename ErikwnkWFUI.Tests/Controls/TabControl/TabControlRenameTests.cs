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
/// Renaming a tab: starting, ending, the length, right after adding, and the box going away with the control.
/// </summary>
[Trait("Focus", "true")]
[Collection(FocusTestCollection.Name)]
public class TabControlRenameTests : TabControlRenameTestBase
{
    // ---- starting ----

    [Fact]
    public void BeginRenameTab_PutsATextBoxOnTheTab_WithTheNameSelectedAndTheFocus()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);

            tabs.BeginRenameTab(1);
            Pump();

            TextBox box = Box(tabs);
            Assert.True(tabs.IsRenamingTab);
            Assert.Equal("Tab 1", box.Text);
            Assert.Equal(box.Text.Length, box.SelectionLength);
            Assert.True(box.Focused);
            Assert.Equal(tabs.Handle, GetParent(box.Handle));
            Assert.Equal(1, tabs.SelectedIndex);
        });
    }

    [Fact]
    public void TheBox_SitsInsideTheTabsRectangle_AndMatchesItsColors()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.SelectedTabBackColor = Color.FromArgb(11, 12, 13);

            tabs.BeginRenameTab(2);

            TextBox box = Box(tabs);
            Rectangle tab = tabs.GetTabRect(2);
            Assert.True(box.Left >= tab.Left && box.Top >= tab.Top && box.Bottom <= tab.Bottom);
            Assert.Equal(Color.FromArgb(11, 12, 13), box.BackColor);
            Assert.Equal(tabs.SelectedTabForeColor, box.ForeColor);
        });
    }

    [Fact]
    public void BeginRenameTab_DoesNothing_WhenRenamingIsNotAllowed()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.AllowUserToRenameTabs = false;

            tabs.BeginRenameTab(1);

            Assert.False(tabs.IsRenamingTab);
        });
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void BeginRenameTab_IgnoresAnIndexThatIsNotATab(int index)
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);

            tabs.BeginRenameTab(index);

            Assert.False(tabs.IsRenamingTab);
        });
    }

    // ---- ending ----

    [Fact]
    public void TypingAName_AndEnter_AppliesIt()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();

            Type("Plan");
            Press(0x0D, '\r');
            Pump();

            Assert.False(tabs.IsRenamingTab);
            Assert.Equal("Plan", tabs.TabPages[1].Text);
        });
    }

    [Fact]
    public void Escape_KeepsTheOldName()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();

            Type("Plan");
            Press(0x1B);
            Pump();

            Assert.False(tabs.IsRenamingTab);
            Assert.Equal("Tab 1", tabs.TabPages[1].Text);
        });
    }

    [Fact]
    public void LeavingTheBox_AppliesTheName()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();
            Type("Plan");

            other.Focus();
            Pump();

            Assert.False(tabs.IsRenamingTab);
            Assert.Equal("Plan", tabs.TabPages[1].Text);
        });
    }

    [Fact]
    public void AnEmptyName_IsNotApplied()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();

            Box(tabs).Text = "   ";
            Press(0x0D, '\r');
            Pump();

            Assert.Equal("Tab 1", tabs.TabPages[1].Text);
        });
    }

    [Fact]
    public void TheNameIsTrimmed()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();

            Box(tabs).Text = "  Plan  ";
            Press(0x0D, '\r');
            Pump();

            Assert.Equal("Plan", tabs.TabPages[1].Text);
        });
    }

    [Fact]
    public void AnUnchangedName_RaisesNothing()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            int raised = 0;
            tabs.TabRenaming += (s, e) => raised++;
            tabs.TabRenamed += (s, e) => raised++;
            tabs.BeginRenameTab(1);
            Pump();

            Press(0x0D, '\r');
            Pump();

            Assert.Equal(0, raised);
        });
    }

    [Fact]
    public void BeginRenameTab_NeedsAWindow_AndDoesNothingWithoutOne()
    {
        using WfuiTabControl tabs = new WfuiTabControl();
        tabs.TabPages.Add("a");

        Assert.False(tabs.BeginRenameTab(0));
        Assert.False(tabs.IsRenamingTab);
    }

    // ---- length ----

    [Fact]
    public void TheNameLength_IsLimitedTo40_ByDefault()
    {
        using WfuiTabControl tabs = new WfuiTabControl();

        Assert.Equal(40, tabs.MaxTabNameLength);
    }

    [Fact]
    public void TheBox_StopsAcceptingCharacters_AtTheLimit()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.MaxTabNameLength = 10;
            tabs.BeginRenameTab(1);
            Pump();
            Box(tabs).Clear();

            Type(new string('a', 25));
            Press(0x0D, '\r');
            Pump();

            Assert.Equal(new string('a', 10), tabs.TabPages[1].Text);
        });
    }

    [Fact]
    public void TheDefaultLimit_AppliesToTheBox()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();

            Assert.Equal(40, Box(tabs).MaxLength);
        });
    }

    [Fact]
    public void ChangingTheLimit_WhileEditing_UpdatesTheBox()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();

            tabs.MaxTabNameLength = 15;

            Assert.Equal(15, Box(tabs).MaxLength);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ALimitBelowOne_IsRejected(int value)
    {
        using WfuiTabControl tabs = new WfuiTabControl();

        Assert.Throws<System.ArgumentOutOfRangeException>(() => tabs.MaxTabNameLength = value);
        Assert.Equal(40, tabs.MaxTabNameLength);
    }

    [Fact]
    public void ANameThatIsAlreadyLongerThanTheLimit_IsShownWhole_AndCanBeShortened()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.TabPages[1].Text = new string('x', 60);
            tabs.MaxTabNameLength = 40;
            tabs.BeginRenameTab(1);
            Pump();

            Assert.Equal(60, Box(tabs).Text.Length);

            Box(tabs).Text = "short";
            Press(0x0D, '\r');
            Pump();

            Assert.Equal("short", tabs.TabPages[1].Text);
        });
    }

    // ---- rename right after adding ----

    [Fact]
    public void RenameTabAfterAdding_StartsTheEditOnTheNewTab()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.RenameTabAfterAdding = true;
            WfuiContextMenuStrip menu = (WfuiContextMenuStrip)tabs.ContextMenuStrip!;
            Rectangle tab = tabs.GetTabRect(0);
            tabs.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, tab.Left + 8, tab.Top + 8, 0));
            menu.InvokePrivate("OnOpening", new CancelEventArgs());

            ((ToolStripMenuItem)menu.Items[0]).PerformClick();
            Pump();

            Assert.True(tabs.IsRenamingTab);
            Assert.Equal("New tab", Box(tabs).Text);
            Assert.Equal(1, tabs.SelectedIndex);
        });
    }

    [Fact]
    public void AddingATab_DoesNotStartAnEdit_ByDefault()
    {
        StaThread.Run(() =>
        {
            using Form host = Show(out WfuiTabControl tabs, out TextBox other);
            WfuiContextMenuStrip menu = (WfuiContextMenuStrip)tabs.ContextMenuStrip!;

            menu.InvokePrivate("OnOpening", new CancelEventArgs());
            ((ToolStripMenuItem)menu.Items[0]).PerformClick();
            Pump();

            Assert.False(tabs.IsRenamingTab);
        });
    }

    // ---- the box goes away with the control ----

    [Fact]
    public void DisposingTheControl_WhileEditing_DisposesTheBox()
    {
        StaThread.Run(() =>
        {
            Form host = Show(out WfuiTabControl tabs, out TextBox other);
            tabs.BeginRenameTab(1);
            Pump();
            TextBox box = Box(tabs);

            host.Dispose();

            Assert.True(box.IsDisposed);
        });
    }

    // ---- the read-only control has none of it ----

    [Fact]
    public void TheReadOnlyControl_HasNoRenaming()
    {
        System.Type readOnly = typeof(ErikwnkWFUI.Controls.ReadOnlyTabControl);

        Assert.Null(readOnly.GetMethod("BeginRenameTab"));
        Assert.Null(readOnly.GetProperty("AllowUserToRenameTabs"));
    }
}
