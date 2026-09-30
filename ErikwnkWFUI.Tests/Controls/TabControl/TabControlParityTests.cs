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
[Collection(FocusTestCollection.Name)]
public class TabControlParityTests
{
    [DllImport("user32.dll")]
    private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int message, System.IntPtr wParam, System.IntPtr lParam);

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int VK_LEFT = 0x25;
    private const int VK_RIGHT = 0x27;

    public enum Kind
    {
        ReadOnly,
        Editable
    }

    private static System.Windows.Forms.TabControl Create(Kind? kind)
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

    private static void Pump()
    {
        for (int i = 0; i < 4; i++)
        {
            Application.DoEvents();
        }
    }

    // A form with a tab control of three pages, shown, then the scenario.
    private static List<string> Run(Kind? kind, System.Action<System.Windows.Forms.TabControl, List<string>> scenario, System.Action<System.Windows.Forms.TabControl>? configure = null, string[]? pages = null)
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
    private static void AssertSame(Kind kind, System.Action<System.Windows.Forms.TabControl, List<string>> scenario, System.Action<System.Windows.Forms.TabControl>? configure = null, string[]? pages = null)
    {
        List<string> expected = Run(null, scenario, configure, pages);
        List<string> actual = Run(kind, scenario, configure, pages);

        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
    }

    // Runs a step and logs how it ended - an exception is an observation
    // too (the standard control throws for some arguments).
    private static void Try(List<string> log, string label, System.Action step)
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
    private static void Watch(System.Windows.Forms.TabControl tabs, List<string> log)
    {
        tabs.Selecting += (s, e) => log.Add($"Selecting index={e.TabPageIndex} page={e.TabPage?.Text} action={e.Action} cancel={e.Cancel}");
        tabs.Selected += (s, e) => log.Add($"Selected index={e.TabPageIndex} page={e.TabPage?.Text} action={e.Action}");
        tabs.Deselecting += (s, e) => log.Add($"Deselecting index={e.TabPageIndex} page={e.TabPage?.Text} action={e.Action} cancel={e.Cancel}");
        tabs.Deselected += (s, e) => log.Add($"Deselected index={e.TabPageIndex} page={e.TabPage?.Text} action={e.Action}");
        tabs.SelectedIndexChanged += (s, e) => log.Add($"SelectedIndexChanged now={tabs.SelectedIndex}");
    }

    private static void ClickTab(System.Windows.Forms.TabControl tabs, int index, int message = WM_LBUTTONDOWN, int upMessage = WM_LBUTTONUP)
    {
        Rectangle tab = tabs.GetTabRect(index);
        System.IntPtr at = (System.IntPtr)((tab.Left + 10) | ((tab.Top + 8) << 16));
        SendMessage(tabs.Handle, message, (System.IntPtr)1, at);
        SendMessage(tabs.Handle, upMessage, System.IntPtr.Zero, at);
        Pump();
    }

    private static void PressKey(System.Windows.Forms.TabControl tabs, int virtualKey)
    {
        SendMessage(tabs.Handle, WM_KEYDOWN, (System.IntPtr)virtualKey, System.IntPtr.Zero);
        SendMessage(tabs.Handle, WM_KEYUP, (System.IntPtr)virtualKey, System.IntPtr.Zero);
        Pump();
    }

    // ---- selection and its events ----

    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void ChangingTheSelectionInCode_RaisesTheSameEventsInTheSameOrder(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            Watch(tabs, log);
            tabs.SelectedIndex = 2;
            tabs.SelectedIndex = 0;
            tabs.SelectedIndex = 0;
            tabs.SelectedTab = tabs.TabPages[1];
        });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void TheSelectMethods_BehaveTheSame(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            Watch(tabs, log);
            Try(log, "SelectTab(2)", () => tabs.SelectTab(2));
            Try(log, "SelectTab(99)", () => tabs.SelectTab(99));
            Try(log, "SelectTab(\"none\")", () => tabs.SelectTab("none"));
            Try(log, "SelectTab(page)", () => tabs.SelectTab(tabs.TabPages[0]));
            Try(log, "DeselectTab(0)", () => tabs.DeselectTab(0));
            log.Add("selected=" + tabs.SelectedIndex);
            Try(log, "DeselectTab(page)", () => tabs.DeselectTab(tabs.TabPages[1]));
            log.Add("selected=" + tabs.SelectedIndex);
        });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void CancellingInSelecting_KeepsTheSelection_Identically(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            Watch(tabs, log);
            tabs.Selecting += (s, e) => e.Cancel = e.TabPageIndex == 1;

            tabs.SelectedIndex = 1;
            log.Add("selected=" + tabs.SelectedIndex);
            tabs.SelectedIndex = 2;
            log.Add("selected=" + tabs.SelectedIndex);
        });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void CancellingInDeselecting_KeepsTheSelection_Identically(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            Watch(tabs, log);
            tabs.Deselecting += (s, e) => e.Cancel = true;

            tabs.SelectedIndex = 2;
            log.Add("selected=" + tabs.SelectedIndex);
        });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void ClickingATab_SelectsItAndRaisesTheSameEvents(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            Watch(tabs, log);
            ClickTab(tabs, 2);
            log.Add("selected=" + tabs.SelectedIndex);
            ClickTab(tabs, 2);
            log.Add("selected=" + tabs.SelectedIndex);
            ClickTab(tabs, 0);
            log.Add("selected=" + tabs.SelectedIndex);
        });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void TheArrowKeys_MoveTheSelectionTheSame(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            tabs.Focus();
            Pump();
            Watch(tabs, log);

            PressKey(tabs, VK_RIGHT);
            log.Add("selected=" + tabs.SelectedIndex);
            PressKey(tabs, VK_RIGHT);
            PressKey(tabs, VK_RIGHT);
            log.Add("selected=" + tabs.SelectedIndex);
            PressKey(tabs, VK_LEFT);
            log.Add("selected=" + tabs.SelectedIndex);
        });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void ClickingATabPutsTheFocusOnTheTabControl_Identically(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            ClickTab(tabs, 1);
            log.Add("focused=" + tabs.Focused);
            log.Add("selected=" + tabs.SelectedIndex);
        });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void ADisabledPage_CanStillBeSelected_LikeInTheStandardControl(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            tabs.TabPages[1].Enabled = false;
            ClickTab(tabs, 1);
            log.Add("selected=" + tabs.SelectedIndex);
            log.Add("page enabled=" + tabs.TabPages[1].Enabled);
        });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void ADisabledControl_IgnoresClicks_LikeTheStandardControl(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            tabs.Enabled = false;
            ClickTab(tabs, 2);
            log.Add("selected=" + tabs.SelectedIndex);
        });
    }

    // ---- the page collection ----

    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void AddingAndInsertingPages_LeavesTheSelectionWhereTheStandardControlDoes(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            Watch(tabs, log);
            tabs.TabPages.Add("Four");
            log.Add($"selected={tabs.SelectedIndex} count={tabs.TabCount}");
            tabs.TabPages.Insert(0, new TabPage("Zero"));
            log.Add($"selected={tabs.SelectedIndex} count={tabs.TabCount} first={tabs.TabPages[0].Text}");
            tabs.SelectedIndex = 3;
            tabs.TabPages.Insert(1, new TabPage("Inserted"));
            log.Add($"selected={tabs.SelectedIndex} page={tabs.SelectedTab?.Text}");
        });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void RemovingPages_SelectsWhatTheStandardControlSelects(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            Watch(tabs, log);
            tabs.SelectedIndex = 2;
            log.Add("--- remove the selected last one");
            tabs.TabPages.RemoveAt(2);
            log.Add($"selected={tabs.SelectedIndex} page={tabs.SelectedTab?.Text}");

            log.Add("--- remove one in front of the selected");
            tabs.TabPages.Add("Again");
            tabs.SelectedIndex = 2;
            tabs.TabPages.RemoveAt(0);
            log.Add($"selected={tabs.SelectedIndex} page={tabs.SelectedTab?.Text}");

            log.Add("--- remove the selected first one");
            tabs.SelectedIndex = 0;
            tabs.TabPages.RemoveAt(0);
            log.Add($"selected={tabs.SelectedIndex} page={tabs.SelectedTab?.Text}");

            log.Add("--- remove the rest");
            tabs.TabPages.Clear();
            log.Add($"selected={tabs.SelectedIndex} count={tabs.TabCount} page={tabs.SelectedTab?.Text}");
        });
    }

    // ---- mouse events ----

    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void ALeftClick_RaisesTheSameMouseEventsInTheSameOrder(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            tabs.MouseDown += (s, e) => log.Add($"MouseDown {e.Button}");
            tabs.MouseUp += (s, e) => log.Add($"MouseUp {e.Button}");
            tabs.Click += (s, e) => log.Add("Click");
            tabs.MouseClick += (s, e) => log.Add($"MouseClick {e.Button}");
            tabs.DoubleClick += (s, e) => log.Add("DoubleClick");
            tabs.MouseDoubleClick += (s, e) => log.Add($"MouseDoubleClick {e.Button}");

            ClickTab(tabs, 1);
            ClickTab(tabs, 1, WM_LBUTTONDBLCLK, WM_LBUTTONUP);
        });
    }

    // The right button is left out for the editable control: there it
    // opens its menu and selects the tab, both on purpose (see
    // TabControlContextMenuTests) - the read-only one is the standard
    // behavior.
    [Fact]
    public void ARightClick_OnTheReadOnlyControl_DoesNotSelect_LikeTheStandardControl()
    {
        System.Action<System.Windows.Forms.TabControl, List<string>> scenario = (tabs, log) =>
        {
            Watch(tabs, log);
            tabs.MouseDown += (s, e) => log.Add($"MouseDown {e.Button}");
            tabs.MouseUp += (s, e) => log.Add($"MouseUp {e.Button}");

            Rectangle tab = tabs.GetTabRect(2);
            System.IntPtr at = (System.IntPtr)((tab.Left + 10) | ((tab.Top + 8) << 16));
            SendMessage(tabs.Handle, WM_RBUTTONDOWN, (System.IntPtr)2, at);
            SendMessage(tabs.Handle, WM_RBUTTONUP, System.IntPtr.Zero, at);
            Pump();
            log.Add("selected=" + tabs.SelectedIndex);
        };

        Assert.Equal(Run(null, scenario), Run(Kind.ReadOnly, scenario));
    }

    // ---- geometry ----

    // The exact size of a tab that sizes itself is NOT compared: with the
    // themed controls' painting the native control lays an automatic tab out
    // a little larger than it does for the standard one (see
    // TheAutomaticTabSize_IsALittleLargerThanTheStandardControls - a known,
    // deliberate difference). What has to match is everything that does not
    // depend on that: where the strip sits, the order of the tabs, that the
    // pages stay inside the control, and every size that is given explicitly.
    private static string Shape(System.Windows.Forms.TabControl tabs)
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

    [Theory]
    [InlineData(Kind.ReadOnly, TabAlignment.Top, false)]
    [InlineData(Kind.ReadOnly, TabAlignment.Bottom, false)]
    [InlineData(Kind.ReadOnly, TabAlignment.Left, false)]
    [InlineData(Kind.ReadOnly, TabAlignment.Right, false)]
    [InlineData(Kind.ReadOnly, TabAlignment.Top, true)]
    [InlineData(Kind.ReadOnly, TabAlignment.Left, true)]
    public void TheStripSitsOnTheSameSideOfThePage(Kind kind, TabAlignment alignment, bool multiline)
    {
        AssertSame(
            kind,
            (tabs, log) => log.Add(Shape(tabs)),
            tabs =>
            {
                tabs.Alignment = alignment;
                tabs.Multiline = multiline;
            },
            new[] { "Overview", "Details", "A much longer tab name", "Four", "Five", "Six" });
    }

    [Theory]
    [InlineData(Kind.ReadOnly, TabSizeMode.Normal)]
    [InlineData(Kind.ReadOnly, TabSizeMode.FillToRight)]
    [InlineData(Kind.ReadOnly, TabSizeMode.Fixed)]
    public void TheSizeModes_KeepTheShapeOfTheLayout(Kind kind, TabSizeMode mode)
    {
        AssertSame(
            kind,
            (tabs, log) => log.Add(Shape(tabs)),
            tabs =>
            {
                tabs.Multiline = true;
                tabs.SizeMode = mode;
            },
            new[] { "Overview", "Details", "A longer one", "Four", "Five", "Six" });
    }

    // FillToRight is about the rows reaching the right edge.
    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void FillToRight_StretchesEveryRowToTheRightEdge_LikeTheStandardControl(Kind kind)
    {
        AssertSame(
            kind,
            (tabs, log) =>
            {
                foreach (IGrouping<int, int> row in Enumerable.Range(0, tabs.TabCount).GroupBy(i => tabs.GetTabRect(i).Top))
                {
                    int right = row.Max(i => tabs.GetTabRect(i).Right);
                    log.Add($"row reaches the edge={right >= tabs.Width - 6}");
                }
            },
            tabs =>
            {
                tabs.Multiline = true;
                tabs.SizeMode = TabSizeMode.FillToRight;
            },
            new[] { "Overview", "Details", "A longer one", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten" });
    }

    // Sizes that are given are kept exactly - these must be identical.
    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void ExplicitItemSizeAndPadding_ProduceTheExactSameTabs(Kind kind)
    {
        AssertSame(
            kind,
            (tabs, log) =>
            {
                log.Add($"rows={tabs.RowCount} display={tabs.DisplayRectangle}");
                log.Add(string.Join(";", Enumerable.Range(0, tabs.TabCount).Select(i => tabs.GetTabRect(i).ToString())));
            },
            tabs =>
            {
                tabs.SizeMode = TabSizeMode.Fixed;
                tabs.ItemSize = new Size(90, 28);
                tabs.Padding = new Point(12, 6);
            });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void ChangingATabsText_ResizesItInTheSameDirection(Kind kind)
    {
        AssertSame(kind, (tabs, log) =>
        {
            int before = tabs.GetTabRect(1).Width;
            tabs.TabPages[1].Text = "A much longer name now";
            int longer = tabs.GetTabRect(1).Width;
            tabs.TabPages[1].Text = "x";
            int shorter = tabs.GetTabRect(1).Width;

            log.Add("longer text is wider=" + (longer > before));
            log.Add("shorter text is narrower=" + (shorter < longer));
            log.Add("neighbour moved along=" + (tabs.GetTabRect(2).Left > tabs.GetTabRect(1).Left));
        });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void TabImages_MakeATabWiderInBoth(Kind kind)
    {
        using ImageList images = new ImageList { ImageSize = new Size(16, 16) };
        using Bitmap icon = new Bitmap(16, 16);
        images.Images.Add(icon);

        AssertSame(
            kind,
            (tabs, log) =>
            {
                log.Add("tab with an image is wider=" + (tabs.GetTabRect(0).Width > tabs.GetTabRect(1).Width));
            },
            tabs =>
            {
                tabs.ImageList = images;
                tabs.TabPages[0].Text = "Two";
                tabs.TabPages[0].ImageIndex = 0;
            });
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void ResizingTheControl_WrapsAndUnwrapsTheRowsTheSame(Kind kind)
    {
        AssertSame(
            kind,
            (tabs, log) =>
            {
                Form host = (Form)tabs.FindForm()!;
                log.Add("wide: " + (tabs.RowCount > 1 ? "wrapped" : "one row"));
                host.ClientSize = new Size(180, 120);
                Pump();
                log.Add("narrow: " + (tabs.RowCount > 1 ? "wrapped" : "one row"));
                log.Add(Shape(tabs));
                host.ClientSize = new Size(700, 300);
                Pump();
                log.Add("wide again: " + (tabs.RowCount > 1 ? "wrapped" : "one row"));
            },
            tabs => tabs.Multiline = true,
            new[] { "Overview", "Details", "Notes", "Reports", "Archive", "Settings" });
    }

    // The one deliberate difference, written down so it cannot grow
    // unnoticed: while the native control sizes a tab itself, the themed
    // controls' tabs come out a little larger (about 6 px wider and 1 px
    // taller for a short name on .NET 8 without visual styles, about 12 and 3
    // with them on .NET Framework) than the standard control's. The tab
    // heights and the page area start follow from it.
    [Fact]
    public void TheAutomaticTabSize_IsALittleLargerThanTheStandardControls()
    {
        Size standard = default;
        Size themed = default;

        Run(null, (tabs, log) =>
        {
            standard = tabs.GetTabRect(0).Size;
            log.Add("x");
        });
        Run(Kind.ReadOnly, (tabs, log) =>
        {
            themed = tabs.GetTabRect(0).Size;
            log.Add("x");
        });

        Assert.True(themed.Width >= standard.Width);
        Assert.True(themed.Height >= standard.Height);
        Assert.True(themed.Width - standard.Width <= 14);
        Assert.True(themed.Height - standard.Height <= 4);
    }

    // ---- defaults ----

    private static string Defaults(System.Windows.Forms.TabControl tabs)
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

    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void TheDefaultPropertiesAreTheStandardOnes(Kind kind)
    {
        using System.Windows.Forms.TabControl standard = Create(null);
        using System.Windows.Forms.TabControl themed = Create(kind);

        Assert.Equal(Defaults(standard), Defaults(themed));
    }

    [Theory]
    [InlineData(Kind.ReadOnly)]
    public void TheDefaultsOfANewPage_AreTheStandardOnes(Kind kind)
    {
        using System.Windows.Forms.TabControl standard = Create(null);
        using System.Windows.Forms.TabControl themed = Create(kind);
        standard.TabPages.Add("a");
        themed.TabPages.Add("a");

        TabPage expected = standard.TabPages[0];
        TabPage actual = themed.TabPages[0];

        Assert.Equal(
            $"{expected.Text} {expected.Enabled} {expected.Visible} {expected.ImageIndex} {expected.ToolTipText} {expected.Dock} {expected.BorderStyle}",
            $"{actual.Text} {actual.Enabled} {actual.Visible} {actual.ImageIndex} {actual.ToolTipText} {actual.Dock} {actual.BorderStyle}");
        Assert.Equal(standard.SelectedIndex, themed.SelectedIndex);
    }

    // The one default that differs on purpose: a hover highlight is part of
    // the themed look, so HotTrack is on where the standard control has it off.
    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void HotTrack_IsOnByDefault_UnlikeTheStandardControl(Kind kind)
    {
        using System.Windows.Forms.TabControl standard = Create(null);
        using System.Windows.Forms.TabControl themed = Create(kind);

        Assert.False(standard.HotTrack);
        Assert.True(themed.HotTrack);
    }

    // ---- hot tracking is only a look ----

    [Theory]
    [InlineData(Kind.ReadOnly)]
    [InlineData(Kind.Editable)]
    public void HotTrack_ChangesNothingAboutTheSelection(Kind kind)
    {
        AssertSame(
            kind,
            (tabs, log) =>
            {
                Watch(tabs, log);
                Rectangle tab = tabs.GetTabRect(2);
                tabs.InvokeMouseMove(tab.Left + 10, tab.Top + 8);
                Pump();
                log.Add("selected=" + tabs.SelectedIndex);
                ClickTab(tabs, 2);
                log.Add("selected=" + tabs.SelectedIndex);
            },
            tabs => tabs.HotTrack = true);
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
