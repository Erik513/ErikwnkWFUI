using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>RowHeight is made with an image list of the height wanted; the lists that are no longer needed are given back.</summary>
public class ListViewRowHeightTests
{
    private static ImageList? RowHeightList(WfuiListView list) => list.GetPrivateField<ImageList>("_rowHeightImageList");

    [Fact]
    public void ANewRowHeight_GivesBackTheImageListOfTheOldOne()
    {
        using WfuiListView list = new WfuiListView { RowHeight = 30 };
        ImageList first = RowHeightList(list)!;
        bool disposed = false;
        first.Disposed += (s, e) => disposed = true;

        list.RowHeight = 40;

        Assert.True(disposed);
        Assert.Equal(40, list.RowHeight);
        Assert.NotSame(first, RowHeightList(list));
    }

    [Fact]
    public void ARowHeightOfZero_GivesBackTheImageList_AndTheRowsSizeThemselvesAgain()
    {
        using WfuiListView list = new WfuiListView { RowHeight = 30 };
        ImageList first = RowHeightList(list)!;
        bool disposed = false;
        first.Disposed += (s, e) => disposed = true;

        list.RowHeight = 0;

        Assert.True(disposed);
        Assert.Equal(0, list.RowHeight);
        Assert.Null(list.SmallImageList);
    }

    [Fact]
    public void DisposingTheList_GivesBackTheImageList()
    {
        WfuiListView list = new WfuiListView { RowHeight = 30 };
        bool disposed = false;
        RowHeightList(list)!.Disposed += (s, e) => disposed = true;

        list.Dispose();

        Assert.True(disposed);
    }
}
