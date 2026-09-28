using System;
using System.Drawing;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListView = ErikwnkWFUI.Controls.ListView;

namespace ErikwnkWFUI.Tests.Controls.ListView;

/// <summary>
/// Readability of this control's own default colors, in both themes - same
/// approach and same shared ColorContrastHelper as DataGridViewColorContrastTests,
/// see its own remarks for why each grid/list is constructed AFTER
/// switching themes, not before. SelectionOverlayColor is genuinely
/// different from DataGridView's own SelectionBackColor though - it's
/// painted ON TOP of a row's own background rather than replacing it (see
/// its own doc comment), so checking contrast against it needs
/// ColorContrastHelper.Composite to first blend the two the same way
/// OnDrawItem's own SolidBrush actually paints it, not a straight contrast
/// check against either color alone.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ListViewColorContrastTests
{
    [Theory]
    [InlineData(false)] // Dark (the library's own default)
    [InlineData(true)] // Light
    public void ItemText_OnRowBackground_MeetsNormalTextContrast(bool lightTheme)
    {
        RunWithTheme(lightTheme, () =>
        {
            using WfuiListView listView = new WfuiListView();

            AssertMeetsContrast(listView.RowForeColor, listView.RowBackColor,
                ColorContrastHelper.MinimumRatioNormalText, "item text on row background");
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ItemText_OnAlternatingRowBackground_MeetsNormalTextContrast(bool lightTheme)
    {
        RunWithTheme(lightTheme, () =>
        {
            using WfuiListView listView = new WfuiListView();
            Color alternateRowBackColor = listView.GetPrivateField<Color>("_alternateRowBackColor");

            AssertMeetsContrast(listView.RowForeColor, alternateRowBackColor,
                ColorContrastHelper.MinimumRatioNormalText, "item text on the alternating row background");
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeaderText_OnHeaderBackground_MeetsLargeTextContrast(bool lightTheme)
    {
        // The header font is always bold (see HeaderFont) - WCAG's lower,
        // "large text" bar (3:1) applies to bold text at typical UI sizes,
        // not just literally large text.
        RunWithTheme(lightTheme, () =>
        {
            using WfuiListView listView = new WfuiListView();

            AssertMeetsContrast(listView.HeaderForeColor, listView.HeaderBackColor,
                ColorContrastHelper.MinimumRatioLargeTextOrUiComponents, "header text on header background");
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ItemText_OnDefaultSelectionOverlay_MeetsNormalTextContrast(bool lightTheme)
    {
        RunWithTheme(lightTheme, () =>
        {
            using WfuiListView listView = new WfuiListView();
            Color selectedBackground = ColorContrastHelper.Composite(listView.SelectionOverlayColor, listView.RowBackColor);

            AssertMeetsContrast(listView.RowForeColor, selectedBackground,
                ColorContrastHelper.MinimumRatioNormalText, "item text on the default (translucent gray) selection overlay");
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ItemText_OnAccentSelectionOverlay_MeetsNormalTextContrast(bool lightTheme)
    {
        // UIListViewFactory.CreatePrimary's own SelectionOverlayColor
        // (UIColors.Selection) instead of the neutral gray default above.
        RunWithTheme(lightTheme, () =>
        {
            using WfuiListView listView = (WfuiListView)UIStyles.ListViews.CreatePrimary();
            Color selectedBackground = ColorContrastHelper.Composite(listView.SelectionOverlayColor, listView.RowBackColor);

            AssertMeetsContrast(listView.RowForeColor, selectedBackground,
                ColorContrastHelper.MinimumRatioNormalText, "item text on the accent (Primary) selection overlay");
        });
    }

    private static void RunWithTheme(bool lightTheme, Action test)
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
