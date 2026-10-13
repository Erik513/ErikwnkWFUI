using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>A click somewhere else deselects: the watch of the whole application runs only while the list has a window.</summary>
public class ListViewOutsideClickTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private static bool Watching(WfuiListView list)
    {
        object filter = list.GetPrivateField<object>("_outsideClickDeselectFilter")!;

        return filter.GetPrivateField<bool>("_started");
    }

    private (Form Form, WfuiListView List) Show()
    {
        WfuiListView list = new WfuiListView();
        list.Columns.Add("A", 100);
        list.Items.Add(new ListViewItem("one"));
        list.Items.Add(new ListViewItem("two"));
        list.Dock = DockStyle.Top;

        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            ClientSize = new Size(400, 400)
        };
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return (form, list);
    }

    [Fact]
    public void AListWithoutAWindow_WatchesNothing()
    {
        using WfuiListView list = new WfuiListView();

        Assert.False(Watching(list));
    }

    [Fact]
    public void TheWatch_RunsWhileTheListHasAWindow_AndEndsWithIt()
    {
        (Form form, WfuiListView list) = Show();
        Assert.True(Watching(list));

        form.Controls.Remove(list);
        list.Dispose();

        Assert.False(Watching(list));
    }

    [Fact]
    public void TheWatch_EndsWhenTheWindowIsDestroyed_AndStartsAgainWithTheNext()
    {
        (Form form, WfuiListView list) = Show();

        list.InvokePrivate("DestroyHandle");
        Assert.False(Watching(list));

        _ = list.Handle;
        Assert.True(Watching(list));
    }
}
