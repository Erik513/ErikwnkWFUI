using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// The themed tab controls are meant to behave exactly like the standard
/// TabControl and only look different. These tests do not assume that: each
/// scenario is run on a standard System.Windows.Forms.TabControl and on the
/// themed ones, the same steps each time, and what can be observed -
/// events and their order, the selection, geometry, defaults - has to come
/// out identical. A scenario that observes nothing would prove nothing, so
/// each one also checks that its log is not empty.
/// Shown forms and real window messages: in the focus collection. The
/// editable control only repeats the scenarios where what it adds (its
/// mouse, selection and resize hooks) could make a difference - layout and
/// defaults are the read-only control's business.
/// </summary>
public abstract class TabControlParityTestBase
{
    [DllImport("user32.dll")]
    protected static extern System.IntPtr SendMessage(System.IntPtr hWnd, int message, System.IntPtr wParam, System.IntPtr lParam);

    protected const int WM_LBUTTONDOWN = 0x0201;

    protected const int WM_LBUTTONUP = 0x0202;

    protected const int WM_LBUTTONDBLCLK = 0x0203;

    protected const int WM_RBUTTONDOWN = 0x0204;

    protected const int WM_RBUTTONUP = 0x0205;

    protected const int WM_KEYDOWN = 0x0100;

    protected const int WM_KEYUP = 0x0101;

    protected const int VK_LEFT = 0x25;

    protected const int VK_RIGHT = 0x27;

    public enum Kind
    {
        ReadOnly,
        Editable
    }

    protected static System.Windows.Forms.TabControl Create(Kind? kind)
    {
        switch (kind)
        {
            case Kind.ReadOnly:
                return new ErikwnkWFUI.Controls.ReadOnlyTabControl();

            case Kind.Editable:
                return new ErikwnkWFUI.Controls.TabControl();

            default:
                return new System.Windows.Forms.TabControl();
        }
    }

    protected static void Pump()
    {
        for (int i = 0; i < 4; i++)
        {
            Application.DoEvents();
        }
    }

    // A form with a tab control of three pages, shown, then the scenario.
    protected static List<string> Run(Kind? kind, System.Action<System.Windows.Forms.TabControl, List<string>> scenario, System.Action<System.Windows.Forms.TabControl>? configure = null, string[]? pages = null)
    {
        List<string> log = new List<string>();

        StaThread.Run(() =>
        {
            System.Windows.Forms.TabControl tabs = Create(kind);
            tabs.Dock = DockStyle.Fill;

            foreach (string title in pages ?? new[] { "One", "Two", "Three" })
            {
                tabs.TabPages.Add(title);
            }

            configure?.Invoke(tabs);

            using Form host = new Form
            {
                ClientSize = new Size(400, 200),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-3000, -3000),
                ShowInTaskbar = false
            };

            host.Controls.Add(tabs);
            host.Show();
            Pump();

            scenario(tabs, log);
        });

        return log;
    }

    // Standard first, then the themed one - the same scenario.
    protected static void AssertSame(Kind kind, System.Action<System.Windows.Forms.TabControl, List<string>> scenario, System.Action<System.Windows.Forms.TabControl>? configure = null, string[]? pages = null)
    {
        List<string> expected = Run(null, scenario, configure, pages);
        List<string> actual = Run(kind, scenario, configure, pages);

        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
    }

    // Runs a step and logs how it ended - an exception is an observation
    // too (the standard control throws for some arguments).
    protected static void Try(List<string> log, string label, System.Action step)
    {
        try
        {
            step();
            log.Add(label + " ok");
        }
        catch (System.Exception ex)
        {
            log.Add(label + " threw " + ex.GetType().Name);
        }
    }

    // What the selection events report, in the order they come.
    protected static void Watch(System.Windows.Forms.TabControl tabs, List<string> log)
    {
        tabs.Selecting += (s, e) => log.Add($"Selecting index={e.TabPageIndex} page={e.TabPage?.Text} action={e.Action} cancel={e.Cancel}");
        tabs.Selected += (s, e) => log.Add($"Selected index={e.TabPageIndex} page={e.TabPage?.Text} action={e.Action}");
        tabs.Deselecting += (s, e) => log.Add($"Deselecting index={e.TabPageIndex} page={e.TabPage?.Text} action={e.Action} cancel={e.Cancel}");
        tabs.Deselected += (s, e) => log.Add($"Deselected index={e.TabPageIndex} page={e.TabPage?.Text} action={e.Action}");
        tabs.SelectedIndexChanged += (s, e) => log.Add($"SelectedIndexChanged now={tabs.SelectedIndex}");
    }

    protected static void ClickTab(System.Windows.Forms.TabControl tabs, int index, int message = WM_LBUTTONDOWN, int upMessage = WM_LBUTTONUP)
    {
        Rectangle tab = tabs.GetTabRect(index);
        System.IntPtr at = (System.IntPtr)((tab.Left + 10) | ((tab.Top + 8) << 16));
        SendMessage(tabs.Handle, message, (System.IntPtr)1, at);
        SendMessage(tabs.Handle, upMessage, System.IntPtr.Zero, at);
        Pump();
    }

    protected static void PressKey(System.Windows.Forms.TabControl tabs, int virtualKey)
    {
        SendMessage(tabs.Handle, WM_KEYDOWN, (System.IntPtr)virtualKey, System.IntPtr.Zero);
        SendMessage(tabs.Handle, WM_KEYUP, (System.IntPtr)virtualKey, System.IntPtr.Zero);
        Pump();
    }

    // The exact size of a tab that sizes itself is NOT compared: with the
    // themed controls' painting the native control lays an automatic tab out
    // a little larger than it does for the standard one (see
    // TheAutomaticTabSize_IsALittleLargerThanTheStandardControls - a known,
    // deliberate difference). What has to match is everything that does not
    // depend on that: where the strip sits, the order of the tabs, that the
    // pages stay inside the control, and every size that is given explicitly.
    protected static string Shape(System.Windows.Forms.TabControl tabs)
    {
        Rectangle display = tabs.DisplayRectangle;
        List<Rectangle> rects = Enumerable.Range(0, tabs.TabCount).Select(i => tabs.GetTabRect(i)).ToList();
        bool vertical = tabs.Alignment == TabAlignment.Left || tabs.Alignment == TabAlignment.Right;

        string side;

        switch (tabs.Alignment)
        {
            case TabAlignment.Bottom:
                side = rects.All(r => r.Top >= display.Bottom - 2) ? "strip below the page" : "strip elsewhere";
                break;

            case TabAlignment.Left:
                side = rects.All(r => r.Right <= display.Left + 2) ? "strip left of the page" : "strip elsewhere";
                break;

            case TabAlignment.Right:
                side = rects.All(r => r.Left >= display.Right - 2) ? "strip right of the page" : "strip elsewhere";
                break;

            default:
                side = rects.All(r => r.Bottom <= display.Top + 2) ? "strip above the page" : "strip elsewhere";
                break;
        }

        bool inside = tabs.ClientRectangle.Contains(display);
        // Within a row the tabs run in index order. (Rows themselves may
        // wrap differently and the native control moves the selected row
        // next to the page, so the rows are not compared with each other.)
        bool ordered = Enumerable.Range(0, rects.Count)
            .GroupBy(i => vertical ? rects[i].Left : rects[i].Top)
            .All(row => row.Zip(row.Skip(1), (first, second) => vertical
                ? rects[first].Top <= rects[second].Top
                : rects[first].Left <= rects[second].Left).All(x => x));

        return $"{side}; pageInside={inside}; tabsInOrder={ordered}; tabs={rects.Count}";
    }

    protected static string Defaults(System.Windows.Forms.TabControl tabs)
    {
        return string.Join(
            " ",
            $"Alignment={tabs.Alignment}",
            $"Multiline={tabs.Multiline}",
            $"SizeMode={tabs.SizeMode}",
            $"ShowToolTips={tabs.ShowToolTips}",
            $"TabStop={tabs.TabStop}",
            $"Padding={tabs.Padding}",
            $"ItemSize={tabs.ItemSize}",
            $"DrawMode={tabs.DrawMode}",
            $"RightToLeftLayout={tabs.RightToLeftLayout}",
            $"SelectedIndex={tabs.SelectedIndex}",
            $"TabCount={tabs.TabCount}",
            $"Dock={tabs.Dock}",
            $"Size={tabs.Size}");
    }
}

internal static class TabControlParityExtensions
{
    // A mouse move over the tab control, as a real pointer would make it.
    public static void InvokeMouseMove(this System.Windows.Forms.TabControl tabs, int x, int y)
    {
        System.IntPtr at = (System.IntPtr)(x | (y << 16));
        SendMessage(tabs.Handle, 0x0200, System.IntPtr.Zero, at);
    }

    [DllImport("user32.dll")]
    private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int message, System.IntPtr wParam, System.IntPtr lParam);
}
