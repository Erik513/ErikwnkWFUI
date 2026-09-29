using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// CopySelection/CopySelectionAsTable, invoked directly via reflection (see
/// PrivateReflection) since they're only ever reached from Ctrl+C/Ctrl+Shift+C
/// or the context menu in real use. Every test runs its clipboard-touching
/// part on an STA thread (see StaThread) - Clipboard access throws off the
/// default MTA thread xUnit runs tests on. CopyConfirmation is set to None
/// so ShowCopyToast doesn't try to show a ToastForm - harmless here anyway
/// since FindForm() returns null for a listView with no parent Form, but
/// explicit is clearer than relying on that.
///
/// Every test here also forces a real handle via "_ = listView.Handle;",
/// unlike ListViewTestHelpers.CreateListView's own no-handle default -
/// confirmed live that SelectedIndices/SelectedItems simply don't track a
/// selection at all before a real handle exists (neither SelectedIndices.Add
/// nor ListViewItem.Selected registers), and CopySelectionCore's own guard
/// (SelectedItems.Count == 0 -> return) means every one of these tests would
/// silently no-op without it. Safe here specifically because none of these
/// tests touch HeaderInputSubclass's own column-drag machinery (the thing
/// that helper's comment warns doesn't hold up without a real message pump)
/// - same reasoning GridTestHelpers.CreateGrid already relies on for every
/// DataGridView test.
/// </summary>
[Collection(ClipboardTestCollection.Name)]
public class ListViewCopyTests
{
    [Fact]
    public void CopySelection_PutsTabSeparatedRowsOnClipboard_InItemOrderNotSelectionOrder()
    {
        StaThread.Run(() =>
        {
            using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100), ("Status", 100));
            listView.CopyConfirmation = CopyConfirmationStyle.None;
            ListViewTestHelpers.AddItem(listView, "B", "Y");
            ListViewTestHelpers.AddItem(listView, "A", "X");
            _ = listView.Handle;

            // Selected in reverse of item order - the copied order must
            // still come out top-to-bottom (Index order), not selection
            // click order.
            listView.SelectedIndices.Add(1);
            listView.SelectedIndices.Add(0);

            listView.InvokePrivate("CopySelection");

            Assert.Equal("B\tY\r\nA\tX\r\n", Clipboard.GetText());
        });
    }

    [Fact]
    public void CopySelectionAsTable_IncludesAHeaderRow()
    {
        StaThread.Run(() =>
        {
            using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100), ("Status", 100));
            listView.CopyConfirmation = CopyConfirmationStyle.None;
            ListViewTestHelpers.AddItem(listView, "A", "X");
            _ = listView.Handle;
            listView.SelectedIndices.Add(0);

            listView.InvokePrivate("CopySelectionAsTable");

            Assert.Equal("Name\tStatus\r\nA\tX\r\n", Clipboard.GetText());
        });
    }

    [Fact]
    public void CopySelection_NoSelection_LeavesTheClipboardUntouched()
    {
        StaThread.Run(() =>
        {
            using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100));
            listView.CopyConfirmation = CopyConfirmationStyle.None;
            ListViewTestHelpers.AddItem(listView, "A");
            _ = listView.Handle;

            Clipboard.SetText("sentinel");
            listView.InvokePrivate("CopySelection");

            Assert.Equal("sentinel", Clipboard.GetText());
        });
    }

    // The plain-text clipboard content is the universal fallback, but
    // "HTML Format" rides alongside it so apps that understand it (Word,
    // Outlook, browsers, Excel, ...) paste an actual table instead of tab-
    // separated text - see BuildCfHtmlTable/BuildHtmlTable for how that
    // gets built.
    [Fact]
    public void CopySelection_AlsoPutsHtmlFormatOnTheClipboard()
    {
        StaThread.Run(() =>
        {
            using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100));
            listView.CopyConfirmation = CopyConfirmationStyle.None;
            ListViewTestHelpers.AddItem(listView, "A");
            _ = listView.Handle;
            listView.SelectedIndices.Add(0);

            listView.InvokePrivate("CopySelection");

            IDataObject data = Clipboard.GetDataObject()!;
            Assert.True(data.GetDataPresent(DataFormats.Html));
        });
    }

    [Fact]
    public void CopySelection_UsesColumnsInDisplayOrder_NotDeclarationOrder()
    {
        StaThread.Run(() =>
        {
            using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100), ("Status", 100));
            listView.CopyConfirmation = CopyConfirmationStyle.None;
            ListViewTestHelpers.AddItem(listView, "A", "X");
            _ = listView.Handle;
            listView.SelectedIndices.Add(0);

            // Swap the two columns' visual order without touching Items -
            // the copied row must follow the new (Status, Name) order.
            listView.Columns[0].DisplayIndex = 1;
            listView.Columns[1].DisplayIndex = 0;

            listView.InvokePrivate("CopySelection");

            Assert.Equal("X\tA\r\n", Clipboard.GetText());
        });
    }

    private static System.Windows.Forms.ToolStripMenuItem MenuItem(WfuiListView listView, int index)
        => (System.Windows.Forms.ToolStripMenuItem)listView.ContextMenuStrip!.Items[index];

    [Fact]
    public void ContextMenu_IsFlat_WithTheSameEntriesAsTheDataGridViews()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100));

        Assert.Equal(7, listView.ContextMenuStrip!.Items.Count);
        Assert.Equal("Copy", MenuItem(listView, 0).Text);
        Assert.Equal("Copy with header", MenuItem(listView, 1).Text);
        Assert.Equal("Copy all", MenuItem(listView, 3).Text);
        Assert.Equal("Copy all with header", MenuItem(listView, 4).Text);
        Assert.Equal("Select all", MenuItem(listView, 6).Text);
        Assert.All(listView.ContextMenuStrip.Items.Cast<System.Windows.Forms.ToolStripItem>(),
            item => Assert.False(item is System.Windows.Forms.ToolStripDropDownItem d && d.HasDropDownItems));
    }

    [Fact]
    public void ContextMenu_EnableStates_FollowSelectionAndItems()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100));
        listView.ContextMenuStrip!.InvokePrivate("OnOpening", new System.ComponentModel.CancelEventArgs());
        Assert.All(new[] { 0, 1, 3, 4, 6 }, i => Assert.False(MenuItem(listView, i).Enabled));

        ListViewTestHelpers.AddItem(listView, "A");
        listView.ContextMenuStrip!.InvokePrivate("OnOpening", new System.ComponentModel.CancelEventArgs());
        Assert.False(MenuItem(listView, 0).Enabled);
        Assert.False(MenuItem(listView, 1).Enabled);
        Assert.True(MenuItem(listView, 3).Enabled);
        Assert.True(MenuItem(listView, 4).Enabled);
        Assert.True(MenuItem(listView, 6).Enabled);
    }

    [Fact]
    public void ContextMenu_CopyAllWithHeader_PutsHeaderAndEveryRowOnTheClipboard()
    {
        StaThread.Run(() =>
        {
            using WfuiListView listView = ListViewTestHelpers.CreateListView(("Name", 100));
            listView.CopyConfirmation = CopyConfirmationStyle.None;
            ListViewTestHelpers.AddItem(listView, "A");
            ListViewTestHelpers.AddItem(listView, "B");
            _ = listView.Handle;

            MenuItem(listView, 4).PerformClick();

            string text = Clipboard.GetText();
            Assert.StartsWith("Name", text.TrimStart());
            Assert.Contains("A", text);
            Assert.Contains("B", text);
        });
    }
}
