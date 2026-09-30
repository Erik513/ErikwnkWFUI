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
/// Renaming a tab in place: the name turns into a text box that sits on the
/// tab. Everything here goes through a shown form and real window messages
/// (typed characters, Enter, Escape, a double-click, a click elsewhere),
/// because the box is a real child window that needs the focus.
/// </summary>
[Collection(FocusTestCollection.Name)]
public class TabControlRenameTests
{
    [DllImport("user32.dll")]
    private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int message, System.IntPtr wParam, System.IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern System.IntPtr GetFocus();

    [DllImport("user32.dll")]
    private static extern System.IntPtr GetParent(System.IntPtr hWnd);

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_CHAR = 0x0102;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;

    [DllImport("user32.dll")]
    private static extern bool PostMessage(System.IntPtr hWnd, int message, System.IntPtr wParam, System.IntPtr lParam);

    private static Form Show(out WfuiTabControl tabs, out TextBox other, int pageCount = 3)
    {
        tabs = new WfuiTabControl { Dock = DockStyle.Fill };

        for (int i = 0; i < pageCount; i++)
        {
            tabs.TabPages.Add("Tab " + i);
        }

        other = new TextBox { Dock = DockStyle.Bottom };

        Form host = new Form
        {
            ClientSize = new Size(400, 200),
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, -3000),
            ShowInTaskbar = false
        };

        host.Controls.Add(tabs);
        host.Controls.Add(other);
        host.Show();
        Pump();
        return host;
    }

    private static void Pump()
    {
        for (int i = 0; i < 4; i++)
        {
            Application.DoEvents();
        }
    }

    private static TextBox Box(WfuiTabControl tabs) => tabs.GetPrivateField<TextBox>("_renameBox")!;

    private static void Press(int virtualKey, char? character = null)
    {
        System.IntPtr target = GetFocus();
        SendMessage(target, WM_KEYDOWN, (System.IntPtr)virtualKey, System.IntPtr.Zero);

        if (character.HasValue)
        {
            SendMessage(target, WM_CHAR, (System.IntPtr)character.Value, System.IntPtr.Zero);
        }

        SendMessage(target, WM_KEYUP, (System.IntPtr)virtualKey, System.IntPtr.Zero);
    }

    private static void Type(string text)
    {
        foreach (char c in text)
        {
            Press(char.ToUpperInvariant(c), c);
        }

        Pump();
    }

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

    private sealed record TabRenamingSnapshot(string Old, string New, TabPage Page);

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

    // A label and a page's empty area do not take the focus, so the box
    // never loses it - the click itself has to end the edit. The clicks are
    // posted (not sent) so they travel the way real mouse input does, through
    // the message loop where the click watcher sits.
    private static Form ShowWithAPageLabel(out WfuiTabControl tabs, out Label label)
    {
        Form host = Show(out tabs, out TextBox other);
        label = new Label { Text = "content", Location = new Point(10, 10), AutoSize = true };
        tabs.TabPages[1].Controls.Add(label);
        Pump();
        return host;
    }

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

    private static void DoubleClick(WfuiTabControl tabs, int index)
    {
        Rectangle tab = tabs.GetTabRect(index);
        System.IntPtr at = (System.IntPtr)((tab.Left + 10) | ((tab.Top + 8) << 16));
        SendMessage(tabs.Handle, 0x201, (System.IntPtr)1, at);
        SendMessage(tabs.Handle, 0x202, System.IntPtr.Zero, at);
        SendMessage(tabs.Handle, WM_LBUTTONDBLCLK, (System.IntPtr)1, at);
        SendMessage(tabs.Handle, 0x202, System.IntPtr.Zero, at);
        Pump();
    }

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
