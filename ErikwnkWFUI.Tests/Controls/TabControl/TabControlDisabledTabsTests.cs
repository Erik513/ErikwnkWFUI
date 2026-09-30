using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiReadOnlyTabControl = ErikwnkWFUI.Controls.ReadOnlyTabControl;
using WfuiTabControl = ErikwnkWFUI.Controls.TabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// AllowSelectingDisabledTabs: on by default (the standard control lets a
/// disabled page be opened), and when off a disabled tab cannot be selected
/// - by click, keyboard or code - while the arrow keys and Ctrl+Tab step
/// over it instead of getting stuck in front of it.
/// </summary>
[Collection(FocusTestCollection.Name)]
public class TabControlDisabledTabsTests
{
    [DllImport("user32.dll")]
    private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int message, System.IntPtr wParam, System.IntPtr lParam);

    // Pages: 0 One, 1 Two (disabled), 2 Three, 3 Four (disabled), 4 Five.
    private static WfuiReadOnlyTabControl CreateTabs(bool allowDisabled)
    {
        WfuiReadOnlyTabControl tabs = new WfuiReadOnlyTabControl { Size = new Size(500, 200), AllowSelectingDisabledTabs = allowDisabled };

        foreach (string title in new[] { "One", "Two", "Three", "Four", "Five" })
        {
            tabs.TabPages.Add(title);
        }

        tabs.TabPages[1].Enabled = false;
        tabs.TabPages[3].Enabled = false;
        _ = tabs.Handle;
        return tabs;
    }

    private static void Press(WfuiReadOnlyTabControl tabs, Keys key)
    {
        tabs.InvokePrivate("OnKeyDown", new KeyEventArgs(key));
    }

    // ---- the option ----

    [Fact]
    public void ADisabledTab_CanBeSelected_ByDefault_LikeInTheStandardControl()
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: true);

        tabs.SelectedIndex = 1;

        Assert.True(tabs.AllowSelectingDisabledTabs);
        Assert.Equal(1, tabs.SelectedIndex);
    }

    [Fact]
    public void ADisabledTab_CannotBeSelectedInCode_WhenOff()
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: false);

        tabs.SelectedIndex = 1;
        Assert.Equal(0, tabs.SelectedIndex);

        tabs.SelectedTab = tabs.TabPages[3];
        Assert.Equal(0, tabs.SelectedIndex);

        tabs.SelectedIndex = 2;
        Assert.Equal(2, tabs.SelectedIndex);
    }

    [Fact]
    public void Selecting_IsRaisedForTheRefusedTab_AlreadyCancelled()
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: false);
        bool? cancelSeen = null;
        tabs.Selecting += (s, e) =>
        {
            if (e.TabPageIndex == 1)
            {
                cancelSeen = e.Cancel;
            }
        };

        tabs.SelectedIndex = 1;

        Assert.True(cancelSeen);
    }

    [Fact]
    public void EnablingThePage_MakesItSelectable()
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: false);

        tabs.TabPages[1].Enabled = true;
        tabs.SelectedIndex = 1;

        Assert.Equal(1, tabs.SelectedIndex);
    }

    [Fact]
    public void ATabThatIsSelected_WhenItsPageGetsDisabled_StaysSelected()
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: false);
        tabs.SelectedIndex = 2;

        tabs.TabPages[2].Enabled = false;

        Assert.Equal(2, tabs.SelectedIndex);
    }

    [Fact]
    public void TurningTheOptionOnAgain_LetsDisabledTabsBeSelectedAgain()
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: false);

        tabs.AllowSelectingDisabledTabs = true;
        tabs.SelectedIndex = 1;

        Assert.Equal(1, tabs.SelectedIndex);
    }

    [Fact]
    public void TheEditableControl_HasTheOptionToo()
    {
        using WfuiTabControl tabs = new WfuiTabControl { AllowSelectingDisabledTabs = false };
        tabs.TabPages.Add("a");
        tabs.TabPages.Add("b");
        tabs.TabPages[1].Enabled = false;
        _ = tabs.Handle;

        tabs.SelectedIndex = 1;

        Assert.Equal(0, tabs.SelectedIndex);
    }

    // ---- the keyboard steps over them ----

    [Fact]
    public void TheRightArrow_StepsOverDisabledTabs()
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: false);

        Press(tabs, Keys.Right);
        Assert.Equal(2, tabs.SelectedIndex);

        Press(tabs, Keys.Right);
        Assert.Equal(4, tabs.SelectedIndex);
    }

    [Fact]
    public void TheLeftArrow_StepsOverDisabledTabs()
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: false);
        tabs.SelectedIndex = 4;

        Press(tabs, Keys.Left);
        Assert.Equal(2, tabs.SelectedIndex);

        Press(tabs, Keys.Left);
        Assert.Equal(0, tabs.SelectedIndex);
    }

    [Fact]
    public void TheArrowKeys_StopAtTheEnds_LikeTheStandardControl()
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: false);

        Press(tabs, Keys.Left);
        Assert.Equal(0, tabs.SelectedIndex);

        tabs.SelectedIndex = 4;
        Press(tabs, Keys.Right);
        Assert.Equal(4, tabs.SelectedIndex);
    }

    [Fact]
    public void TheArrowKeys_FindNothing_WhenEveryTabBeyondIsDisabled()
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: false);
        tabs.TabPages[4].Enabled = false;
        tabs.SelectedIndex = 2;

        Press(tabs, Keys.Right);

        Assert.Equal(2, tabs.SelectedIndex);
    }

    [Fact]
    public void OnAVerticalStrip_TheUpAndDownArrowsDoTheWork()
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: false);
        tabs.Alignment = TabAlignment.Left;

        Press(tabs, Keys.Down);
        Assert.Equal(2, tabs.SelectedIndex);

        Press(tabs, Keys.Up);
        Assert.Equal(0, tabs.SelectedIndex);
    }

    [Fact]
    public void TheArrowKeys_AreLeftToTheNativeControl_WhenTheOptionIsOn()
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: true);
        KeyEventArgs args = new KeyEventArgs(Keys.Right);

        tabs.InvokePrivate("OnKeyDown", args);

        Assert.False(args.Handled);
    }

    // Ctrl+Tab and its relatives - through the step that does the work.
    [Theory]
    [InlineData(0, 1, 2)]
    [InlineData(2, 1, 4)]
    [InlineData(4, 1, 0)]
    [InlineData(0, -1, 4)]
    [InlineData(4, -1, 2)]
    [InlineData(2, -1, 0)]
    public void TheWrappingStep_SkipsDisabledTabs_AndGoesAroundTheEnds(int from, int direction, int expected)
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: false);
        tabs.SelectedIndex = from;

        tabs.InvokePrivate("SelectEnabledTab", direction, true);

        Assert.Equal(expected, tabs.SelectedIndex);
    }

    [Fact]
    public void TheWrappingStep_DoesNothing_WhenNoOtherTabIsEnabled()
    {
        using WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: false);
        tabs.TabPages[2].Enabled = false;
        tabs.TabPages[4].Enabled = false;

        tabs.InvokePrivate("SelectEnabledTab", 1, true);

        Assert.Equal(0, tabs.SelectedIndex);
    }

    // ---- the mouse ----

    [Fact]
    public void AClickOnADisabledTab_SelectsNothing_WhenOff()
    {
        StaThread.Run(() =>
        {
            using Form host = new Form
            {
                ClientSize = new Size(500, 200),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-3000, -3000),
                ShowInTaskbar = false
            };
            WfuiReadOnlyTabControl tabs = CreateTabs(allowDisabled: false);
            tabs.Dock = DockStyle.Fill;
            host.Controls.Add(tabs);
            host.Show();
            Application.DoEvents();

            Rectangle disabled = tabs.GetTabRect(1);
            System.IntPtr at = (System.IntPtr)((disabled.Left + 10) | ((disabled.Top + 8) << 16));
            SendMessage(tabs.Handle, 0x201, (System.IntPtr)1, at);
            SendMessage(tabs.Handle, 0x202, System.IntPtr.Zero, at);
            Application.DoEvents();

            Assert.Equal(0, tabs.SelectedIndex);

            Rectangle enabled = tabs.GetTabRect(2);
            at = (System.IntPtr)((enabled.Left + 10) | ((enabled.Top + 8) << 16));
            SendMessage(tabs.Handle, 0x201, (System.IntPtr)1, at);
            SendMessage(tabs.Handle, 0x202, System.IntPtr.Zero, at);
            Application.DoEvents();

            Assert.Equal(2, tabs.SelectedIndex);
        });
    }
}
