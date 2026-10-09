using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// The list that is as big as its content (FixedRowCount, AutoSizeToContent): exactly as wide as its
/// columns and as high as the header and its rows, nothing to scroll, sort, move or select.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ListViewDisplayTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private WfuiListView Show(int rows, int items, params int[] widths)
    {
        WfuiListView list = new WfuiListView { FixedRowCount = rows, RowHeight = 28, AllowColumnResizing = false };

        for (int i = 0; i < widths.Length; i++)
            list.Columns.Add("Column " + i, widths[i]);

        for (int i = 0; i < items; i++)
            list.Items.Add(new ListViewItem(new[] { "a", "b", "c" }));

        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            ClientSize = new Size(900, 900)
        };
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return list;
    }

    [Fact]
    public void TheDisplay_IsAsWideAsItsColumns_WhateverSizeItIsGiven()
    {
        WfuiListView list = Show(5, 3, 100, 80, 60);

        Assert.Equal(240, list.Width);

        list.Width = 500;
        list.Size = new Size(10, 10);
        list.Dock = DockStyle.Fill;
        Application.DoEvents();

        Assert.Equal(240, list.Width);
    }

    [Fact]
    public void TheDisplay_IsAsHighAsTheHeaderAndItsRows_AndHasNoScrollBar()
    {
        WfuiListView list = Show(20, 20, 100, 80, 60);

        // The last row ends where the list ends: nothing is cut off, nothing is left over.
        Assert.Equal(list.Items[19].Bounds.Bottom, list.ClientSize.Height);
        Assert.False(list.Scrollable);
    }

    [Fact]
    public void FewerItems_LeaveTheRoom_AndMoreRows_MakeItHigher()
    {
        WfuiListView few = Show(10, 2, 100, 80, 60);
        WfuiListView many = Show(20, 2, 100, 80, 60);

        Assert.True(many.Height > few.Height);

        // The same height as with every row filled.
        WfuiListView full = Show(10, 10, 100, 80, 60);
        Assert.Equal(full.Height, few.Height);
    }

    [Fact]
    public void AColumnThatIsAddedOrWidened_ChangesTheWidth()
    {
        WfuiListView list = Show(5, 1, 100, 80);
        Assert.Equal(180, list.Width);

        list.Columns.Add("More", 50);
        Application.DoEvents();
        Assert.Equal(230, list.Width);

        list.Columns[0].Width = 120;
        Application.DoEvents();
        Assert.Equal(250, list.Width);
    }

    [Fact]
    public void ANewRowHeight_OrFont_ChangesTheHeight()
    {
        WfuiListView list = Show(5, 1, 100);
        int before = list.Height;

        list.RowHeight = 40;
        Application.DoEvents();

        Assert.True(list.Height > before);
    }

    [Fact]
    public void AFixedRowCount_LeavesSortingReorderingAndSelectingAlone()
    {
        WfuiListView list = Show(5, 3, 100, 80, 60);

        Assert.True(list.SortingEnabled);
        Assert.True(list.AllowColumnReordering);
        Assert.True(list.AllowSelection);
        Assert.Equal(ColumnHeaderStyle.Clickable, list.HeaderStyle);

        list.Items[1].Selected = true;
        Assert.Single(list.SelectedItems);
    }

    [Fact]
    public void WithTheSwitchesOff_TheDisplayOnlyShows()
    {
        WfuiListView list = Show(5, 3, 100, 80, 60);
        list.SortingEnabled = false;
        list.AllowColumnReordering = false;
        list.AllowSelection = false;

        list.Items[1].Selected = true;
        Application.DoEvents();

        Assert.Empty(list.SelectedItems);
        Assert.Equal(240, list.Width);
    }

    // ---- AutoSizeToContent ----

    private WfuiListView ShowAuto(int items, Size maximum, params int[] widths)
    {
        WfuiListView list = new WfuiListView { AutoSizeToContent = true, RowHeight = 28, MaximumSize = maximum };

        for (int i = 0; i < widths.Length; i++)
            list.Columns.Add("Column " + i, widths[i]);

        for (int i = 0; i < items; i++)
            list.Items.Add(new ListViewItem(new[] { "a", "b", "c" }));

        Form form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            ClientSize = new Size(900, 900)
        };
        form.Controls.Add(list);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return list;
    }

    [Fact]
    public void AutoSizeToContent_GrowsAndShrinksWithTheItems_AndIsAsWideAsTheColumns()
    {
        WfuiListView list = ShowAuto(3, Size.Empty, 100, 80, 60);
        int three = list.Height;
        Assert.Equal(240, list.Width);
        Assert.Equal(list.Items[2].Bounds.Bottom, list.ClientSize.Height);

        for (int i = 0; i < 3; i++)
            list.Items.Add(new ListViewItem(new[] { "a", "b", "c" }));
        Application.DoEvents();

        Assert.True(list.Height > three);
        Assert.Equal(list.Items[5].Bounds.Bottom, list.ClientSize.Height);

        list.Items.Clear();
        Application.DoEvents();

        Assert.True(list.Height < three);
    }

    [Fact]
    public void AboveTheMaximumHeight_TheListStopsGrowing_AndTheScrollBarGetsItsOwnWidth()
    {
        WfuiListView list = ShowAuto(40, new Size(0, 200), 100, 80, 60);

        Assert.Equal(200, list.Height);
        Assert.Equal(240 + SystemInformation.VerticalScrollBarWidth * list.DeviceDpi / 96, list.Width);
        // The columns have their whole width next to the bar: no horizontal scroll bar.
        Assert.True(list.ClientSize.Width >= 240);
    }

    [Fact]
    public void BelowTheMaximum_TheListHasNoRoomForABar()
    {
        WfuiListView list = ShowAuto(3, new Size(0, 400), 100, 80, 60);

        Assert.Equal(240, list.Width);
        Assert.True(list.Height < 400);
    }

    [Fact]
    public void ColumnsWiderThanTheMaximumWidth_GetAHorizontalBarsHeightOnTop()
    {
        WfuiListView list = ShowAuto(3, new Size(150, 0), 100, 80, 60);
        int wide = ShowAuto(3, Size.Empty, 100, 80, 60).Height;

        Assert.Equal(150, list.Width);
        Assert.Equal(wide + SystemInformation.HorizontalScrollBarHeight * list.DeviceDpi / 96, list.Height);
    }

    [Fact]
    public void AFixedRowCount_OverrulesAutoSizeToContent()
    {
        WfuiListView list = ShowAuto(3, new Size(0, 100), 100, 80, 60);
        int auto = list.Height;

        list.MaximumSize = Size.Empty;
        list.FixedRowCount = 10;
        Application.DoEvents();

        Assert.True(list.Height > auto);
        Assert.False(list.Scrollable);
    }

    [Fact]
    public void TheOrdinaryList_StaysAsItWas()
    {
        using WfuiListView list = new WfuiListView();

        Assert.Equal(0, list.FixedRowCount);
        Assert.True(list.Scrollable);
        Assert.True(list.AllowSelection);
        Assert.Equal(ColumnHeaderStyle.Clickable, list.HeaderStyle);

        list.Size = new Size(333, 111);
        Assert.Equal(333, list.Width);
    }

    [Fact]
    public void AColorSet_FramesTheDisplay_AsItDoesTheOthers()
    {
        using WfuiListView list = (WfuiListView)UIStyles.ListViews.CreateStandard(UIStyles.Colors.Sets.Purple);
        list.FixedRowCount = 3;

        Assert.Equal(UIStyles.Colors.Sets.Purple.Hover, list.BorderColor);
        Assert.Equal(3, list.FixedRowCount);
    }
}
