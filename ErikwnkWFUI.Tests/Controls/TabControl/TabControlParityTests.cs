using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// The themed tab control behaves like the standard one: selection, pages, mouse events, hot tracking.
/// </summary>
[Collection(FocusTestCollection.Name)]
public class TabControlParityTests : TabControlParityTestBase
{
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
