using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// How the list selects: a click on the only selected row takes the selection away, a press on a row and a move to another selects the rows between,
/// Ctrl+A selects all, and the rows are found from a point. The mouse is put where the test wants it (<c>CursorPosition</c>, <c>PressedButtons</c>).
/// </summary>
public class ListViewSelectionTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private WfuiListView Show(int items = 6)
    {
        WfuiListView list = new WfuiListView { View = View.Details, FullRowSelect = true, MultiSelect = true };
        list.Columns.Add("A", 200);

        for (int i = 0; i < items; i++)
            list.Items.Add(new ListViewItem("row " + i));

        list.Dock = DockStyle.Fill;
        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            ClientSize = new Size(300, 300)
        };
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return list;
    }

    private static Point Middle(ListViewItem item) => new Point(40, item.Bounds.Top + item.Bounds.Height / 2);

    private static MouseEventArgs Left(Point at) => new MouseEventArgs(MouseButtons.Left, 1, at.X, at.Y, 0);

    private static void Press(WfuiListView list, Point at, string handler) => list.InvokePrivate(handler, list, Left(at));

    [Fact]
    public void AClickOnTheOnlySelectedRow_TakesTheSelectionAway_AndAnotherRowIsSelectedAsUsual()
    {
        WfuiListView list = Show();
        list.Items[2].Selected = true;
        Point at = Middle(list.Items[2]);

        Press(list, at, "OnListViewMouseDownForToggleDeselect");
        Press(list, at, "OnListViewMouseUpForToggleDeselect");

        Assert.False(list.Items[2].Selected);
        Assert.Equal(2, list.GetPrivateField<int>("_toggleDeselectWatchIndex"));

        // (The system selects the row again a moment later - the watch undoes that at once.)
        list.Items[2].Selected = true;
        Assert.False(list.Items[2].Selected);
        Assert.Equal(-1, list.GetPrivateField<int>("_toggleDeselectWatchIndex"));
    }

    [Fact]
    public void TheToggle_NeedsTheLeftButtonNoKeysAndTheSameRowAtTheRelease()
    {
        WfuiListView list = Show();
        list.Items[1].Selected = true;
        Point on = Middle(list.Items[1]);
        Point other = Middle(list.Items[3]);

        list.InvokePrivate("OnListViewMouseDownForToggleDeselect", list, new MouseEventArgs(MouseButtons.Right, 1, on.X, on.Y, 0));
        list.InvokePrivate("OnListViewMouseUpForToggleDeselect", list, new MouseEventArgs(MouseButtons.Right, 1, on.X, on.Y, 0));
        Assert.True(list.Items[1].Selected);

        Press(list, on, "OnListViewMouseDownForToggleDeselect");
        Press(list, other, "OnListViewMouseUpForToggleDeselect");
        Assert.True(list.Items[1].Selected);

        list.Items[3].Selected = true;
        Press(list, on, "OnListViewMouseDownForToggleDeselect");
        Press(list, on, "OnListViewMouseUpForToggleDeselect");
        Assert.True(list.Items[1].Selected);
    }

    [Fact]
    public void TheSettleTimer_EndsTheWatchOfTheToggle()
    {
        WfuiListView list = Show();
        list.Items[0].Selected = true;
        Point at = Middle(list.Items[0]);
        Press(list, at, "OnListViewMouseDownForToggleDeselect");
        Press(list, at, "OnListViewMouseUpForToggleDeselect");
        Assert.Equal(0, list.GetPrivateField<int>("_toggleDeselectWatchIndex"));

        list.InvokePrivate("OnToggleDeselectSettleTimerTick", list, EventArgs.Empty);

        Assert.Equal(-1, list.GetPrivateField<int>("_toggleDeselectWatchIndex"));
    }

    [Fact]
    public void APressOnARowAndAMoveToAnother_SelectsTheRowsBetween_AndTheReleaseEndsIt()
    {
        WfuiListView list = Show();
        Point first = Middle(list.Items[1]);
        Point third = Middle(list.Items[4]);

        Press(list, first, "OnListViewMouseDownForRowRangeDrag");
        Assert.True(list.GetPrivateField<bool>("_isRowRangeDragging"));

        list.PressedButtons = () => MouseButtons.Left;
        list.CursorPosition = () => list.PointToScreen(third);
        list.InvokePrivate("OnRowRangeDragPollTick", list, EventArgs.Empty);

        Assert.Equal(new[] { 1, 2, 3, 4 }, list.SelectedIndices.Cast<int>().ToArray());

        // (Back to the first row: the range shrinks to it.)
        list.CursorPosition = () => list.PointToScreen(first);
        list.InvokePrivate("OnRowRangeDragPollTick", list, EventArgs.Empty);
        Assert.Equal(new[] { 1 }, list.SelectedIndices.Cast<int>().ToArray());

        Press(list, first, "OnListViewMouseUpForRowRangeDrag");
        Assert.False(list.GetPrivateField<bool>("_isRowRangeDragging"));
    }

    [Fact]
    public void TheDragOutsideTheRows_SelectsUpToTheFirstAndTheLastRow_AndEndsWhenTheButtonIsLetGo()
    {
        WfuiListView list = Show();
        Press(list, Middle(list.Items[2]), "OnListViewMouseDownForRowRangeDrag");
        list.PressedButtons = () => MouseButtons.Left;

        list.CursorPosition = () => list.PointToScreen(new Point(40, list.ClientSize.Height + 20));
        list.InvokePrivate("OnRowRangeDragPollTick", list, EventArgs.Empty);
        Assert.Equal(5, list.SelectedIndices[list.SelectedIndices.Count - 1]);

        list.CursorPosition = () => list.PointToScreen(new Point(40, 0));
        list.InvokePrivate("OnRowRangeDragPollTick", list, EventArgs.Empty);
        Assert.Equal(0, list.SelectedIndices[0]);

        list.PressedButtons = () => MouseButtons.None;
        list.InvokePrivate("OnRowRangeDragPollTick", list, EventArgs.Empty);
        Assert.False(list.GetPrivateField<bool>("_isRowRangeDragging"));

        // (A poll that comes after the end does nothing.)
        list.InvokePrivate("OnRowRangeDragPollTick", list, EventArgs.Empty);
    }

    [Fact]
    public void APressOnEmptySpaceOrWithAKeyOrTheRightButton_StartsNoRangeDrag()
    {
        WfuiListView list = Show(items: 2);
        Point empty = new Point(40, list.ClientSize.Height - 5);

        Press(list, empty, "OnListViewMouseDownForRowRangeDrag");
        Assert.False(list.GetPrivateField<bool>("_isRowRangeDragging"));

        list.InvokePrivate("OnListViewMouseDownForRowRangeDrag", list, new MouseEventArgs(MouseButtons.Right, 1, 40, 5, 0));
        Assert.False(list.GetPrivateField<bool>("_isRowRangeDragging"));
    }

    [Fact]
    public void CtrlA_SelectsAll_AndTheSelectionCanBeTakenAwayAgain()
    {
        WfuiListView list = Show();

        list.InvokePrivate("OnListViewKeyDown", list, new KeyEventArgs(Keys.Control | Keys.A));
        Assert.Equal(6, list.SelectedItems.Count);

        list.InvokePrivate("DeselectAll");
        Assert.Empty(list.SelectedItems);

        using WfuiListView empty = new WfuiListView();
        empty.InvokePrivate("SelectAll");
        Assert.Empty(empty.SelectedItems);
    }

    [Fact]
    public void TheRowRangeAndTheHitTest_IgnoreWhatIsNotThere()
    {
        WfuiListView list = Show(items: 3);
        list.InvokePrivate("ApplyRowRangeSelection", -1, 2);
        list.InvokePrivate("ApplyRowRangeSelection", 0, 9);
        Assert.Empty(list.SelectedItems);

        using WfuiListView noColumns = new WfuiListView();
        Assert.Equal(-1, noColumns.InvokePrivate<int>("GetNearestRowIndex", new Point(5, 5)));
        Assert.Null(noColumns.InvokePrivate<ListViewHitTestInfo>("SafeHitTest", new Point(5, 5))!.Item);
    }
}
