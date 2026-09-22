using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// The actual drag-to-reorder math (GetInsertPosition, ReorderDraggedItem) -
/// unlike MoveItem (ListBoxReorderTests), these are what the real
/// drag-handle gesture itself drives via OnDragOver/OnDragDrop. The
/// gesture's own threshold/DoDragDrop tracking needs a real OLE drag and
/// isn't practical to drive headlessly (same reasoning as
/// DataGridView's/ListView's own hand-rolled column drags - see their own
/// reorder test remarks), but the target-position math underneath it is
/// pure logic, invoked directly here via reflection with a forced handle
/// (GetInsertPosition needs real item layout - IndexFromPoint/
/// GetItemRectangle only work once the handle exists).
/// </summary>
public class ListBoxDragReorderTests
{
    private const int ItemHeight = 30;

    [Fact]
    public void GetInsertPosition_EmptyList_ReturnsZeroWithMoveEffect()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight);

        object?[] args = { new Point(5, 5), null };
        int result = listBox.InvokePrivate<int>("GetInsertPosition", args);

        Assert.Equal(0, result);
        Assert.Equal(DragDropEffects.Move, (DragDropEffects)args[1]!);
    }

    [Fact]
    public void GetInsertPosition_TopHalfOfItem_ReturnsThatItemsIndex()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight, "A", "B", "C");

        // Item 1 spans y=[30,60) - its top half is y in [30,45).
        object?[] args = { new Point(5, 35), null };
        int result = listBox.InvokePrivate<int>("GetInsertPosition", args);

        Assert.Equal(1, result);
        Assert.Equal(DragDropEffects.Move, (DragDropEffects)args[1]!);
    }

    [Fact]
    public void GetInsertPosition_BottomHalfOfItem_ReturnsNextIndex()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight, "A", "B", "C");

        // Item 1 spans y=[30,60) - its bottom half is y in [45,60).
        object?[] args = { new Point(5, 50), null };
        int result = listBox.InvokePrivate<int>("GetInsertPosition", args);

        Assert.Equal(2, result);
    }

    [Fact]
    public void GetInsertPosition_BelowLastItem_ReturnsItemCount()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight, "A", "B", "C");

        object?[] args = { new Point(5, (ItemHeight * 3) + 50), null };
        int result = listBox.InvokePrivate<int>("GetInsertPosition", args);

        Assert.Equal(3, result);
        Assert.Equal(DragDropEffects.Move, (DragDropEffects)args[1]!);
    }

    [Fact]
    public void ReorderDraggedItem_MovesItemToInsertPosition()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight, "A", "B", "C");
        listBox.SetPrivateField("_dragIndex", 0);
        listBox.SetPrivateField("_dragInsertPosition", 2);

        listBox.InvokePrivate("ReorderDraggedItem");

        Assert.Equal("B", listBox.Items[0]);
        Assert.Equal("A", listBox.Items[1]);
        Assert.Equal("C", listBox.Items[2]);
    }

    [Fact]
    public void ReorderDraggedItem_SetsSelectedIndexToNewPosition()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight, "A", "B", "C");
        listBox.SetPrivateField("_dragIndex", 0);
        listBox.SetPrivateField("_dragInsertPosition", 2);

        listBox.InvokePrivate("ReorderDraggedItem");

        Assert.Equal(1, listBox.SelectedIndex);
    }

    [Fact]
    public void ReorderDraggedItem_RaisesItemsReordered()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight, "A", "B", "C");
        listBox.SetPrivateField("_dragIndex", 0);
        listBox.SetPrivateField("_dragInsertPosition", 2);
        bool raised = false;
        listBox.ItemsReordered += (sender, e) => raised = true;

        listBox.InvokePrivate("ReorderDraggedItem");

        Assert.True(raised);
    }

    [Fact]
    public void ReorderDraggedItem_WhenTargetIsSamePosition_IsANoOp()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBoxWithHandle(ItemHeight, "A", "B", "C");
        // Insert position 0, dragging item 0 - already exactly there.
        listBox.SetPrivateField("_dragIndex", 0);
        listBox.SetPrivateField("_dragInsertPosition", 0);
        bool raised = false;
        listBox.ItemsReordered += (sender, e) => raised = true;

        listBox.InvokePrivate("ReorderDraggedItem");

        Assert.Equal("A", listBox.Items[0]);
        Assert.Equal("B", listBox.Items[1]);
        Assert.Equal("C", listBox.Items[2]);
        Assert.False(raised);
    }
}
