using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Controls.DataGridView;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// What happens to a control on a page - whatever control it is - when the
/// tab is switched while it has the focus: it must lose the focus, so the
/// next keystrokes do not land in a control the user can no longer see
/// (Enter opening a cell edit, a typed letter appearing in a hidden text
/// box, Space toggling a hidden check box).
///
/// Every control kind is checked the same way, against the themed tab
/// controls and the standard TabControl side by side, so "behaves like the
/// standard one" is compared rather than assumed. Each kind also has a
/// control experiment: without a tab switch the very same keys do change its
/// state - otherwise "state unchanged after the switch" would prove nothing.
/// Uses a shown form: focus needs real windows.
/// </summary>
[Collection(FocusTestCollection.Name)]
public class TabControlFocusTests
{
    [DllImport("user32.dll")]
    private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int message, System.IntPtr wParam, System.IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern System.IntPtr GetFocus();

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_CHAR = 0x0102;

    public enum Kind
    {
        Standard,
        ReadOnly,
        Editable
    }

    public enum Content
    {
        TextBox,
        ComboBox,
        CheckBox,
        Button,
        ListBox,
        NumericUpDown,
        DataGridView,
        ListView,
        ThemedListBox,
        ThemedComboBox,
        ThemedTextBox
    }

    // A control on a page, plus a description of its observable state that
    // changes when keys reach it.
    private sealed class Subject
    {
        public Subject(Control control, System.Func<string> state)
        {
            Control = control;
            State = state;
        }

        public Control Control { get; }

        public System.Func<string> State { get; }
    }

    private static Subject CreateSubject(Content content)
    {
        switch (content)
        {
            case Content.TextBox:
            {
                TextBox box = new TextBox();
                return new Subject(box, () => box.Text);
            }

            case Content.ComboBox:
            {
                ComboBox combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                combo.Items.AddRange(new object[] { "alpha", "bravo", "charlie" });
                combo.SelectedIndex = 0;
                return new Subject(combo, () => combo.SelectedIndex.ToString());
            }

            case Content.CheckBox:
            {
                CheckBox check = new CheckBox { Text = "check" };
                return new Subject(check, () => check.Checked.ToString());
            }

            case Content.Button:
            {
                int clicks = 0;
                Button button = new Button { Text = "button" };
                button.Click += (s, e) => clicks++;
                return new Subject(button, () => clicks.ToString());
            }

            case Content.ListBox:
            {
                System.Windows.Forms.ListBox list = new System.Windows.Forms.ListBox();
                list.Items.AddRange(new object[] { "alpha", "bravo", "charlie" });
                list.SelectedIndex = 0;
                return new Subject(list, () => list.SelectedIndex.ToString());
            }

            case Content.NumericUpDown:
            {
                NumericUpDown number = new NumericUpDown { Minimum = 0, Maximum = 1000, Value = 1 };
                return new Subject(number, () => number.Text);
            }

            case Content.DataGridView:
            {
                WfuiDataGridView grid = new WfuiDataGridView
                {
                    BindingContext = new BindingContext(),
                    DataSource = GridTestHelpers.CreateItems(("A", 1), ("B", 2))
                };
                return new Subject(grid, () => grid.IsCurrentCellInEditMode.ToString());
            }

            case Content.ThemedListBox:
            {
                ErikwnkWFUI.Controls.ListBox list = new ErikwnkWFUI.Controls.ListBox();
                list.Items.AddRange(new object[] { "alpha", "bravo", "charlie" });
                list.SelectedIndex = 0;
                return new Subject(list, () => list.SelectedIndex.ToString());
            }

            case Content.ThemedComboBox:
            {
                ComboBox combo = ErikwnkWFUI.UIStyles.ComboBoxes.CreateStandard();
                combo.Items.AddRange(new object[] { "alpha", "bravo", "charlie" });
                combo.SelectedIndex = 0;
                return new Subject(combo, () => combo.SelectedIndex.ToString());
            }

            case Content.ThemedTextBox:
            {
                TextBox box = ErikwnkWFUI.UIStyles.TextBoxes.CreateStandard();
                return new Subject(box, () => box.Text);
            }

            default:
            {
                ErikwnkWFUI.Controls.ListView list = new ErikwnkWFUI.Controls.ListView { View = View.Details };
                list.Columns.Add("Name", 100);
                list.Items.Add("alpha");
                list.Items.Add("bravo");
                list.Items.Add("charlie");
                list.Items[0].Selected = true;
                return new Subject(list, () => string.Join(",", list.SelectedIndices.Cast<int>()));
            }
        }
    }

    private static System.Windows.Forms.TabControl CreateTabs(Kind kind)
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

    private static Form Show(Kind kind, Content content, out System.Windows.Forms.TabControl tabs, out Subject subject)
    {
        tabs = CreateTabs(kind);
        tabs.Dock = DockStyle.Fill;
        tabs.TabPages.Add("one");
        tabs.TabPages.Add("two");

        subject = CreateSubject(content);
        subject.Control.Dock = DockStyle.Fill;
        tabs.TabPages[0].Controls.Add(subject.Control);

        Form host = new Form
        {
            ClientSize = new Size(400, 200),
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, -3000),
            ShowInTaskbar = false
        };

        host.Controls.Add(tabs);
        host.Show();
        Pump();

        subject.Control.Focus();
        Pump();
        return host;
    }

    private static void Pump()
    {
        for (int i = 0; i < 3; i++)
        {
            Application.DoEvents();
        }
    }

    private static void Press(System.IntPtr target, int virtualKey, char? character)
    {
        SendMessage(target, WM_KEYDOWN, (System.IntPtr)virtualKey, System.IntPtr.Zero);

        if (character.HasValue)
        {
            SendMessage(target, WM_CHAR, (System.IntPtr)character.Value, System.IntPtr.Zero);
        }

        SendMessage(target, WM_KEYUP, (System.IntPtr)virtualKey, System.IntPtr.Zero);
    }

    // Enter, Space, a letter and a digit - to whatever has the focus.
    private static void TypeWhereTheFocusIs(Form host)
    {
        System.IntPtr target = GetFocus() != System.IntPtr.Zero ? GetFocus() : host.Handle;

        Press(target, 0x0D, '\r');
        Press(target, 0x20, ' ');
        Press(target, 0x42, 'b');
        Press(target, 0x35, '5');

        // Generously: a button clicks on the key-up, which a slow moment of
        // the machine can leave a message-loop round or two behind.
        for (int i = 0; i < 8; i++)
        {
            Pump();
            System.Threading.Thread.Sleep(2);
        }
    }

    public static System.Collections.Generic.IEnumerable<object[]> Combinations()
    {
        foreach (Kind kind in new[] { Kind.Standard, Kind.ReadOnly, Kind.Editable })
        {
            foreach (Content content in System.Enum.GetValues(typeof(Content)))
            {
                yield return new object[] { kind, content };
            }
        }
    }

    public static System.Collections.Generic.IEnumerable<object[]> Contents()
    {
        foreach (Content content in System.Enum.GetValues(typeof(Content)))
        {
            yield return new object[] { content };
        }
    }

    // The control experiment.
    [Theory]
    [MemberData(nameof(Contents))]
    public void TheSameKeys_DoChangeTheControl_WhileItStillHasTheFocus(Content content)
    {
        StaThread.Run(() =>
        {
            using Form host = Show(Kind.Standard, content, out System.Windows.Forms.TabControl tabs, out Subject subject);
            string before = subject.State();

            TypeWhereTheFocusIs(host);

            Assert.NotEqual(before, subject.State());
        });
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void SwitchingTheTabInCode_TakesTheFocusOffTheControl(Kind kind, Content content)
    {
        StaThread.Run(() =>
        {
            using Form host = Show(kind, content, out System.Windows.Forms.TabControl tabs, out Subject subject);
            Assert.True(subject.Control.Focused);

            tabs.SelectedIndex = 1;
            Pump();

            Assert.False(subject.Control.Focused);
        });
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void KeysAfterSwitchingTheTab_DoNotReachTheControl(Kind kind, Content content)
    {
        StaThread.Run(() =>
        {
            using Form host = Show(kind, content, out System.Windows.Forms.TabControl tabs, out Subject subject);
            string before = subject.State();

            tabs.SelectedIndex = 1;
            Pump();
            TypeWhereTheFocusIs(host);

            Assert.Equal(before, subject.State());
        });
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void KeysAfterSwitchingWithAClick_DoNotReachTheControl(Kind kind, Content content)
    {
        StaThread.Run(() =>
        {
            using Form host = Show(kind, content, out System.Windows.Forms.TabControl tabs, out Subject subject);
            string before = subject.State();

            Rectangle tab = tabs.GetTabRect(1);
            System.IntPtr at = (System.IntPtr)((tab.Left + 10) | ((tab.Top + 8) << 16));
            SendMessage(tabs.Handle, 0x201, (System.IntPtr)1, at);
            SendMessage(tabs.Handle, 0x202, System.IntPtr.Zero, at);
            Pump();
            TypeWhereTheFocusIs(host);

            Assert.Equal(1, tabs.SelectedIndex);
            Assert.Equal(before, subject.State());
        });
    }

    private static void ClickTab(System.Windows.Forms.TabControl tabs, int index)
    {
        Rectangle tab = tabs.GetTabRect(index);
        System.IntPtr at = (System.IntPtr)((tab.Left + 10) | ((tab.Top + 8) << 16));
        SendMessage(tabs.Handle, 0x201, (System.IntPtr)1, at);
        SendMessage(tabs.Handle, 0x202, System.IntPtr.Zero, at);
        Pump();
    }

    // Going away and coming back with clicks on the tabs: the click puts
    // the focus on the tab control each time, so the keys still don't
    // reach the control on the page.
    [Theory]
    [MemberData(nameof(Combinations))]
    public void KeysAfterClickingAwayAndBack_DoNotReachTheControl(Kind kind, Content content)
    {
        StaThread.Run(() =>
        {
            using Form host = Show(kind, content, out System.Windows.Forms.TabControl tabs, out Subject subject);
            string before = subject.State();

            ClickTab(tabs, 1);
            ClickTab(tabs, 0);
            TypeWhereTheFocusIs(host);

            Assert.Equal(0, tabs.SelectedIndex);
            Assert.Equal(before, subject.State());
        });
    }

    // Switching back in code is different, and the standard control does
    // the same: the page is shown again and WinForms hands the focus back
    // to the control that had it. Locked down so the themed controls can't
    // drift from that.
    [Theory]
    [MemberData(nameof(Combinations))]
    public void ComingBackInCode_GivesTheControlItsFocusBack_LikeTheStandardControl(Kind kind, Content content)
    {
        StaThread.Run(() =>
        {
            using Form host = Show(kind, content, out System.Windows.Forms.TabControl tabs, out Subject subject);

            tabs.SelectedIndex = 1;
            Pump();
            tabs.SelectedIndex = 0;
            Pump();

            Assert.True(subject.Control.Focused);
        });
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void ACellBeingEdited_IsEndedByTheSwitch(Kind kind, Content content)
    {
        if (content != Content.DataGridView)
            return;

        StaThread.Run(() =>
        {
            using Form host = Show(kind, content, out System.Windows.Forms.TabControl tabs, out Subject subject);
            WfuiDataGridView grid = (WfuiDataGridView)subject.Control;
            grid.CurrentCell = grid.Rows[0].Cells[0];
            grid.BeginEdit(false);
            Assert.True(grid.IsCurrentCellInEditMode);

            tabs.SelectedIndex = 1;
            Pump();

            Assert.False(grid.IsCurrentCellInEditMode);
        });
    }
}
