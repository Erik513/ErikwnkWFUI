using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// DataGridView's own OnCellFormatting override - draws the delete column's
/// "X" glyph, skips it for the placeholder row, and carries the row's own
/// resting BackColor over to SelectionBackColor so pressing the glyph never
/// shows the grid's real (green) selection color. Invoked directly via
/// reflection (it's a protected override) with a hand-built
/// DataGridViewCellFormattingEventArgs, since there's no real paint pass to
/// drive here.
/// </summary>
public class DataGridViewDeleteColumnFormattingTests
{
    private static DataGridViewCellFormattingEventArgs FormatArgs(int columnIndex, int rowIndex, DataGridViewCellStyle style)
    {
        return new DataGridViewCellFormattingEventArgs(columnIndex, rowIndex, null, typeof(object), style);
    }

    [Fact]
    public void RealRow_SetsTheDeleteGlyphAndRedForeColor()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.ShowDeleteRowColumn = true;
        int deleteColumnIndex = grid.Columns["__deleteRow"]!.Index;
        DataGridViewCellStyle style = new DataGridViewCellStyle { BackColor = Color.White };

        DataGridViewCellFormattingEventArgs args = FormatArgs(deleteColumnIndex, 0, style);
        grid.InvokePrivate("OnCellFormatting", args);

        Assert.Equal("✕", args.Value);
        Assert.True(args.FormattingApplied);
        Assert.Equal(UIColors.Red, style.ForeColor);
        Assert.Equal(UIColors.Red, style.SelectionForeColor);
        Assert.Equal(Color.White, style.SelectionBackColor); // carried over, not the grid's real selection color
    }

    [Fact]
    public void PlaceholderRow_LeavesTheGlyphUnsetButStillNeutralizesSelectionColor()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.ShowDeleteRowColumn = true;
        int deleteColumnIndex = grid.Columns["__deleteRow"]!.Index;
        int placeholderIndex = grid.Rows.Count - 1;
        DataGridViewCellStyle style = new DataGridViewCellStyle { BackColor = Color.White };

        DataGridViewCellFormattingEventArgs args = FormatArgs(deleteColumnIndex, placeholderIndex, style);
        grid.InvokePrivate("OnCellFormatting", args);

        Assert.False(args.FormattingApplied);
        Assert.Null(args.Value);
        Assert.Equal(Color.White, style.SelectionBackColor);
    }

    [Fact]
    public void HoveredRow_UsesALighterRedForeColor()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.ShowDeleteRowColumn = true;
        int deleteColumnIndex = grid.Columns["__deleteRow"]!.Index;
        grid.SetPrivateField("_hoveredDeleteRowIndex", 1);
        DataGridViewCellStyle hoveredStyle = new DataGridViewCellStyle();
        DataGridViewCellStyle restingStyle = new DataGridViewCellStyle();

        grid.InvokePrivate("OnCellFormatting", FormatArgs(deleteColumnIndex, 1, hoveredStyle));
        grid.InvokePrivate("OnCellFormatting", FormatArgs(deleteColumnIndex, 0, restingStyle));

        Assert.Equal(UIColors.Lighten(UIColors.Red, 40), hoveredStyle.ForeColor);
        Assert.Equal(UIColors.Red, restingStyle.ForeColor);
    }

    [Fact]
    public void OtherColumn_DoesNothing()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        grid.ShowDeleteRowColumn = true;
        int nameColumnIndex = grid.Columns["Name"]!.Index;
        DataGridViewCellStyle style = new DataGridViewCellStyle { BackColor = Color.White };

        DataGridViewCellFormattingEventArgs args = FormatArgs(nameColumnIndex, 0, style);
        grid.InvokePrivate("OnCellFormatting", args);

        Assert.False(args.FormattingApplied);
        Assert.Null(args.Value);
        Assert.Equal(Color.Empty, style.SelectionBackColor); // untouched
    }

    [Fact]
    public void WithoutShowDeleteRowColumn_DoesNothingEvenForWhatWouldBeItsOwnColumnIndex()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
        DataGridViewCellStyle style = new DataGridViewCellStyle { BackColor = Color.White };

        DataGridViewCellFormattingEventArgs args = FormatArgs(0, 0, style);
        grid.InvokePrivate("OnCellFormatting", args);

        Assert.False(args.FormattingApplied);
        Assert.Equal(Color.Empty, style.SelectionBackColor);
    }
}
