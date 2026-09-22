using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// MoveItem - the programmatic reorder path (no user drag interaction),
/// used directly by consumers and indirectly by the drag-to-reorder gesture
/// itself. The drag gesture's own threshold/DoDragDrop tracking needs a
/// real OLE drag and isn't practical to drive headlessly (same as
/// DataGridView's/ListView's own hand-rolled column drags - see their own
/// reorder test remarks), so this only covers the actual move math and its
/// side effects.
/// </summary>
public class ListBoxReorderTests
{
    [Fact]
    public void MoveItem_MovesItemToNewIndex()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox("A", "B", "C");

        listBox.MoveItem(0, 2);

        Assert.Equal("B", listBox.Items[0]);
        Assert.Equal("C", listBox.Items[1]);
        Assert.Equal("A", listBox.Items[2]);
    }

    [Fact]
    public void MoveItem_SetsSelectedIndexToTargetPosition()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox("A", "B", "C");

        listBox.MoveItem(0, 2);

        Assert.Equal(2, listBox.SelectedIndex);
    }

    [Fact]
    public void MoveItem_RaisesItemsReordered()
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox("A", "B", "C");
        bool raised = false;
        listBox.ItemsReordered += (sender, e) => raised = true;

        listBox.MoveItem(0, 2);

        Assert.True(raised);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(3, 1)]
    [InlineData(0, -1)]
    [InlineData(0, 3)]
    public void MoveItem_WithOutOfRangeIndex_IsANoOp(int fromIndex, int toIndex)
    {
        using WfuiListBox listBox = ListBoxTestHelpers.CreateListBox("A", "B", "C");
        bool raised = false;
        listBox.ItemsReordered += (sender, e) => raised = true;

        listBox.MoveItem(fromIndex, toIndex);

        Assert.Equal("A", listBox.Items[0]);
        Assert.Equal("B", listBox.Items[1]);
        Assert.Equal("C", listBox.Items[2]);
        Assert.False(raised);
    }
}
