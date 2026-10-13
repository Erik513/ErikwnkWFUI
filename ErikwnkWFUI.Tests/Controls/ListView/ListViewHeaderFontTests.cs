using System.Drawing;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>The font of the column headers: the list holds one (no new font for every read and every header painted).</summary>
public class ListViewHeaderFontTests
{
    [Fact]
    public void TheDefaultHeaderFont_IsBoldInTheSizeOfTheFont_AndTheSameOneEachTime()
    {
        using WfuiListView list = new WfuiListView();

        Font first = list.HeaderFont;

        Assert.Same(first, list.HeaderFont);
        Assert.True(first.Bold);
        Assert.Equal(list.Font.Size, first.Size);
    }

    [Fact]
    public void ANewFont_GivesTheHeaderANewFont_AndTheOldOneIsGivenBack()
    {
        using WfuiListView list = new WfuiListView();
        Font old = list.HeaderFont;

        list.Font = new Font(list.Font.FontFamily, 18f);

        Font current = list.HeaderFont;
        Assert.NotSame(old, current);
        Assert.Equal(18f, current.Size);
        Assert.Throws<ArgumentException>(() => _ = old.Height);
    }

    [Fact]
    public void AHeaderFontThatWasSet_IsTheOneRead_AndStaysTheCallers()
    {
        using Font mine = new Font("Segoe UI", 14f, FontStyle.Italic);
        WfuiListView list = new WfuiListView { HeaderFont = mine };

        Assert.Same(mine, list.HeaderFont);

        list.Font = new Font(list.Font.FontFamily, 12f);
        list.Dispose();

        Assert.Equal(14f, mine.Size);
    }

    [Fact]
    public void DisposingTheList_GivesBackTheHeaderFontItMade()
    {
        WfuiListView list = new WfuiListView();
        Font made = list.HeaderFont;

        list.Dispose();

        Assert.Throws<ArgumentException>(() => _ = made.Height);
    }
}
