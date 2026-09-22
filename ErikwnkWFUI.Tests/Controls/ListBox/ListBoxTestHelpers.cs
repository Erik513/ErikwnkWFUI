using WfuiListBox = ErikwnkWFUI.Controls.ListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

internal static class ListBoxTestHelpers
{
    public static WfuiListBox CreateListBox(params string[] items)
    {
        WfuiListBox listBox = new WfuiListBox();

        foreach (string item in items)
        {
            listBox.Items.Add(item);
        }

        return listBox;
    }

    // Forces the native handle - needed for anything that depends on real
    // item layout (IndexFromPoint, GetItemRectangle), which .NET's own
    // ListBox only computes once the handle actually exists. Safe to force
    // here unlike ListView's own equivalent helper (which deliberately does
    // NOT do this by default) - this control has no native child-window
    // subclassing of its own (no HeaderInputSubclass-style NativeWindow),
    // so there's nothing for repeated handle creation/destruction to
    // destabilize the way it did there.
    public static WfuiListBox CreateListBoxWithHandle(int itemHeight, params string[] items)
    {
        WfuiListBox listBox = CreateListBox(items);
        listBox.ItemHeightCustom = itemHeight;
        _ = listBox.Handle;
        return listBox;
    }
}
