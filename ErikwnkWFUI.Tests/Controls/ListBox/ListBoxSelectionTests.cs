using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// Selection behavior, including the toggle-deselect regression this
/// control's OnMouseDown/OnMouseUp/WndProc split exists for - see
/// CapturePendingToggleDeselect's own remarks on why the capture had to
/// move out of OnMouseDown and into a raw WM_LBUTTONDOWN intercept: a
/// native ListBox commits its own click-to-select synchronously as part of
/// that same native message, before OnMouseDown/MouseDown ever fires, so
/// checking SelectedIndex from OnMouseDown always saw the NEW selection
/// already applied - every plain click toggled straight back off on
/// release, not just a second click on an already-selected item. These
/// tests call CapturePendingToggleDeselect/OnMouseUp directly (via
/// reflection) rather than dispatching a real WM_LBUTTONDOWN - not a
/// substitute for the live re-test that actually caught the bug, but they
/// do lock in that, given an accurate SelectedIndex snapshot, the
/// arm/clear logic itself stays correct.
/// </summary>
public class ListBoxSelectionTests
{
    private const int ItemHeight = 30;

    [Fact]
    public void ClearSelected_WhenNothingSelected_IsANoOp()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox("A", "B");

        listBox.ClearSelected();

        Assert.Equal(-1, listBox.SelectedIndex);
    }

    [Fact]
    public void ClearSelected_ClearsSelection()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox("A", "B");
        listBox.SelectedIndex = 0;

        listBox.ClearSelected();

        Assert.Equal(-1, listBox.SelectedIndex);
    }

    [Fact]
    public void CapturePendingToggleDeselect_ArmsToggle_WhenClickedItemAlreadySelected()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight, "A", "B", "C");
        listBox.SelectedIndex = 0;

        listBox.InvokePrivate("CapturePendingToggleDeselect", PointToLParam(5, 5));

        Assert.Equal(0, listBox.GetPrivateField<int>("_pendingToggleDeselectIndex"));
    }

    [Fact]
    public void CapturePendingToggleDeselect_DoesNotArmToggle_WhenClickedItemNotSelected()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight, "A", "B", "C");
        listBox.SelectedIndex = 1;

        // Clicking item 0 while item 1 is the one actually selected - the
        // exact case that regressed: a fresh click on a DIFFERENT,
        // not-yet-selected item must never arm the toggle, or it would
        // deselect itself back off the instant it's released.
        listBox.InvokePrivate("CapturePendingToggleDeselect", PointToLParam(5, 5));

        Assert.Equal(-1, listBox.GetPrivateField<int>("_pendingToggleDeselectIndex"));
    }

    [Fact]
    public void CapturePendingToggleDeselect_DoesNotArmToggle_WhenNothingSelected()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight, "A", "B", "C");

        listBox.InvokePrivate("CapturePendingToggleDeselect", PointToLParam(5, 5));

        Assert.Equal(-1, listBox.GetPrivateField<int>("_pendingToggleDeselectIndex"));
    }

    [Fact]
    public void OnMouseUp_ClearsSelection_WhenReleasedOnStillSelectedPendingItem()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight, "A", "B", "C");
        listBox.SelectedIndex = 0;
        listBox.SetPrivateField("_pendingToggleDeselectIndex", 0);

        listBox.InvokePrivate("OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));

        Assert.Equal(-1, listBox.SelectedIndex);
    }

    [Fact]
    public void OnMouseUp_KeepsSelection_WhenReleasedOnADifferentItemThanPending()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight, "A", "B", "C");
        listBox.SelectedIndex = 0;
        listBox.SetPrivateField("_pendingToggleDeselectIndex", 0);

        // The mouse moved to item 2 before releasing - not a click on item
        // 0 anymore (e.g. it turned into a drag-handle drag elsewhere), so
        // this must not deselect anything.
        listBox.InvokePrivate("OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, ItemHeight * 2 + 5, 0));

        Assert.Equal(0, listBox.SelectedIndex);
    }

    [Fact]
    public void OnMouseUp_KeepsSelection_WhenNoToggleWasPending()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight, "A", "B", "C");
        listBox.SelectedIndex = 0;

        listBox.InvokePrivate("OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));

        Assert.Equal(0, listBox.SelectedIndex);
    }

    private static IntPtr PointToLParam(int x, int y)
    {
        return (IntPtr)((y << 16) | (x & 0xFFFF));
    }

    // ---- the real mouse messages: select, click again to deselect (end to end) ----

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    private static void Click(WfuiListBox listBox, int index)
    {
        // The middle of the row, left of the (absent) drag handle.
        int y = index * ItemHeight + ItemHeight / 2;
        IntPtr position = (IntPtr)(40 | (y << 16));

        SendMessage(listBox.Handle, 0x0201, (IntPtr)1, position); // WM_LBUTTONDOWN
        SendMessage(listBox.Handle, 0x0202, IntPtr.Zero, position); // WM_LBUTTONUP
    }

    private (WfuiListBox ListBox, Form Form) Shown(SelectionMode mode)
    {
        WfuiListBox listBox = new WfuiListBox { AllowReorder = false, SelectionMode = mode, Width = 200, Height = 200 };
        listBox.ItemHeight = ItemHeight;
        listBox.Items.AddRange(new object[] { "A", "B", "C" });
        Form form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-3000, 0), FormBorderStyle = FormBorderStyle.None };
        form.Controls.Add(listBox);
        form.Show();
        Application.DoEvents();

        return (listBox, form);
    }

    [Fact]
    public void AClickSelects_AClickOnTheSelectedItemDeselectsIt_AClickOnAnotherMovesTheSelection()
    {
        (WfuiListBox listBox, Form form) = Shown(SelectionMode.One);
        using (form)
        {
            Click(listBox, 1);
            Assert.Equal(1, listBox.SelectedIndex);

            Click(listBox, 1);
            Assert.Equal(-1, listBox.SelectedIndex);

            Click(listBox, 1);
            Assert.Equal(1, listBox.SelectedIndex);

            Click(listBox, 2);
            Assert.Equal(2, listBox.SelectedIndex);

            // The room below the items clears it as well.
            SendMessage(listBox.Handle, 0x0201, (IntPtr)1, (IntPtr)(40 | (190 << 16)));
            SendMessage(listBox.Handle, 0x0202, IntPtr.Zero, (IntPtr)(40 | (190 << 16)));
            Assert.Equal(-1, listBox.SelectedIndex);
        }
    }

    [Fact]
    public void InAMultipleSelection_AClickDoesNotClearTheOthers()
    {
        (WfuiListBox listBox, Form form) = Shown(SelectionMode.MultiSimple);
        using (form)
        {
            Click(listBox, 0);
            Click(listBox, 2);
            Assert.Equal(2, listBox.SelectedIndices.Count);

            // MultiSimple: a click on a selected item takes only that one out.
            Click(listBox, 2);
            Assert.Single(listBox.SelectedIndices);
            Assert.Equal(0, listBox.SelectedIndex);
        }
    }
}
