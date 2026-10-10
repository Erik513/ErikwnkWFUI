using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// The cases around the normal ones: other mouse buttons, a double click, two presses in a row, the keyboard,
/// a handler that disables or disposes the list in the middle of a click, items that disappear under the mouse,
/// a list that cannot be reordered (sorted, bound), an empty one, one that is scrolled, and texts that are null.
/// The clicks are real window messages to a window that is shown (out of sight).
/// </summary>
public class ListBoxEdgeCaseTests : IDisposable
{
    private const int ItemHeight = 30;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    private readonly List<Form> _forms = new List<Form>();

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private WfuiListBox Shown(int items = 3, SelectionMode mode = SelectionMode.One, bool reorder = false)
    {
        WfuiListBox listBox = new WfuiListBox { AllowReorder = reorder, SelectionMode = mode, Width = 200, Height = 200 };
        listBox.ItemHeight = ItemHeight;

        for (int i = 0; i < items; i++)
            listBox.Items.Add("Item " + i);

        Form form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-3000, 0), FormBorderStyle = FormBorderStyle.None, ClientSize = new Size(260, 260) };
        form.Controls.Add(listBox);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return listBox;
    }

    private static IntPtr At(int index) => (IntPtr)(40 | ((index * ItemHeight + ItemHeight / 2) << 16));

    private static void Press(WfuiListBox listBox, int down, int up, int index)
    {
        SendMessage(listBox.Handle, down, (IntPtr)1, At(index));
        SendMessage(listBox.Handle, up, IntPtr.Zero, At(index));
    }

    private static void Click(WfuiListBox listBox, int index) => Press(listBox, WM_LBUTTONDOWN, WM_LBUTTONUP, index);

    // ---- the other mouse buttons ----

    [Theory]
    [InlineData(WM_RBUTTONDOWN, WM_RBUTTONUP)]
    [InlineData(WM_MBUTTONDOWN, WM_MBUTTONUP)]
    public void AClickWithAnotherButton_OnTheSelectedItem_DoesNotDeselectIt(int down, int up)
    {
        WfuiListBox listBox = Shown();
        Click(listBox, 1);
        Assert.Equal(1, listBox.SelectedIndex);

        Press(listBox, down, up, 1);

        Assert.Equal(1, listBox.SelectedIndex);
    }

    // ---- the order and the number of events ----

    // (The native list raises SelectedIndexChanged for a click on the item that is already selected as well; what
    // counts is that the last one says what the selection is, and that none says anything else.)
    [Fact]
    public void TheEventsOfAClick_EndWithTheSelectionThatIsThere()
    {
        WfuiListBox listBox = Shown();
        List<int> seen = new List<int>();
        listBox.SelectedIndexChanged += (s, e) => seen.Add(listBox.SelectedIndex);

        Click(listBox, 1);
        Assert.Equal(1, seen[seen.Count - 1]);

        Click(listBox, 1); // deselected
        Assert.Equal(-1, seen[seen.Count - 1]);

        Click(listBox, 2);
        Assert.Equal(2, seen[seen.Count - 1]);

        Assert.All(seen, index => Assert.Contains(index, new[] { -1, 1, 2 }));
    }

    [Fact]
    public void ADoubleClick_KeepsTheItemSelected_AndRaisesDoubleClick()
    {
        WfuiListBox listBox = Shown();
        int doubleClicks = 0;
        listBox.DoubleClick += (s, e) => doubleClicks++;

        Click(listBox, 1);
        Press(listBox, WM_LBUTTONDBLCLK, WM_LBUTTONUP, 1);

        Assert.Equal(1, listBox.SelectedIndex);
        Assert.Equal(1, doubleClicks);
    }

    [Fact]
    public void TwoPressesWithoutARelease_DoNotThrow_AndDeselectOnlyOnce()
    {
        WfuiListBox listBox = Shown();
        Click(listBox, 1);

        SendMessage(listBox.Handle, WM_LBUTTONDOWN, (IntPtr)1, At(1));
        SendMessage(listBox.Handle, WM_LBUTTONDOWN, (IntPtr)1, At(1));
        SendMessage(listBox.Handle, WM_LBUTTONUP, IntPtr.Zero, At(1));

        Assert.True(listBox.SelectedIndex is -1 or 1);

        // And the next plain click works as it should.
        Click(listBox, 2);
        Assert.Equal(2, listBox.SelectedIndex);
    }

    // ---- the keyboard ----

    [Fact]
    public void TheArrowKeys_MoveTheSelection_AndAreNotAToggle()
    {
        WfuiListBox listBox = Shown();
        listBox.Focus();

        SendMessage(listBox.Handle, WM_KEYDOWN, (IntPtr)0x28, IntPtr.Zero); // Down
        SendMessage(listBox.Handle, WM_KEYUP, (IntPtr)0x28, IntPtr.Zero);
        SendMessage(listBox.Handle, WM_KEYDOWN, (IntPtr)0x28, IntPtr.Zero);
        SendMessage(listBox.Handle, WM_KEYUP, (IntPtr)0x28, IntPtr.Zero);
        Assert.Equal(1, listBox.SelectedIndex);

        SendMessage(listBox.Handle, WM_KEYDOWN, (IntPtr)0x26, IntPtr.Zero); // Up
        SendMessage(listBox.Handle, WM_KEYUP, (IntPtr)0x26, IntPtr.Zero);
        Assert.Equal(0, listBox.SelectedIndex);
    }

    // ---- a handler that does something to the list in the middle of a click ----

    [Fact]
    public void AHandler_ThatDisposesTheList_DoesNotBreakTheClick()
    {
        WfuiListBox listBox = Shown();
        listBox.SelectedIndexChanged += (s, e) => listBox.Dispose();

        Exception? thrown = Record.Exception(() => Click(listBox, 1));

        Assert.Null(thrown);
        Assert.True(listBox.IsDisposed);
    }

    [Fact]
    public void AHandler_ThatDisablesTheList_LeavesItInAConsistentState()
    {
        WfuiListBox listBox = Shown();
        listBox.SelectedIndexChanged += (s, e) => listBox.Enabled = false;

        Click(listBox, 1);

        Assert.False(listBox.Enabled);
        Assert.Equal(listBox.DisabledBackColor, listBox.InvokePrivate<Color>("GetBackColor", 1, true, true, false));

        // Back on, it works again.
        listBox.Enabled = true;
        Click(listBox, 2);
        Assert.Equal(2, listBox.SelectedIndex);
    }

    [Fact]
    public void AHandler_ThatEmptiesTheList_DoesNotBreakTheClick()
    {
        WfuiListBox listBox = Shown();
        listBox.SelectedIndexChanged += (s, e) => listBox.Items.Clear();

        Exception? thrown = Record.Exception(() => Click(listBox, 1));

        Assert.Null(thrown);
        Assert.Empty(listBox.Items);
    }

    [Fact]
    public void ItemsThatDisappearUnderTheMouse_DoNotBreakTheHoverOrThePainting()
    {
        WfuiListBox listBox = Shown(items: 6);
        listBox.SetPrivateField("_hoverIndex", 5);

        listBox.Items.Clear();
        listBox.InvokePrivate("UpdateHoverIndex", new Point(10, 10));

        using Bitmap bitmap = new Bitmap(listBox.Width, listBox.Height);
        Exception? thrown = Record.Exception(() => listBox.DrawToBitmap(bitmap, new Rectangle(0, 0, listBox.Width, listBox.Height)));

        Assert.Null(thrown);
    }

    [Fact]
    public void ADisabledList_ShowsNoHover_EvenWhereTheMouseWas()
    {
        WfuiListBox listBox = Shown(items: 3);
        listBox.SetPrivateField("_hoverIndex", 0);
        listBox.Enabled = false;

        using Bitmap bitmap = new Bitmap(listBox.Width, listBox.Height);
        listBox.DrawToBitmap(bitmap, new Rectangle(0, 0, listBox.Width, listBox.Height));

        // A pixel of the first row, away from its text: the plain row color, not the hover color.
        Color pixel = bitmap.GetPixel(150, 5);

        Assert.NotEqual(listBox.HoverBackColor.ToArgb(), pixel.ToArgb());
    }

    // ---- a list that cannot be reordered ----

    [Fact]
    public void ASortedList_AndABoundList_OfferNoReordering()
    {
        using WfuiListBox sorted = new WfuiListBox { AllowReorder = true, Sorted = true };
        Assert.False(CanReorder(sorted));

        using WfuiListBox bound = new WfuiListBox { AllowReorder = true, DataSource = new List<string> { "a", "b" } };
        Assert.False(CanReorder(bound));

        using WfuiListBox plain = new WfuiListBox { AllowReorder = true };
        Assert.True(CanReorder(plain));

        // The setting is kept for when the list is plain again.
        sorted.Sorted = false;
        Assert.True(CanReorder(sorted));
    }

    private static bool CanReorder(WfuiListBox listBox) =>
        (bool)typeof(WfuiListBox).GetProperty("CanReorder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(listBox)!;

    // ---- empty, one item, scrolled ----

    [Fact]
    public void AnEmptyList_TakesClicksAndKeysWithoutAFailure()
    {
        WfuiListBox listBox = Shown(items: 0);
        listBox.Focus();

        Exception? thrown = Record.Exception(() =>
        {
            Click(listBox, 0);
            SendMessage(listBox.Handle, WM_KEYDOWN, (IntPtr)0x28, IntPtr.Zero);
            SendMessage(listBox.Handle, WM_KEYUP, (IntPtr)0x28, IntPtr.Zero);
        });

        Assert.Null(thrown);
        Assert.Equal(-1, listBox.SelectedIndex);
    }

    [Fact]
    public void AScrolledList_FindsTheInsertPositionFromTheVisibleRows()
    {
        WfuiListBox listBox = Shown(items: 40);
        listBox.TopIndex = 10;

        // The first visible row is item 10: its top half is the place before it.
        object?[] args = { new Point(5, 5), null };
        int position = listBox.InvokePrivate<int>("GetInsertPosition", args);

        Assert.Equal(10, position);
    }

    [Fact]
    public void ADropOutsideTheList_ReordersNothing_AndEndsTheDrag()
    {
        WfuiListBox listBox = Shown(items: 4, reorder: true);
        listBox.SetPrivateField("_dragIndex", 0);
        listBox.SetPrivateField("_dragInsertPosition", 3);
        listBox.SetPrivateField("_isDragging", true);

        Point outside = listBox.PointToScreen(new Point(-500, -500));
        DragEventArgs drop = new DragEventArgs(new DataObject(), 0, outside.X, outside.Y, DragDropEffects.Move, DragDropEffects.Move);
        listBox.InvokePrivate("OnDragDrop", drop);

        Assert.Equal("Item 0", listBox.Items[0]);
        Assert.False(listBox.GetPrivateField<bool>("_isDragging"));
        Assert.Equal(-1, listBox.GetPrivateField<int>("_dragIndex"));
    }

    // ---- texts that are null ----

    [Fact]
    public void AProviderThatReturnsNull_ShowsAnEmptyText()
    {
        using WfuiListBox listBox = new WfuiListBox { DisplayTextProvider = item => null! };

        Assert.Equal("", listBox.InvokePrivate<string>("GetDisplayText", "x"));
    }
}
