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
public abstract class TabControlRenameTestBase
{
    [DllImport("user32.dll")]
    protected static extern System.IntPtr SendMessage(System.IntPtr hWnd, int message, System.IntPtr wParam, System.IntPtr lParam);

    [DllImport("user32.dll")]
    protected static extern System.IntPtr GetFocus();

    [DllImport("user32.dll")]
    protected static extern System.IntPtr GetParent(System.IntPtr hWnd);

    protected const int WM_KEYDOWN = 0x0100;

    protected const int WM_KEYUP = 0x0101;

    protected const int WM_CHAR = 0x0102;

    protected const int WM_LBUTTONDBLCLK = 0x0203;

    protected const int WM_LBUTTONDOWN = 0x0201;

    protected const int WM_RBUTTONDOWN = 0x0204;

    [DllImport("user32.dll")]
    protected static extern bool PostMessage(System.IntPtr hWnd, int message, System.IntPtr wParam, System.IntPtr lParam);

    protected static Form Show(out WfuiTabControl tabs, out TextBox other, int pageCount = 3)
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

    protected static void Pump()
    {
        for (int i = 0; i < 4; i++)
        {
            Application.DoEvents();
        }
    }

    protected static TextBox Box(WfuiTabControl tabs) => tabs.GetPrivateField<TextBox>("_renameBox")!;

    protected static void Press(int virtualKey, char? character = null)
    {
        System.IntPtr target = GetFocus();
        SendMessage(target, WM_KEYDOWN, (System.IntPtr)virtualKey, System.IntPtr.Zero);

        if (character.HasValue)
        {
            SendMessage(target, WM_CHAR, (System.IntPtr)character.Value, System.IntPtr.Zero);
        }

        SendMessage(target, WM_KEYUP, (System.IntPtr)virtualKey, System.IntPtr.Zero);
    }

    protected static void Type(string text)
    {
        foreach (char c in text)
        {
            Press(char.ToUpperInvariant(c), c);
        }

        Pump();
    }

    protected sealed record TabRenamingSnapshot(string Old, string New, TabPage Page);

    // A label and a page's empty area do not take the focus, so the box
    // never loses it - the click itself has to end the edit. The clicks are
    // posted (not sent) so they travel the way real mouse input does, through
    // the message loop where the click watcher sits.
    protected static Form ShowWithAPageLabel(out WfuiTabControl tabs, out Label label)
    {
        Form host = Show(out tabs, out TextBox other);
        label = new Label { Text = "content", Location = new Point(10, 10), AutoSize = true };
        tabs.TabPages[1].Controls.Add(label);
        Pump();
        return host;
    }

    protected static void DoubleClick(WfuiTabControl tabs, int index)
    {
        Rectangle tab = tabs.GetTabRect(index);
        System.IntPtr at = (System.IntPtr)((tab.Left + 10) | ((tab.Top + 8) << 16));
        SendMessage(tabs.Handle, 0x201, (System.IntPtr)1, at);
        SendMessage(tabs.Handle, 0x202, System.IntPtr.Zero, at);
        SendMessage(tabs.Handle, WM_LBUTTONDBLCLK, (System.IntPtr)1, at);
        SendMessage(tabs.Handle, 0x202, System.IntPtr.Zero, at);
        Pump();
    }
}
