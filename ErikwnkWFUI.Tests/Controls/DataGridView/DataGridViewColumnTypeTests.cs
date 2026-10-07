using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// Columns that are not text: button, combo box and link columns would show the system's own
/// white buttons and dark-blue links on the dark rows, so they are put in the grid's colors when
/// the column is added. [Collection]: the colors read the live accent, and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class DataGridViewColumnTypeTests
{
    private static System.Windows.Forms.DataGridView Create(bool editable) =>
        editable ? UIStyles.DataGridViews.CreateStandard() : UIStyles.DataGridViews.CreateReadOnlyStandard();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AButtonColumn_IsFlatAndInTheGridsColors(bool editable)
    {
        using System.Windows.Forms.DataGridView grid = Create(editable);
        DataGridViewButtonColumn column = new DataGridViewButtonColumn();

        grid.Columns.Add(column);

        Assert.Equal(FlatStyle.Flat, column.FlatStyle);
        Assert.Equal(UIColors.BackgroundLighter, column.DefaultCellStyle.BackColor);
        Assert.Equal(UIColors.TextPrimary, column.DefaultCellStyle.ForeColor);
    }

    [Fact]
    public void AComboColumn_IsFlat_WithItsArrowOnTheCurrentCellOnly()
    {
        using System.Windows.Forms.DataGridView grid = Create(true);
        DataGridViewComboBoxColumn column = new DataGridViewComboBoxColumn();

        grid.Columns.Add(column);

        Assert.Equal(FlatStyle.Flat, column.FlatStyle);
        Assert.True(column.DisplayStyleForCurrentCellOnly);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ALinkColumn_ShowsItsLinks_ReadableOnTheRows_InBothThemes(bool lightTheme)
    {
        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);
            using System.Windows.Forms.DataGridView grid = Create(true);
            DataGridViewLinkColumn column = new DataGridViewLinkColumn();

            grid.Columns.Add(column);
            Color row = ((ErikwnkWFUI.Controls.ReadOnlyDataGridView)grid).RowBackColor;

            Assert.True(ColorContrastHelper.GetContrastRatio(column.LinkColor, row) >= 4.5, "link " + column.LinkColor);
            Assert.True(ColorContrastHelper.GetContrastRatio(column.VisitedLinkColor, row) >= 4.5, "visited " + column.VisitedLinkColor);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }

    [Fact]
    public void ACheckBoxAndATextColumn_AreLeftAsTheyAre_AndTheColorsCanBeChangedAfterwards()
    {
        using System.Windows.Forms.DataGridView grid = Create(true);
        DataGridViewCheckBoxColumn check = new DataGridViewCheckBoxColumn();
        DataGridViewButtonColumn button = new DataGridViewButtonColumn();

        grid.Columns.Add(check);
        grid.Columns.Add(button);

        Assert.Equal(FlatStyle.Standard, check.FlatStyle);

        button.DefaultCellStyle.BackColor = Color.Teal;
        Assert.Equal(Color.Teal, button.DefaultCellStyle.BackColor);
    }
}
