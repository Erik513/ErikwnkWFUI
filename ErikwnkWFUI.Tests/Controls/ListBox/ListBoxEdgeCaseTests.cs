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
        Assert.NotEqual(listBox.SelectedBackColor, listBox.InvokePrivate<Color>("GetBackColor", 1, true, true, false));

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

    // ---- the final check: what the code does around the mouse and the drag ----

    private static int HoverOf(WfuiListBox listBox) => listBox.GetPrivateField<int>("_hoverIndex");

    [Fact]
    public void APress_OnTheHandle_AndOnTheRoomBelowTheItems_RaiseMouseDownToo()
    {
        WfuiListBox listBox = Shown(items: 3, reorder: true);
        int downs = 0;
        listBox.MouseDown += (s, e) => downs++;

        // The handle sits at the right end of a row.
        int y = ItemHeight / 2;
        SendMessage(listBox.Handle, WM_LBUTTONDOWN, (IntPtr)1, (IntPtr)((listBox.ClientSize.Width - 12) | (y << 16)));
        SendMessage(listBox.Handle, WM_LBUTTONUP, IntPtr.Zero, (IntPtr)((listBox.ClientSize.Width - 12) | (y << 16)));
        Assert.Equal(1, downs);
        Assert.Equal(0, listBox.SelectedIndex);

        // And below the three rows.
        SendMessage(listBox.Handle, WM_LBUTTONDOWN, (IntPtr)1, (IntPtr)(40 | (180 << 16)));
        SendMessage(listBox.Handle, WM_LBUTTONUP, IntPtr.Zero, (IntPtr)(40 | (180 << 16)));
        Assert.Equal(2, downs);
        Assert.Equal(-1, listBox.SelectedIndex);
    }

    [Fact]
    public void ARightClickBelowTheItems_LeavesTheSelectionAlone()
    {
        WfuiListBox listBox = Shown();
        Click(listBox, 1);

        SendMessage(listBox.Handle, WM_RBUTTONDOWN, (IntPtr)2, (IntPtr)(40 | (180 << 16)));
        SendMessage(listBox.Handle, WM_RBUTTONUP, IntPtr.Zero, (IntPtr)(40 | (180 << 16)));

        Assert.Equal(1, listBox.SelectedIndex);
    }

    [Fact]
    public void AForeignDrag_IsLeftToTheHandlersOfTheApplication_ButTheOwnOneIsTheReordering()
    {
        WfuiListBox listBox = Shown(items: 4, reorder: true);
        int handled = 0;
        DragEventHandler onDragOver = (s, e) => { handled++; e.Effect = DragDropEffects.Copy; };
        listBox.DragOver += onDragOver;
        Point inside = listBox.PointToScreen(new Point(40, 40));

        // Files from outside: not a reordering - no drop line, the application's handler decides.
        DragEventArgs foreign = new DragEventArgs(new DataObject(), 0, inside.X, inside.Y, DragDropEffects.All, DragDropEffects.None);
        listBox.InvokePrivate("OnDragOver", foreign);

        Assert.Equal(1, handled);
        Assert.Equal(DragDropEffects.Copy, foreign.Effect);
        Assert.Equal(-1, listBox.GetPrivateField<int>("_dragInsertPosition"));

        // The list's own drag: the reordering (the drop line and the move effect; a handler of the application would
        // still have the last word, see above).
        listBox.DragOver -= onDragOver;
        listBox.SetPrivateField("_isDragging", true);
        listBox.SetPrivateField("_dragIndex", 0);
        DragEventArgs own = new DragEventArgs(new DataObject(), 0, inside.X, inside.Y, DragDropEffects.Move, DragDropEffects.None);
        listBox.InvokePrivate("OnDragOver", own);

        Assert.Equal(DragDropEffects.Move, own.Effect);
        Assert.NotEqual(-1, listBox.GetPrivateField<int>("_dragInsertPosition"));
    }

    [Fact]
    public void ItemsThatWentWhileOneWasDragged_DoNotBreakTheDrop()
    {
        WfuiListBox listBox = Shown(items: 4, reorder: true);
        listBox.SetPrivateField("_dragIndex", 3);
        listBox.SetPrivateField("_dragInsertPosition", 0);
        listBox.Items.Clear();

        Exception? thrown = Record.Exception(() => listBox.InvokePrivate("ReorderDraggedItem"));

        Assert.Null(thrown);
        Assert.Empty(listBox.Items);
    }

    [Fact]
    public void TheHandle_StandsWhereItDoes_WithAndWithoutAScrollBar()
    {
        WfuiListBox few = Shown(items: 3, reorder: true);
        WfuiListBox many = Shown(items: 40, reorder: true);

        Rectangle fewRow = few.GetItemRectangle(0);
        Rectangle manyRow = many.GetItemRectangle(0);
        Rectangle fewHandle = few.InvokePrivate<Rectangle>("GetDragHandleRectangle", fewRow);
        Rectangle manyHandle = many.InvokePrivate<Rectangle>("GetDragHandleRectangle", manyRow);

        // The row of a list with a scroll bar is narrower by the bar already: the handle keeps its place in the row.
        Assert.True(manyRow.Width < fewRow.Width, "the list with 40 items has no scroll bar");
        Assert.Equal(fewRow.Right - fewHandle.Right, manyRow.Right - manyHandle.Right);
    }

    [Fact]
    public void ARowThatMovesUnderTheMouse_TakesTheHoverWithIt_AndASwitchedOffListLosesIt()
    {
        WfuiListBox listBox = Shown(items: 40);
        listBox.SetPrivateField("_hoverIndex", 2);

        SendMessage(listBox.Handle, 0x020A, (IntPtr)unchecked((int)(-120u << 16)), IntPtr.Zero); // WM_MOUSEWHEEL
        Application.DoEvents();

        Point mouse = listBox.PointToClient(Cursor.Position);
        int expected = listBox.ClientRectangle.Contains(mouse) ? listBox.IndexFromPoint(mouse) : -1;
        Assert.Equal(expected, HoverOf(listBox));

        listBox.SetPrivateField("_hoverIndex", 1);
        listBox.Enabled = false;
        listBox.Enabled = true;
        Assert.Equal(-1, HoverOf(listBox));
    }

    // ---- the second look: moving, no selection, a drag that is given up ----

    [Fact]
    public void MoveItem_ToTheSamePlace_ChangesAndRaisesNothing()
    {
        WfuiListBox listBox = Shown(items: 3);
        int raised = 0;
        listBox.ItemsReordered += (s, e) => raised++;

        listBox.MoveItem(1, 1);

        Assert.Equal(0, raised);
        Assert.Equal(new object[] { "Item 0", "Item 1", "Item 2" }, listBox.Items.Cast<object>().ToArray());
    }

    [Fact]
    public void MoveItem_InASortedList_OrABoundOne_IsAnErrorThatTouchesNothing()
    {
        WfuiListBox sorted = Shown(items: 3);
        sorted.Sorted = true;
        Assert.Throws<InvalidOperationException>(() => sorted.MoveItem(0, 2));
        Assert.Equal(3, sorted.Items.Count);

        using WfuiListBox bound = new WfuiListBox { DataSource = new List<string> { "a", "b", "c" } };
        Assert.Throws<InvalidOperationException>(() => bound.MoveItem(0, 2));
    }

    [Fact]
    public void AListWithoutASelection_CanBeReordered_WithoutAFailure()
    {
        WfuiListBox listBox = Shown(items: 3, mode: SelectionMode.None, reorder: true);

        Exception? thrown = Record.Exception(() =>
        {
            listBox.MoveItem(0, 2);

            // A press on the handle of a row (the drag is prepared, nothing is selected).
            int y = ItemHeight / 2;
            IntPtr onTheHandle = (IntPtr)((listBox.ClientSize.Width - 12) | (y << 16));
            SendMessage(listBox.Handle, WM_LBUTTONDOWN, (IntPtr)1, onTheHandle);
            SendMessage(listBox.Handle, WM_LBUTTONUP, IntPtr.Zero, onTheHandle);
        });

        Assert.Null(thrown);
        Assert.Equal("Item 0", listBox.Items[2]);
        Assert.Equal(-1, listBox.SelectedIndex);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ADragThatIsGivenUp_OrFails_EndsTheDragState_WithoutAMouseUp(bool fails)
    {
        WfuiListBox listBox = Shown(items: 3, reorder: true);
        listBox.SetPrivateField("_dragIndex", 1);
        listBox.SetPrivateField("_dragStartPoint", new Point(10, 10));
        listBox.DragDropStarter = index =>
        {
            Assert.True(listBox.GetPrivateField<bool>("_isDragging"), "the drag has started");

            if (fails)
                throw new InvalidOperationException("the system drag failed");
        };

        // A move far enough from the press to start the drag.
        MouseEventArgs move = new MouseEventArgs(MouseButtons.Left, 0, 100, 100, 0);
        Exception? thrown = Record.Exception(() => listBox.InvokePrivate("TryStartDrag", move));

        Assert.Equal(fails, thrown is not null);
        Assert.False(listBox.GetPrivateField<bool>("_isDragging"));
        Assert.Equal(-1, listBox.GetPrivateField<int>("_dragIndex"));

        // And the selected item is painted as selected again, the hover works again.
        Assert.NotEqual(
            listBox.InvokePrivate<Color>("GetBackColor", 1, false, false, false),
            listBox.InvokePrivate<Color>("GetBackColor", 1, true, false, false));
    }

    // ---- a click on a row that is only partly visible at the bottom ----

    [Theory]
    [InlineData(7)]
    [InlineData(20)]
    public void AClickOnTheRowThatIsPartlyVisibleAtTheBottom_SelectsIt_WhateverTheListDoesToShowIt(int items)
    {
        WfuiListBox listBox = Shown(items: items);
        // 200 pixels high, 30 per row: the seventh row (index 6) shows its upper part only.
        int partlyVisible = 6;
        int y = 6 * ItemHeight + 5;

        Assert.Equal(partlyVisible, listBox.IndexFromPoint(new Point(40, y)));

        IntPtr at = (IntPtr)(40 | (y << 16));
        SendMessage(listBox.Handle, WM_LBUTTONDOWN, (IntPtr)1, at);
        SendMessage(listBox.Handle, WM_LBUTTONUP, IntPtr.Zero, at);

        Assert.Equal(partlyVisible, listBox.SelectedIndex);
    }
}
