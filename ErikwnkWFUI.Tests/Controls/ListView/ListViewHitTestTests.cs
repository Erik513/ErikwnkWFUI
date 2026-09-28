using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// SafeHitTest, invoked directly via reflection - wraps a real, pre-existing
/// .NET WinForms bug: the stock ListView.HitTest throws ArgumentOutOfRangeException
/// internally in View.Details when Columns.Count == 0, unrelated to anything
/// in this library. This is the regression test for that fix - every guard
/// clause here must return a safe "nothing hit" result instead of ever
/// reaching the native HitTest call that would throw.
/// </summary>
public class ListViewHitTestTests
{
    [Fact]
    public void SafeHitTest_NoColumns_ReturnsNoneInsteadOfThrowing()
    {
        using WfuiListView listView = new WfuiListView();

        ListViewHitTestInfo result = listView.InvokePrivate<ListViewHitTestInfo>("SafeHitTest", new Point(5, 5))!;

        Assert.Null(result.Item);
        Assert.Equal(ListViewHitTestLocations.None, result.Location);
    }

    // ListViewTestHelpers.CreateListView deliberately never forces a real
    // handle to be created (see its own comment), which also means this
    // case and SafeHitTest's location-outside-ClientRectangle guard can't
    // be told apart headlessly - IsHandleCreated is false here regardless
    // of the point passed in, so this exercises the "no handle yet" branch
    // specifically (a real column exists, unlike the test above).
    [Fact]
    public void SafeHitTest_NoHandleCreatedYet_ReturnsNoneInsteadOfThrowing()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100));

        Assert.False(listView.IsHandleCreated);

        ListViewHitTestInfo result = listView.InvokePrivate<ListViewHitTestInfo>("SafeHitTest", new Point(5, 5))!;

        Assert.Null(result.Item);
        Assert.Equal(ListViewHitTestLocations.None, result.Location);
    }
}
