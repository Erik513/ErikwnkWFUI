using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;
using WfuiReadOnlyDataGridView = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// Readability of this control's own default colors, against WCAG's actual
/// contrast-ratio math (see ColorContrastHelper) rather than eyeballing a
/// screenshot - checked in both themes, since a color pair that reads fine
/// in Dark can fail once ApplyTheme(Light) swaps the base surfaces out from
/// under it. Each grid is constructed AFTER switching themes, not before -
/// this control's own colors are read from UIColors once, into fields, at
/// construction (see ReadOnlyDataGridView's own remarks on ApplyTheme) -
/// same [Collection]/try-finally pattern as UIAccentColorsTests, since
/// ApplyTheme mutates process-wide static state.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class DataGridViewColorContrastTests
{
    [Theory]
    [InlineData(false)] // Dark (the library's own default)
    [InlineData(true)] // Light
    public void RowText_OnRowBackground_MeetsNormalTextContrast(bool lightTheme)
    {
        RunWithTheme(lightTheme, () =>
        {
            using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();

            AssertMeetsContrast(grid.DefaultCellStyle.ForeColor, grid.DefaultCellStyle.BackColor,
                ColorContrastHelper.MinimumRatioNormalText, "row text on row background");
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RowText_OnAlternatingRowBackground_MeetsNormalTextContrast(bool lightTheme)
    {
        RunWithTheme(lightTheme, () =>
        {
            using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();

            AssertMeetsContrast(grid.AlternatingRowsDefaultCellStyle.ForeColor, grid.AlternatingRowsDefaultCellStyle.BackColor,
                ColorContrastHelper.MinimumRatioNormalText, "row text on the alternating row background");
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeaderText_OnHeaderBackground_MeetsLargeTextContrast(bool lightTheme)
    {
        // The header font is always bold (see ApplyStyles) - WCAG's lower,
        // "large text" bar (3:1) applies to bold text at typical UI sizes,
        // not just literally large text.
        RunWithTheme(lightTheme, () =>
        {
            using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();

            AssertMeetsContrast(grid.ColumnHeadersDefaultCellStyle.ForeColor, grid.ColumnHeadersDefaultCellStyle.BackColor,
                ColorContrastHelper.MinimumRatioLargeTextOrUiComponents, "header text on header background");
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectedRowText_OnDefaultSelectionBackground_MeetsNormalTextContrast(bool lightTheme)
    {
        RunWithTheme(lightTheme, () =>
        {
            using WfuiReadOnlyDataGridView grid = new WfuiReadOnlyDataGridView();

            AssertMeetsContrast(grid.DefaultCellStyle.SelectionForeColor, grid.DefaultCellStyle.SelectionBackColor,
                ColorContrastHelper.MinimumRatioNormalText, "selected-row text on the default (neutral gray) selection background");
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectedRowText_OnAccentSelectionBackground_MeetsNormalTextContrast(bool lightTheme)
    {
        // UIStyles.DataGridViews.CreatePrimary's own SelectionBackColor
        // (UIColors.Primary) instead of the neutral gray default above -
        // the accent color changes per theme too (SetAccent), not just the
        // base surfaces ApplyTheme covers, but the default accent already
        // differs enough between the two themes' base colors to be worth
        // checking on its own.
        RunWithTheme(lightTheme, () =>
        {
            using WfuiDataGridView grid = (WfuiDataGridView)UIStyles.DataGridViews.CreatePrimary();

            AssertMeetsContrast(grid.DefaultCellStyle.SelectionForeColor, grid.DefaultCellStyle.SelectionBackColor,
                ColorContrastHelper.MinimumRatioNormalText, "selected-row text on the accent (Primary) selection background");
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeleteGlyph_RestingColor_OnRowBackground_MeetsUiComponentContrast(bool lightTheme)
    {
        // A single "✕" glyph functioning as a button, not a sentence of
        // text - WCAG's non-text/UI-component bar (3:1) applies, not the
        // stricter normal-text one.
        RunWithTheme(lightTheme, () =>
        {
            // A real row, not the "type here to add a row" placeholder -
            // OnCellFormatting deliberately draws no glyph at all for the
            // placeholder (see its own remarks), which would leave
            // style.ForeColor untouched (Color.Empty) instead of the real
            // delete-glyph color this test means to check.
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
            grid.ShowDeleteRowColumn = true;
            DataGridViewCellStyle style = new DataGridViewCellStyle { BackColor = grid.DefaultCellStyle.BackColor };

            grid.InvokePrivate("OnCellFormatting", new DataGridViewCellFormattingEventArgs(
                grid.Columns["__deleteRow"]!.Index, 0, null, typeof(object), style));

            AssertMeetsContrast(style.ForeColor, style.BackColor,
                ColorContrastHelper.MinimumRatioLargeTextOrUiComponents, "the resting delete glyph on row background");
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeleteGlyph_HoveredColor_OnRowBackground_MeetsUiComponentContrast(bool lightTheme)
    {
        RunWithTheme(lightTheme, () =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using WfuiDataGridView grid = GridTestHelpers.CreateGrid(items);
            grid.ShowDeleteRowColumn = true;
            int deleteColumnIndex = grid.Columns["__deleteRow"]!.Index;
            grid.SetPrivateField("_hoveredDeleteRowIndex", 0);
            DataGridViewCellStyle style = new DataGridViewCellStyle { BackColor = grid.DefaultCellStyle.BackColor };

            grid.InvokePrivate("OnCellFormatting", new DataGridViewCellFormattingEventArgs(
                deleteColumnIndex, 0, null, typeof(object), style));

            AssertMeetsContrast(style.ForeColor, style.BackColor,
                ColorContrastHelper.MinimumRatioLargeTextOrUiComponents, "the hovered delete glyph on row background");
        });
    }

    private static void RunWithTheme(bool lightTheme, System.Action test)
    {
        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);
            test();
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }

    private static void AssertMeetsContrast(Color foreground, Color background, double minimumRatio, string description)
    {
        double ratio = ColorContrastHelper.GetContrastRatio(foreground, background);

        Assert.True(ratio >= minimumRatio,
            $"{description} has a contrast ratio of {ratio:F2}:1, below the required {minimumRatio:F1}:1 " +
            $"(foreground {foreground}, background {background}).");
    }
}
