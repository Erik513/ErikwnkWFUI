using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// The bold font of the column headers: the cell style keeps the reference it is given, so the font must stay alive as
/// long as the grid, be the same one while the font of the grid is the same, and follow a new font.
/// </summary>
public class DataGridViewHeaderFontTests
{
    public static IEnumerable<object[]> Grids()
    {
        yield return new object[] { "read-only", (Func<System.Windows.Forms.DataGridView>)(() => UIStyles.DataGridViews.CreateReadOnlyStandard()) };
        yield return new object[] { "editable", (Func<System.Windows.Forms.DataGridView>)(() => UIStyles.DataGridViews.CreateStandard()) };
    }

    [Theory]
    [MemberData(nameof(Grids))]
    public void TheHeaderFont_CanBeReadAndUsed_ItIsNotDisposed(string name, Func<System.Windows.Forms.DataGridView> make)
    {
        using System.Windows.Forms.DataGridView grid = make();
        grid.Columns.Add("A", "A");

        Font font = grid.ColumnHeadersDefaultCellStyle.Font;

        Assert.True(font.Bold);
        Assert.True(font.Height > 0);
        Assert.True(TextRenderer.MeasureText("Ab", font).Width > 0);
    }

    [Theory]
    [MemberData(nameof(Grids))]
    public void TheSameFontIsKept_WhileTheGridFontStays_AndColorsChange(string name, Func<System.Windows.Forms.DataGridView> make)
    {
        using System.Windows.Forms.DataGridView grid = make();
        Font first = grid.ColumnHeadersDefaultCellStyle.Font;

        ((ErikwnkWFUI.Controls.ReadOnlyDataGridView)grid).HeaderBackColor = Color.Red;
        ((ErikwnkWFUI.Controls.ReadOnlyDataGridView)grid).RowBackColor = Color.Blue;
        grid.Enabled = false;
        grid.Enabled = true;

        Assert.Same(first, grid.ColumnHeadersDefaultCellStyle.Font);
        Assert.True(first.Height > 0);
    }

    [Theory]
    [MemberData(nameof(Grids))]
    public void ANewGridFont_GivesANewBoldHeaderFont_OfThatSize(string name, Func<System.Windows.Forms.DataGridView> make)
    {
        using System.Windows.Forms.DataGridView grid = make();
        Font first = grid.ColumnHeadersDefaultCellStyle.Font;

        using Font big = new Font("Segoe UI", 18);
        grid.Font = big;
        Font second = grid.ColumnHeadersDefaultCellStyle.Font;

        Assert.NotSame(first, second);
        Assert.True(second.Bold);
        Assert.Equal(18f, second.Size);
        Assert.True(second.Height > 0);
    }

    [Theory]
    [MemberData(nameof(Grids))]
    public void TheHeaderFont_IsFreedWithTheGrid(string name, Func<System.Windows.Forms.DataGridView> make)
    {
        System.Windows.Forms.DataGridView grid = make();
        Font font = grid.ColumnHeadersDefaultCellStyle.Font;

        grid.Dispose();

        Assert.Throws<ArgumentException>(() => _ = font.Height);
    }

    [Fact]
    public void ThePreviousHeaderFont_IsFreed_WhenTheGridFontChanges()
    {
        using System.Windows.Forms.DataGridView grid = UIStyles.DataGridViews.CreateReadOnlyStandard();
        Font first = grid.ColumnHeadersDefaultCellStyle.Font;

        using Font other = new Font("Segoe UI", 12);
        grid.Font = other;

        Assert.Throws<ArgumentException>(() => _ = first.Height);
    }
}
