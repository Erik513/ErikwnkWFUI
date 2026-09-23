using System.Windows.Forms;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

internal static class ListViewTestHelpers
{
    // Deliberately doesn't force the native handle to be created, unlike
    // GridTestHelpers' CreateGrid - DataGridView needs one before its
    // data-bound Columns/Rows populate, but this control's Items/Columns
    // exist in managed state regardless. Forcing a handle here crashes the
    // test host once enough of these pile up in one run (HeaderInputSubclass
    // attaches to the real header hwnd, which doesn't hold up without a
    // real message pump) - and none of these tests need real header
    // painting or native hit-testing anyway.
    public static WfuiListView CreateListView(params (string Header, int Width)[] columns)
    {
        WfuiListView listView = new WfuiListView();

        foreach ((string header, int width) in columns)
        {
            listView.Columns.Add(header, width);
        }

        return listView;
    }

    public static void AddItem(WfuiListView listView, params string[] cellText)
    {
        listView.Items.Add(new ListViewItem(cellText));
    }
}
