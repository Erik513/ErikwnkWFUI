using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ComboBox;

/// <summary>
/// What the combo box does: selecting and its events, opening and closing the list by mouse and
/// keyboard, a disabled box, a handler that interferes, binding, <c>ReplaceItems</c>, text and
/// size edge cases, and changes made to a box that is already shown. The looks are in
/// ComboBoxTests and ComboBoxClosedBoxTests. [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ComboBoxBehaviorTests : IDisposable
{
    private readonly List<Form> _hosts = new List<Form>();

    public void Dispose()
    {
        foreach (Form host in _hosts)
            host.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    private ErikwnkWFUI.Controls.ComboBox Show(params object[] items)
    {
        ErikwnkWFUI.Controls.ComboBox box = (ErikwnkWFUI.Controls.ComboBox)UIStyles.ComboBoxes.CreateStandard();
        box.Items.AddRange(items.Length == 0 ? new object[] { "One", "Two", "Three" } : items);
        box.Width = 160;
        box.Location = new Point(10, 10);

        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-4000, -4000),
            ShowInTaskbar = false,
            ClientSize = new Size(300, 260),
            BackColor = UIColors.BackgroundMedium
        };

        host.Controls.Add(box);
        host.Show();
        _hosts.Add(host);
        Pump();

        return box;
    }

    private static void Pump()
    {
        for (int i = 0; i < 4; i++)
        {
            Application.DoEvents();
            Thread.Sleep(30);
        }
    }

    private static void Click(Control control)
    {
        IntPtr at = (IntPtr)(20 | (10 << 16));
        SendMessage(control.Handle, 0x201, (IntPtr)1, at);
        SendMessage(control.Handle, 0x202, IntPtr.Zero, at);
        Pump();
    }

    private static void Key(Control control, Keys key)
    {
        SendMessage(control.Handle, 0x100, (IntPtr)(int)key, IntPtr.Zero);
        SendMessage(control.Handle, 0x101, (IntPtr)(int)key, IntPtr.Zero);
        Pump();
    }

    // ---- selecting and the events ----

    [Fact]
    public void ASelection_RaisesSelectedIndexChangedOnce_AndSettingTheSameIndexNone()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show();
        int changes = 0;
        box.SelectedIndexChanged += delegate { changes++; };

        box.SelectedIndex = 1;
        box.SelectedIndex = 1;
        box.SelectedItem = "Two";

        Assert.Equal(1, changes);
        Assert.Equal("Two", box.SelectedItem);
    }

    [Fact]
    public void ASelectionThatIsOutOfRange_IsRefused_AndMinusOneClearsIt()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show();
        box.SelectedIndex = 0;

        Assert.Throws<ArgumentOutOfRangeException>(() => box.SelectedIndex = 7);
        Assert.Equal(0, box.SelectedIndex);

        box.SelectedIndex = -1;
        Assert.Equal(-1, box.SelectedIndex);
        Assert.Null(box.SelectedItem);
    }

    [Fact]
    public void AHandlerThatDisablesTheBox_OrTakesTheChangeBack_LeavesItUsable()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show();
        box.SelectedIndex = 0;
        box.SelectedIndexChanged += delegate
        {
            if (box.SelectedIndex == 2)
                box.SelectedIndex = 0;
        };

        box.SelectedIndex = 2;
        Assert.Equal(0, box.SelectedIndex);

        box.SelectedIndexChanged += delegate { box.Enabled = false; };
        box.SelectedIndex = 1;
        Assert.False(box.Enabled);

        box.Enabled = true;
        using Bitmap picture = new Bitmap(box.Width, box.Height);
        box.DrawToBitmap(picture, new Rectangle(0, 0, box.Width, box.Height));
        Assert.Equal(box.Size, picture.Size);
    }

    // ---- opening and closing the list ----

    [Fact]
    public void AClick_OpensTheList_AndRaisesTheEvents_AndEscapeClosesItAgain()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show();
        List<string> events = new List<string>();
        box.DropDown += delegate { events.Add("DropDown"); };
        box.DropDownClosed += delegate { events.Add("DropDownClosed"); };

        Click(box);
        Assert.True(box.DroppedDown, "the click did not open the list");

        // Open, the border is the strongest one.
        Assert.Equal(
            UIColors.EnsureContrast(UIStyles.Colors.Sets.Gray.Pressed, box.BackColor, 6.0),
            box.BorderColor);

        Key(box, Keys.Escape);
        Assert.False(box.DroppedDown);

        Assert.Equal(new[] { "DropDown", "DropDownClosed" }, events.ToArray());
    }

    [Fact]
    public void ADisabledBox_DoesNotOpen()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show();
        box.Enabled = false;

        Click(box);

        Assert.False(box.DroppedDown);
    }

    [Fact]
    public void TheKeyboard_ChangesTheChoice_OpensWithF4_AndPicksByTyping()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show("Apple", "Banana", "Cherry");
        box.Select();
        Pump();
        box.SelectedIndex = 0;

        Key(box, Keys.Down);
        Assert.Equal(1, box.SelectedIndex);

        Key(box, Keys.Up);
        Assert.Equal(0, box.SelectedIndex);

        // Typing the first letter jumps to the entry.
        SendMessage(box.Handle, 0x102, (IntPtr)'c', IntPtr.Zero);
        Pump();
        Assert.Equal(2, box.SelectedIndex);

        Key(box, Keys.F4);
        Assert.True(box.DroppedDown, "F4 did not open the list");
    }

    [Fact]
    public void TheBoxLosingTheFocus_WhileOpen_ClosesTheList_AndTheLookGoesBack()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show();
        Button other = new Button { Location = new Point(10, 200) };
        _hosts[_hosts.Count - 1].Controls.Add(other);
        box.Select();
        Pump();

        Click(box);
        Assert.True(box.DroppedDown);

        other.Focus();
        Pump();

        Assert.False(box.DroppedDown);
        Assert.Equal(UIColors.GetOutlineColor(box.BackColor), box.BorderColor);
    }

    // ---- binding ----

    private sealed class Entry
    {
        public Entry(string name, int id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; }

        public int Id { get; }
    }

    [Fact]
    public void ABoundBox_ShowsTheDisplayMember_AndGivesTheValueMember()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show();
        box.DataSource = new List<Entry> { new Entry("Alpha", 10), new Entry("Beta", 20) };
        box.DisplayMember = "Name";
        box.ValueMember = "Id";

        box.SelectedIndex = 1;

        Assert.Equal(20, box.SelectedValue);
        Assert.Equal("Beta", box.GetItemText(box.SelectedItem));

        using Bitmap picture = new Bitmap(box.Width, box.Height);
        box.DrawToBitmap(picture, new Rectangle(0, 0, box.Width, box.Height));
        Assert.Equal(box.Size, picture.Size);
    }

    [Fact]
    public void AnItemThatIsEmptyOrNull_DrawsNothingWrong()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show("", "Two");
        box.SelectedIndex = 0;

        Exception? thrown = Record.Exception(() =>
        {
            using Bitmap picture = new Bitmap(box.Width, box.Height);
            box.DrawToBitmap(picture, new Rectangle(0, 0, box.Width, box.Height));
        });

        Assert.Null(thrown);
    }

    // ---- ReplaceItems ----

    [Fact]
    public void ReplaceItems_KeepsTheSelectionByPosition_AndDropsItWhenThePlaceIsGone()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show("a", "b", "c");
        box.SelectedIndex = 2;

        UIStyles.ComboBoxes.ReplaceItems(box, new object[] { "x", "y", "z" });
        Assert.Equal(2, box.SelectedIndex);
        Assert.Equal("z", box.SelectedItem);

        // Fewer entries than the old position: nothing is selected.
        UIStyles.ComboBoxes.ReplaceItems(box, new object[] { "p", "q" });
        Assert.Equal(-1, box.SelectedIndex);
    }

    [Fact]
    public void ReplaceItems_WithoutKeepSelection_ClearsIt_AndNullArgumentsAreIgnored()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show("a", "b", "c");
        box.SelectedIndex = 1;

        UIStyles.ComboBoxes.ReplaceItems(box, new object[] { "x", "y" }, keepSelection: false);
        Assert.Equal(-1, box.SelectedIndex);
        Assert.Equal(2, box.Items.Count);

        UIStyles.ComboBoxes.ReplaceItems(box, null!);
        UIStyles.ComboBoxes.ReplaceItems(null!, new object[] { "q" });
        Assert.Equal(2, box.Items.Count);

        UIStyles.ComboBoxes.ReplaceItems(box, Array.Empty<object>());
        Assert.Empty(box.Items);
        Assert.Equal(-1, box.SelectedIndex);
    }

    [Fact]
    public void ReplaceItems_RaisesSelectedIndexChanged_ForTheClearAndTheRestore()
    {
        // Documented: Items.Clear() and the restore can each raise it once.
        ErikwnkWFUI.Controls.ComboBox box = Show("a", "b", "c");
        box.SelectedIndex = 1;
        List<int> seen = new List<int>();
        box.SelectedIndexChanged += delegate { seen.Add(box.SelectedIndex); };

        UIStyles.ComboBoxes.ReplaceItems(box, new object[] { "x", "y", "z" });

        Assert.Equal(1, box.SelectedIndex);
        Assert.Equal(1, seen[seen.Count - 1]);
        Assert.True(seen.Count <= 2, "more events than the clear and the restore: " + seen.Count);
    }

    // ---- text and size ----

    [Fact]
    public void ALongText_IsCutBeforeTheArrow_NeverDrawnOverIt()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show("A very long entry that cannot possibly fit into the box at all");
        box.SelectedIndex = 0;
        box.Width = 120;

        using Bitmap picture = new Bitmap(box.Width, box.Height);
        box.DrawToBitmap(picture, new Rectangle(0, 0, box.Width, box.Height));

        // The text is cut where the arrow part begins (22 px and the border from the right):
        // the strip in front of the chevron itself has nothing drawn in it.
        for (int x = picture.Width - 23; x < picture.Width - 18; x++)
        {
            for (int y = 3; y < picture.Height - 3; y++)
                Assert.True(picture.GetPixel(x, y) == box.BackColor, "something is drawn at " + x + "," + y);
        }
    }

    [Fact]
    public void ATinyOrWideBox_DrawsWithoutAFuss()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show();
        box.SelectedIndex = 1;

        foreach (int width in new[] { 30, 60, 400 })
        {
            box.Width = width;
            Exception? thrown = Record.Exception(() =>
            {
                using Bitmap picture = new Bitmap(box.Width, box.Height);
                box.DrawToBitmap(picture, new Rectangle(0, 0, box.Width, box.Height));
            });

            Assert.Null(thrown);
        }
    }

    // ---- changes made while it is shown ----

    [Fact]
    public void TheColorsAndTheFont_CanChangeWhileShown_AndTheBoxFollows()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show();
        box.SelectedIndex = 0;

        Color Corner()
        {
            using Bitmap picture = new Bitmap(box.Width, box.Height);
            box.DrawToBitmap(picture, new Rectangle(0, 0, box.Width, box.Height));

            return picture.GetPixel(4, 4);
        }

        Assert.Equal(box.BackColor, Corner());

        box.BackColor = Color.FromArgb(40, 90, 60);
        Assert.Equal(Color.FromArgb(40, 90, 60), Corner());

        using Font larger = new Font(box.Font.FontFamily, 14f);
        Exception? thrown = Record.Exception(() => box.Font = larger);
        Assert.Null(thrown);
    }

    [Fact]
    public void ChangingTheStyleAtRuntime_SwitchesWhoPaintsTheClosedBox()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show();

        Assert.True(box.OwnsClosedPainting);

        box.DropDownStyle = ComboBoxStyle.DropDown;
        Assert.False(box.OwnsClosedPainting);

        box.DropDownStyle = ComboBoxStyle.DropDownList;
        Assert.True(box.OwnsClosedPainting);
        Assert.Equal(UIColors.GetOutlineColor(box.BackColor), box.BorderColor);
    }

    [Fact]
    public void ADisposedBox_CanBeDisposedAgain_AndTheColorsCanBeSetBeforeTheHandleExists()
    {
        ErikwnkWFUI.Controls.ComboBox box = (ErikwnkWFUI.Controls.ComboBox)UIStyles.ComboBoxes.CreateStandard();
        box.Colors = UIStyles.Colors.Sets.Green;
        box.Colors = null;

        box.Dispose();
        Exception? thrown = Record.Exception(() => box.Dispose());

        Assert.Null(thrown);
    }
}
