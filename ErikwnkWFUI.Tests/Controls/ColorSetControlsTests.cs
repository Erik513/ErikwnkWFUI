using System.Drawing;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;
using WfuiListView = ErikwnkWFUI.Controls.ListView;
using WfuiReadOnlyGrid = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests.Controls;

/// <summary>
/// The tables and lists made with a color set (<c>CreateStandard(colors: ...)</c>):
/// the set's color frames and highlights them in place of the neutral gray,
/// and Primary is just the primary set. The sample colors are the hard cases
/// for any such mapping (light, dark, mid). [Collection]: the sets read the
/// live accent, and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ColorSetControlsTests
{
    private static readonly Color[] Samples =
    {
        Color.FromArgb(230, 120, 20),
        Color.FromArgb(250, 220, 60),
        Color.FromArgb(0, 128, 128),
        Color.FromArgb(110, 50, 160),
        Color.FromArgb(15, 15, 15),
        Color.FromArgb(120, 200, 40),
        Color.FromArgb(128, 128, 128),
    };

    private static WfuiReadOnlyGrid AsGrid(System.Windows.Forms.DataGridView grid) => (WfuiReadOnlyGrid)grid;

    private static WfuiListView AsList(System.Windows.Forms.ListView list) => (WfuiListView)list;

    // ---- DataGridView ----

    [Fact]
    public void AGrid_WithASet_UsesItsColorForBorderSelectionDragLineAndMenu()
    {
        UIColorSet set = UIStyles.Colors.Sets.Teal;

        foreach (WfuiReadOnlyGrid grid in new[]
        {
            AsGrid(UIStyles.DataGridViews.CreateStandard(null, set)),
            AsGrid(UIStyles.DataGridViews.CreateReadOnlyStandard(null, set))
        })
        {
            using (grid)
            {
                Assert.Equal(set.Hover, grid.BorderColor);
                Assert.Equal(set.Hover, grid.SelectionBackColor);
                Assert.Equal(set.Hover, grid.ColumnReorderIndicatorColor);
                Assert.Equal(set.Hover, grid.ContextMenuSelectionColor);
            }
        }
    }

    [Fact]
    public void AGrid_WithoutASet_KeepsItsNeutralColors()
    {
        using WfuiDataGridView plain = new WfuiDataGridView();
        using WfuiReadOnlyGrid made = AsGrid(UIStyles.DataGridViews.CreateReadOnlyStandard());
        using WfuiReadOnlyGrid plainReadOnly = new WfuiReadOnlyGrid();

        Assert.Equal(plainReadOnly.BorderColor, made.BorderColor);
        Assert.Equal(plainReadOnly.SelectionBackColor, made.SelectionBackColor);
        Assert.Equal(plainReadOnly.ColumnReorderIndicatorColor, made.ColumnReorderIndicatorColor);
        Assert.Equal(plain.SelectionBackColor, AsGrid(UIStyles.DataGridViews.CreateStandard()).SelectionBackColor);
    }

    [Fact]
    public void GridPrimary_IsTheStandardWithThePrimarySet_AndFollowsTheAccent()
    {
        try
        {
            UIColors.SetAccent(UIAccentColors.Purple);

            using WfuiReadOnlyGrid primary = AsGrid(UIStyles.DataGridViews.CreatePrimary());
            using WfuiReadOnlyGrid viaSet = AsGrid(UIStyles.DataGridViews.CreateStandard(null, UIStyles.Colors.Sets.Primary));
            using WfuiReadOnlyGrid readOnlyPrimary = AsGrid(UIStyles.DataGridViews.CreateReadOnlyPrimary());

            Assert.Equal(UIAccentColors.Purple, primary.BorderColor);
            Assert.Equal(viaSet.BorderColor, primary.BorderColor);
            Assert.Equal(viaSet.SelectionBackColor, primary.SelectionBackColor);
            Assert.Equal(viaSet.ColumnReorderIndicatorColor, primary.ColumnReorderIndicatorColor);
            Assert.Equal(viaSet.ContextMenuSelectionColor, primary.ContextMenuSelectionColor);
            Assert.Equal(UIAccentColors.Purple, readOnlyPrimary.SelectionBackColor);
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Fact]
    public void ASelectedCell_HasReadableText_OnTheColorOfAnySet()
    {
        foreach (Color color in Samples)
        {
            using WfuiReadOnlyGrid grid = AsGrid(UIStyles.DataGridViews.CreateReadOnlyStandard(null, color));

            Assert.True(
                ColorContrastHelper.GetContrastRatio(
                    grid.DefaultCellStyle.SelectionForeColor,
                    grid.DefaultCellStyle.SelectionBackColor) >= ColorContrastHelper.MinimumRatioLargeTextOrUiComponents,
                $"selected text on {grid.DefaultCellStyle.SelectionBackColor} (from {color})");
        }
    }

    [Fact]
    public void AGridInASetColor_LooksTheSame_InBothThemes()
    {
        (Color, Color, Color, Color) Look()
        {
            using WfuiReadOnlyGrid grid = AsGrid(UIStyles.DataGridViews.CreateReadOnlyStandard(null, UIStyles.Colors.Sets.Orange));

            return (grid.BorderColor, grid.SelectionBackColor, grid.ColumnReorderIndicatorColor, grid.ContextMenuSelectionColor);
        }

        try
        {
            UIColors.ApplyTheme(UIThemes.Dark);
            var dark = Look();

            UIColors.ApplyTheme(UIThemes.Light);

            Assert.Equal(dark, Look());
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }

    // ---- ListView ----

    [Fact]
    public void AList_WithASet_UsesItsColorForBorderSelectionDragLineAndMenu()
    {
        UIColorSet set = UIStyles.Colors.Sets.Purple;
        using WfuiListView list = AsList(UIStyles.ListViews.CreateStandard(set));

        Assert.Equal(set.Hover, list.BorderColor);
        Assert.Equal(set.Hover, list.ColumnReorderIndicatorColor);
        Assert.Equal(set.Hover, list.ContextMenuSelectionColor);
        Assert.Equal(Color.FromArgb(60, set.Hover.R, set.Hover.G, set.Hover.B), list.SelectionOverlayColor);
    }

    [Fact]
    public void ANeutralSet_GetsAStrongerSelectionTint()
    {
        using WfuiListView list = AsList(UIStyles.ListViews.CreateStandard(UIStyles.Colors.Sets.Gray));

        Assert.Equal(130, list.SelectionOverlayColor.A);
    }

    [Fact]
    public void AList_WithoutASet_KeepsItsNeutralColors()
    {
        using WfuiListView plain = new WfuiListView();
        using WfuiListView made = AsList(UIStyles.ListViews.CreateStandard());

        Assert.Equal(plain.BorderColor, made.BorderColor);
        Assert.Equal(plain.SelectionOverlayColor, made.SelectionOverlayColor);
        Assert.Equal(plain.ColumnReorderIndicatorColor, made.ColumnReorderIndicatorColor);
        Assert.Equal(plain.ContextMenuSelectionColor, made.ContextMenuSelectionColor);
    }

    [Fact]
    public void ListPrimary_IsTheStandardWithThePrimarySet_AndFollowsTheAccent()
    {
        try
        {
            UIColors.SetAccent(UIAccentColors.Purple);

            using WfuiListView primary = AsList(UIStyles.ListViews.CreatePrimary());
            using WfuiListView viaSet = AsList(UIStyles.ListViews.CreateStandard(UIStyles.Colors.Sets.Primary));

            Assert.Equal(UIAccentColors.Purple, primary.BorderColor);
            Assert.Equal(UIColors.Selection, primary.SelectionOverlayColor);
            Assert.Equal(viaSet.BorderColor, primary.BorderColor);
            Assert.Equal(viaSet.SelectionOverlayColor, primary.SelectionOverlayColor);
            Assert.Equal(viaSet.ColumnReorderIndicatorColor, primary.ColumnReorderIndicatorColor);
            Assert.Equal(viaSet.ContextMenuSelectionColor, primary.ContextMenuSelectionColor);
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Fact]
    public void ASelectedRow_HasReadableText_UnderTheTintOfAnySet_InBothThemes()
    {
        try
        {
            foreach (bool light in new[] { false, true })
            {
                UIColors.ApplyTheme(light ? UIThemes.Light : UIThemes.Dark);

                foreach (Color color in Samples)
                {
                    using WfuiListView list = AsList(UIStyles.ListViews.CreateStandard(color));
                    Color tinted = ColorContrastHelper.Composite(list.SelectionOverlayColor, list.RowBackColor);

                    Assert.True(
                        ColorContrastHelper.GetContrastRatio(list.RowForeColor, tinted) >= ColorContrastHelper.MinimumRatioNormalText,
                        $"row text under the tint of {color} ({(light ? "Light" : "Dark")})");
                }
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }

    [Fact]
    public void AListInASetColor_LooksTheSame_InBothThemes()
    {
        (Color, Color, Color, Color) Look()
        {
            using WfuiListView list = AsList(UIStyles.ListViews.CreateStandard(UIStyles.Colors.Sets.Orange));

            return (list.BorderColor, list.SelectionOverlayColor, list.ColumnReorderIndicatorColor, list.ContextMenuSelectionColor);
        }

        try
        {
            UIColors.ApplyTheme(UIThemes.Dark);
            var dark = Look();

            UIColors.ApplyTheme(UIThemes.Light);

            Assert.Equal(dark, Look());
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }
}
