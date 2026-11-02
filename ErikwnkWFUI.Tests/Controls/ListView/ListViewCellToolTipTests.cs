using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>The tooltip with the whole text of a cell: shown where the text is cut off, remembered per cell, and taken away where the mouse leaves.</summary>
public class ListViewCellToolTipTests : IDisposable
{
    private const string LongText = "A text that is much too long for a narrow column of this list";

    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private WfuiListView Show()
    {
        WfuiListView list = new WfuiListView { View = View.Details, FullRowSelect = true };
        list.Columns.Add("A", 60);
        list.Columns.Add("B", 200);
        list.Items.Add(new ListViewItem(new[] { LongText, "short" }));
        list.Items.Add(new ListViewItem(new[] { "x", LongText }));
        list.Items.Add(new ListViewItem(new[] { "", "" }));
        list.Dock = DockStyle.Fill;
        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            ClientSize = new Size(400, 200)
        };
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return list;
    }

    private static void Move(WfuiListView list, Point at) => list.InvokePrivate("OnListViewMouseMoveForToolTip", list, new MouseEventArgs(MouseButtons.None, 0, at.X, at.Y, 0));

    private static (int Row, int Column) Remembered(WfuiListView list) => (list.GetPrivateField<int>("_toolTipRow"), list.GetPrivateField<int>("_toolTipDisplayColumn"));

    [Fact]
    public void OverACellWhoseTextIsCut_TheCellIsRemembered_AndTheSameCellAgainChangesNothing()
    {
        WfuiListView list = Show();
        Rectangle row = list.Items[0].Bounds;
        Point at = new Point(20, row.Top + row.Height / 2);

        Move(list, at);
        Assert.Equal((0, 0), Remembered(list));

        Move(list, new Point(25, at.Y));
        Assert.Equal((0, 0), Remembered(list));
    }

    [Fact]
    public void OverACellWhoseTextFits_OrIsEmpty_OrOnNoRow_NoTooltipStaysForIt()
    {
        WfuiListView list = Show();
        Rectangle first = list.Items[0].Bounds;
        Rectangle empty = list.Items[2].Bounds;

        Move(list, new Point(100, first.Top + first.Height / 2));
        Assert.Equal((0, 1), Remembered(list));

        Move(list, new Point(100, empty.Top + empty.Height / 2));
        Assert.Equal((2, 1), Remembered(list));

        // (Below the last row: no row at all.)
        Move(list, new Point(100, list.ClientSize.Height - 3));
        Assert.Equal((-1, -1), Remembered(list));
    }

    [Fact]
    public void AMoveBeyondTheColumns_AndTheMouseLeaving_LeaveNoCellRemembered()
    {
        WfuiListView list = Show();
        Rectangle row = list.Items[1].Bounds;

        Move(list, new Point(260 + 40, row.Top + row.Height / 2));
        Assert.Equal((-1, -1), Remembered(list));

        Move(list, new Point(100, row.Top + row.Height / 2));
        Assert.Equal((1, 1), Remembered(list));

        list.InvokePrivate("OnListViewMouseLeave", list, EventArgs.Empty);
        Assert.Equal((-1, -1), Remembered(list));
    }

    [Fact]
    public void TheCheckForACutText_AlsoAnswersForAColumnWithNoRoomAtAll()
    {
        WfuiListView list = Show();
        list.Columns[0].Width = 2;

        Assert.True(list.InvokePrivate<bool>("IsTextTruncated", "x", list.Font, list.Columns[0]));

        list.Columns[1].Width = 400;
        Assert.False(list.InvokePrivate<bool>("IsTextTruncated", "short", list.Font, list.Columns[1]));
        Assert.True(list.InvokePrivate<bool>("IsTextTruncated", LongText + LongText + LongText, list.Font, list.Columns[1]));
    }
}
