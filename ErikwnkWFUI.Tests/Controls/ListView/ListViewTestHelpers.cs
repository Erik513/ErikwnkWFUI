using System.Windows.Forms;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

internal static class ListViewTestHelpers
{
    // Deliberately does NOT force the native window handle to be created,
    // unlike GridTestHelpers' own CreateGrid - DataGridView genuinely
    // needs one (plus a BindingContext) before its Columns/Rows populate
    // at all, since those come from data binding. This control's Items
    // and Columns are added directly and already exist in managed state
    // regardless of Handle - forcing one here was confirmed live to make
    // the test host process crash outright once enough of these had been
    // created and disposed within one run (HeaderInputSubclass attaches a
    // NativeWindow to the real header hwnd on OnHandleCreated, and
    // something about doing that many times over in a tight loop, with no
    // actual message pump running the way a shown app has one, doesn't
    // hold up - not something any of these tests' own assertions actually
    // need to exercise anyway, since none of them depend on real header
    // painting or native mouse hit-testing).
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
