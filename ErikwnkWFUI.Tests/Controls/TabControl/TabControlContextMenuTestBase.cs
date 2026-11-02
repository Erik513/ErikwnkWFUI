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
/// The built-in right-click menu (add, rename and close tab) of the
/// editable TabControl: present while any of the Allow... switches is on,
/// absent on the read-only control. Opened through the menu's own
/// protected OnOpening, same as the other controls' menu tests, since a
/// real right-click isn't practical to drive headlessly.
/// [Collection] (see AccentColorTestCollection): the colors read UIColors.
/// </summary>
public abstract class TabControlContextMenuTestBase
{
    protected static WfuiTabControl CreateTabs(int pageCount = 3)
    {
        WfuiTabControl tabs = new WfuiTabControl { Size = new Size(300, 200) };

        for (int i = 0; i < pageCount; i++)
        {
            tabs.TabPages.Add("Tab " + i);
        }

        _ = tabs.Handle;
        return tabs;
    }

    protected static WfuiContextMenuStrip Menu(WfuiTabControl tabs) => (WfuiContextMenuStrip)tabs.ContextMenuStrip!;

    protected static ToolStripMenuItem AddItem(WfuiTabControl tabs) => (ToolStripMenuItem)Menu(tabs).Items[0];

    protected static ToolStripMenuItem RenameItem(WfuiTabControl tabs) => (ToolStripMenuItem)Menu(tabs).Items[1];

    protected static ToolStripMenuItem CloseItem(WfuiTabControl tabs) => (ToolStripMenuItem)Menu(tabs).Items[2];

    protected static void RightClick(WfuiTabControl tabs, int tabIndex)
    {
        Rectangle rect = tabs.GetTabRect(tabIndex);
        tabs.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, rect.Left + 8, rect.Top + 8, 0));
    }

    protected static void Open(WfuiTabControl tabs)
    {
        Menu(tabs).InvokePrivate("OnOpening", new CancelEventArgs());
    }

    [DllImport("user32.dll")]
    protected static extern System.IntPtr SendMessage(System.IntPtr hWnd, int message, System.IntPtr wParam, System.IntPtr lParam);
}
