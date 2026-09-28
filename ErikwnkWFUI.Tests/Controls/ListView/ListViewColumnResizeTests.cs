using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// Column resize is hand-rolled in HeaderInputSubclass (mouse deltas via
/// SetCapture on the header hwnd), same as the reorder drag - not
/// practical to drive headlessly. Any width change still goes through
/// OnColumnWidthChanging's validation/clamping, so that's what these tests
/// cover, invoking it directly via reflection.
///
/// Is/Set/AllowColumnResizable are thin wrappers over the shared
/// ColumnFeatureSwitch (see ColumnFeatureSwitchTests, in the parent Controls
/// test folder) - the default/master-switch/re-enable matrix is covered
/// there once instead of per control; this file keeps just the one test
/// below confirming this control's own property actually delegates to it.
/// </summary>
public class ListViewColumnResizeTests
{
    [Fact]
    public void SetColumnResizable_False_MakesOnlyThatColumnNotResizable()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("Item", 100), ("Status", 100));

        listView.SetColumnResizable(0, false);

        Assert.False(listView.IsColumnResizable(0));
        Assert.True(listView.IsColumnResizable(1));

        listView.SetColumnResizable(0, true);
        Assert.True(listView.IsColumnResizable(0));
    }

    [Fact]
    public void ColumnWidthChanging_BelowMinimum_ClampsToMinimumColumnWidth()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("A", 100));
        listView.MinimumColumnWidth = 40;

        ColumnWidthChangingEventArgs args = new ColumnWidthChangingEventArgs(0, 5);
        listView.InvokePrivate("OnColumnWidthChanging", listView, args);

        Assert.True(args.Cancel);
        Assert.Equal(40, args.NewWidth);
    }

    [Fact]
    public void ColumnWidthChanging_AboveMinimum_IsLeftAlone()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("A", 100));
        listView.MinimumColumnWidth = 40;

        ColumnWidthChangingEventArgs args = new ColumnWidthChangingEventArgs(0, 120);
        listView.InvokePrivate("OnColumnWidthChanging", listView, args);

        Assert.False(args.Cancel);
        Assert.Equal(120, args.NewWidth);
    }

    [Fact]
    public void ColumnWidthChanging_ColumnNotResizable_AlwaysCancelledBackToCurrentWidth()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("A", 100), ("B", 100));
        listView.SetColumnResizable(0, false);

        ColumnWidthChangingEventArgs args = new ColumnWidthChangingEventArgs(0, 150);
        listView.InvokePrivate("OnColumnWidthChanging", listView, args);

        Assert.True(args.Cancel);
        Assert.Equal(100, args.NewWidth);
    }

    // GetEffectiveMinimumWidth picks whichever of MinimumColumnWidth or the
    // column's own header-text width is larger, so a caption never gets
    // clipped even if MinimumColumnWidth was configured smaller than it -
    // the other tests above only ever exercise the "MinimumColumnWidth
    // wins" side (a short "A"/"Name" caption), never this one.
    [Fact]
    public void ColumnWidthChanging_HeaderTextWiderThanMinimumColumnWidth_ClampsToHeaderTextWidthInstead()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("A much longer column caption", 200));
        listView.MinimumColumnWidth = 1;

        ColumnWidthChangingEventArgs args = new ColumnWidthChangingEventArgs(0, 5);
        listView.InvokePrivate("OnColumnWidthChanging", listView, args);

        Assert.True(args.Cancel);
        Assert.True(args.NewWidth > listView.MinimumColumnWidth);
    }

    // The fix for a real bug: the trailing-background fill used to read a
    // value cached from the header's own DrawColumnHeader event, which
    // could go stale because the header and the row area are two separate
    // native windows that don't repaint in lockstep (see ListView.cs's own
    // history on GetColumnsTotalWidth). This locks in that it's instead
    // always the live, correct sum of every column's current width.
    [Fact]
    public void GetColumnsTotalWidth_SumsEveryColumnsCurrentWidth()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("A", 100), ("B", 50));

        Assert.Equal(150, listView.InvokePrivate<int>("GetColumnsTotalWidth"));

        listView.Columns[0].Width = 40;

        Assert.Equal(90, listView.InvokePrivate<int>("GetColumnsTotalWidth"));
    }

    [Fact]
    public void GetColumnsTotalWidth_NoColumns_IsZero()
    {
        using WfuiListView listView = new WfuiListView();

        Assert.Equal(0, listView.InvokePrivate<int>("GetColumnsTotalWidth"));
    }

    // AutoFitColumnsToContent needs a real native handle (AutoResizeColumn
    // is a native operation) - ListViewTestHelpers.CreateListView
    // deliberately never forces one (see its own comment), so the actual
    // resizing can't be exercised headlessly. This only locks in the guard
    // clause itself: calling it before a handle exists, or with no columns,
    // must be a safe no-op rather than reaching that native call.
    [Fact]
    public void AutoFitColumnsToContent_NoHandleYet_DoesNotThrow()
    {
        using WfuiListView listView = ListViewTestHelpers.CreateListView(("A", 100));

        Exception exception = Record.Exception(() => listView.AutoFitColumnsToContent());

        Assert.Null(exception);
    }

    [Fact]
    public void AutoFitColumnsToContent_NoColumns_DoesNotThrow()
    {
        using WfuiListView listView = new WfuiListView();

        Exception exception = Record.Exception(() => listView.AutoFitColumnsToContent());

        Assert.Null(exception);
    }
}
