using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;
using WfuiGrid = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// Tab controls, list views, grids, menus and tooltips follow a change of the theme or the accent while they are
/// shown: afterwards they look as one made after the change. What was set by hand stays.
/// [Collection]: the theme and the accent are switched.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ThemeFollowCompositeControlsTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();
    private readonly Color _accentAtStart = UIColors.Primary;

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(_accentAtStart);
    }

    private T Show<T>(T control) where T : Control
    {
        control.Bounds = new Rectangle(5, 5, 240, 120);
        control.TabStop = false;
        Form form = OffscreenForm.Create(new Size(260, 140), Color.FromArgb(40, 90, 140));
        form.Controls.Add(control);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return control;
    }

    private static string Hash(Control control)
    {
        using Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
        long sum = 0;

        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                sum = sum * 31 + bitmap.GetPixel(x, y).ToArgb();

        return sum.ToString();
    }

    private static Control Fill(Control control)
    {
        if (control is ErikwnkWFUI.Controls.TabControl tabs)
        {
            tabs.TabPages.Add(new TabPage("One"));
            tabs.TabPages.Add(new TabPage("Two"));
        }
        else if (control is ReadOnlyTabControl readOnlyTabs)
        {
            readOnlyTabs.TabPages.Add(new TabPage("One"));
            readOnlyTabs.TabPages.Add(new TabPage("Two"));
        }
        else if (control is System.Windows.Forms.ListView list)
        {
            list.Columns.Add("A", 80);
            list.Columns.Add("B", 80);
            list.Items.Add(new ListViewItem(new[] { "1", "2" }));
            list.Items.Add(new ListViewItem(new[] { "3", "4" }));
        }
        else if (control is System.Windows.Forms.DataGridView grid)
        {
            grid.Columns.Add("A", "A");
            grid.Columns.Add("B", "B");
            grid.Rows.Add("1", "2");
            grid.Rows.Add("3", "4");
        }

        return control;
    }

    // The colors the control exposes, and the picture it paints.
    private static string Look(Control control)
    {
        string look = Hash(control);

        if (control is ErikwnkWFUI.Controls.TabControl tabs)
            look += $" {tabs.PageBackColor.ToArgb()} {tabs.SelectedTabIndicatorColor.ToArgb()} {tabs.ContextMenuSelectionColor.ToArgb()} {tabs.TabPages[0].BackColor.ToArgb()} {tabs.ForeColor.ToArgb()}";
        else if (control is ReadOnlyTabControl readOnly)
            look += $" {readOnly.PageBackColor.ToArgb()} {readOnly.SelectedTabIndicatorColor.ToArgb()} {readOnly.TabPages[0].BackColor.ToArgb()} {readOnly.ForeColor.ToArgb()}";
        else if (control is WfuiListView list)
            look += $" {list.BackColor.ToArgb()} {list.RowBackColor.ToArgb()} {list.RowForeColor.ToArgb()} {list.HeaderBackColor.ToArgb()} {list.HeaderForeColor.ToArgb()} {list.BorderColor.ToArgb()} {list.SelectionOverlayColor.ToArgb()} {list.ColumnReorderIndicatorColor.ToArgb()} {list.ContextMenuSelectionColor.ToArgb()} {((ErikwnkWFUI.Controls.ContextMenuStrip)list.ContextMenuStrip!).SelectionBackColor.ToArgb()}";
        else if (control is WfuiGrid grid)
            look += $" {grid.BackgroundColor.ToArgb()} {grid.GridColor.ToArgb()} {grid.RowBackColor.ToArgb()} {grid.RowForeColor.ToArgb()} {grid.HeaderBackColor.ToArgb()} {grid.HeaderForeColor.ToArgb()} {grid.BorderColor.ToArgb()} {grid.SelectionBackColor.ToArgb()} {grid.ColumnReorderIndicatorColor.ToArgb()} {grid.ContextMenuSelectionColor.ToArgb()}"
                + $" {grid.DefaultCellStyle.BackColor.ToArgb()} {grid.AlternatingRowsDefaultCellStyle.BackColor.ToArgb()} {grid.DefaultCellStyle.ForeColor.ToArgb()} {grid.ColumnHeadersDefaultCellStyle.BackColor.ToArgb()} {grid.ColumnHeadersDefaultCellStyle.ForeColor.ToArgb()} {grid.DefaultCellStyle.SelectionBackColor.ToArgb()} {grid.DefaultCellStyle.SelectionForeColor.ToArgb()}";

        return look;
    }

    public static IEnumerable<object[]> Controls()
    {
        foreach (var entry in new (string, Func<Control>)[]
        {
            ("TabControls.Standard", () => Fill(UIStyles.TabControls.CreateStandard())),
            ("TabControls.Primary", () => Fill(UIStyles.TabControls.CreatePrimary())),
            ("TabControls.ReadOnlyStandard", () => Fill(UIStyles.TabControls.CreateReadOnlyStandard())),
            ("TabControls.ReadOnlyPrimary", () => Fill(UIStyles.TabControls.CreateReadOnlyPrimary())),
            ("TabControls.Green", () => Fill(UIStyles.TabControls.CreateStandard(UIStyles.Colors.Sets.Green))),
            ("ListViews.Standard", () => Fill(UIStyles.ListViews.CreateStandard())),
            ("ListViews.Primary", () => Fill(UIStyles.ListViews.CreatePrimary())),
            ("ListViews.Green", () => Fill(UIStyles.ListViews.CreateStandard(UIStyles.Colors.Sets.Green))),
            ("DataGridViews.Standard", () => Fill(UIStyles.DataGridViews.CreateStandard())),
            ("DataGridViews.Primary", () => Fill(UIStyles.DataGridViews.CreatePrimary())),
            ("DataGridViews.ReadOnlyStandard", () => Fill(UIStyles.DataGridViews.CreateReadOnlyStandard())),
            ("DataGridViews.ReadOnlyPrimary", () => Fill(UIStyles.DataGridViews.CreateReadOnlyPrimary())),
            ("DataGridViews.Green", () => Fill(UIStyles.DataGridViews.CreateReadOnlyStandard(null, UIStyles.Colors.Sets.Green))),
        })
            yield return new object[] { entry.Item1, entry.Item2 };
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void AShownControl_LooksAsOneMadeAfterTheChange_InBothDirections(string name, Func<Control> make)
    {
        Control shown = Show(make());
        string dark = Look(shown);

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();
        Control madeInLight = Show(make());

        Assert.NotEqual(dark, Look(shown));
        Assert.Equal(Look(madeInLight), Look(shown));

        UIColors.ApplyTheme(UIThemes.Dark);
        Application.DoEvents();
        Control madeInDark = Show(make());

        Assert.Equal(Look(madeInDark), Look(shown));
        Assert.Equal(dark, Look(shown));
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void AShownControl_LooksAsOneMadeAfterAChangeOfTheAccent(string name, Func<Control> make)
    {
        Control shown = Show(make());

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Assert.Equal(Look(Show(make())), Look(shown));
    }

    [Fact]
    public void TheMenu_FollowsTheThemeAndTheAccent()
    {
        ErikwnkWFUI.Controls.ContextMenuStrip primary = UIStyles.ContextMenuStrips.CreatePrimary();
        ErikwnkWFUI.Controls.ContextMenuStrip standard = UIStyles.ContextMenuStrips.CreateStandard();
        Color before = primary.SelectionBackColor;

        UIColors.SetAccent(Color.OrangeRed);

        Assert.NotEqual(before, primary.SelectionBackColor);
        Assert.Equal(UIColors.GetReadableFill(UIColors.PrimarySet.Hover, UIColors.PrimarySet.HoverText), primary.SelectionBackColor);
        Assert.Equal(UIColors.BorderLight, standard.SelectionBackColor);
    }

    [Fact]
    public void ATooltip_FollowsTheTheme_AndStopsWhenDisposed()
    {
        ToolTip toolTip = ErikwnkWFUI.Factories.UIToolTipFactory.CreateToolTip();
        ToolTip byHand = ErikwnkWFUI.Factories.UIToolTipFactory.CreateToolTip();
        byHand.BackColor = Color.Red;
        Color darkBack = toolTip.BackColor;

        UIColors.ApplyTheme(UIThemes.Light);

        Assert.NotEqual(darkBack, toolTip.BackColor);
        Assert.Equal(UIColors.BackgroundMedium, toolTip.BackColor);
        Assert.Equal(UIColors.TextPrimary, toolTip.ForeColor);
        Assert.Equal(Color.Red, byHand.BackColor);

        toolTip.Dispose();
        byHand.Dispose();
        Color lightBack = UIColors.BackgroundMedium;
        UIColors.ApplyTheme(UIThemes.Dark);

        Assert.NotEqual(UIColors.BackgroundMedium, lightBack);
    }

    [Fact]
    public void WhatWasSetByHand_Stays()
    {
        WfuiListView list = (WfuiListView)Fill(Show(new WfuiListView()));
        list.RowBackColor = Color.Red;
        list.HeaderForeColor = Color.Yellow;
        list.BackColor = Color.Purple;
        WfuiGrid grid = (WfuiGrid)Fill(Show(new WfuiGrid()));
        grid.RowBackColor = Color.Red;
        grid.BackgroundColor = Color.Purple;
        ReadOnlyTabControl tabs = (ReadOnlyTabControl)Fill(Show(new ReadOnlyTabControl()));
        tabs.TabPages[1].BackColor = Color.Green;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(Color.Red, list.RowBackColor);
        Assert.Equal(Color.Yellow, list.HeaderForeColor);
        Assert.Equal(Color.Purple, list.BackColor);
        Assert.Equal(Color.Red, grid.RowBackColor);
        Assert.Equal(Color.Purple, grid.BackgroundColor);
        Assert.Equal(Color.Green, tabs.TabPages[1].BackColor);
    }
}
