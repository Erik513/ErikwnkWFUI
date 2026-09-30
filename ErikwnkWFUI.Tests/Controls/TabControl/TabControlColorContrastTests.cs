using System;
using System.Drawing;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTabControl = ErikwnkWFUI.Controls.TabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// Readability of the tab control's default colors in both themes, against
/// WCAG's contrast math (see ColorContrastHelper) - same approach as
/// DataGridViewColorContrastTests and ListViewColorContrastTests. The
/// colors are ThemeColors and follow the theme live, but the control is still
/// created after the switch so each case reads exactly what it checks.
///
/// Three bars, by what the color is for: text that has to be read (4.5:1),
/// small graphics that carry information (the padlock and the selected tab's
/// indicator bar, 3:1), and colors that are allowed to be quiet - disabled
/// text, which WCAG exempts, and the outlines, which only accompany other
/// cues. Those only have to be visible at all.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TabControlColorContrastTests
{
    private const double VisibleAtAll = 1.3;
    private const double DisabledTextFloor = 1.5;

    public static System.Collections.Generic.IEnumerable<object[]> Themes()
    {
        yield return new object[] { false }; // Dark (the library's own default)
        yield return new object[] { true }; // Light
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TabText_MeetsNormalTextContrast_OnItsBackgrounds(bool lightTheme)
    {
        RunWithTheme(lightTheme, tabs =>
        {
            AssertMeets(tabs.TabForeColor, tabs.TabBackColor, ColorContrastHelper.MinimumRatioNormalText, "tab text on the tab background");
            AssertMeets(tabs.TabForeColor, tabs.HoverTabBackColor, ColorContrastHelper.MinimumRatioNormalText, "tab text on the hovered tab background");
            AssertMeets(tabs.SelectedTabForeColor, tabs.SelectedTabBackColor, ColorContrastHelper.MinimumRatioNormalText, "selected tab text on the selected tab background");
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void PageText_MeetsNormalTextContrast_OnThePageBackground(bool lightTheme)
    {
        // What a label on a page gets by default: the control's own
        // ForeColor on PageBackColor.
        RunWithTheme(lightTheme, tabs =>
        {
            AssertMeets(tabs.ForeColor, tabs.PageBackColor, ColorContrastHelper.MinimumRatioNormalText, "page text on the page background");
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheScrollArrowsHoverGlyph_MeetsNormalTextContrast(bool lightTheme)
    {
        // Arrows use the tab text colors: the resting glyph is the tab text
        // (checked above), the hovered and pressed ones switch to the
        // selected-tab text color on the hover / selected background.
        RunWithTheme(lightTheme, tabs =>
        {
            AssertMeets(tabs.SelectedTabForeColor, tabs.HoverTabBackColor, ColorContrastHelper.MinimumRatioNormalText, "hovered arrow glyph on the hover background");
            AssertMeets(tabs.SelectedTabForeColor, tabs.SelectedTabBackColor, ColorContrastHelper.MinimumRatioNormalText, "pressed arrow glyph on the pressed background");
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ThePadlock_MeetsUiComponentContrast_OnBothTabBackgrounds(bool lightTheme)
    {
        // Drawn in the tab's text color at alpha 190 (see DrawPadlock).
        RunWithTheme(lightTheme, tabs =>
        {
            Color onTab = ColorContrastHelper.Composite(Color.FromArgb(190, tabs.TabForeColor), tabs.TabBackColor);
            Color onSelected = ColorContrastHelper.Composite(Color.FromArgb(190, tabs.SelectedTabForeColor), tabs.SelectedTabBackColor);

            AssertMeets(onTab, tabs.TabBackColor, ColorContrastHelper.MinimumRatioLargeTextOrUiComponents, "padlock on a tab");
            AssertMeets(onSelected, tabs.SelectedTabBackColor, ColorContrastHelper.MinimumRatioLargeTextOrUiComponents, "padlock on the selected tab");
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void DisabledTabText_IsStillVisible(bool lightTheme)
    {
        // Disabled text is exempt from WCAG and deliberately follows the
        // theme's TextDisabled, like ListBox's - it only must not vanish.
        RunWithTheme(lightTheme, tabs =>
        {
            AssertMeets(tabs.DisabledTabForeColor, tabs.TabBackColor, DisabledTextFloor, "disabled tab text on the tab background");
            AssertMeets(tabs.DisabledTabForeColor, tabs.SelectedTabBackColor, DisabledTextFloor, "disabled tab text on the selected tab background");
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheIndicatorBar_MeetsUiComponentContrast_NeutralAndInTheAccent(bool lightTheme)
    {
        RunWithTheme(lightTheme, tabs =>
        {
            using WfuiTabControl accent = (WfuiTabControl)ErikwnkWFUI.UIStyles.TabControls.CreatePrimary();

            AssertMeets(tabs.SelectedTabIndicatorColor, tabs.SelectedTabBackColor, ColorContrastHelper.MinimumRatioLargeTextOrUiComponents, "neutral indicator bar on the selected tab");
            AssertMeets(accent.SelectedTabIndicatorColor, accent.SelectedTabBackColor, ColorContrastHelper.MinimumRatioLargeTextOrUiComponents, "accent indicator bar on the selected tab");
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheOutlines_AreVisibleAgainstWhatTheyTouch(bool lightTheme)
    {
        RunWithTheme(lightTheme, tabs =>
        {
            AssertMeets(tabs.BorderColor, tabs.TabBackColor, VisibleAtAll, "outline against a tab");
            AssertMeets(tabs.BorderColor, tabs.PageBackColor, VisibleAtAll, "outline against the page");
        });
    }

    private static void RunWithTheme(bool lightTheme, Action<WfuiTabControl> test)
    {
        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);
            using WfuiTabControl tabs = new WfuiTabControl();
            test(tabs);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }

    private static void AssertMeets(Color foreground, Color background, double minimumRatio, string description)
    {
        double ratio = ColorContrastHelper.GetContrastRatio(foreground, background);

        Assert.True(ratio >= minimumRatio,
            $"{description} has a contrast ratio of {ratio:F2}:1, below the required {minimumRatio:F1}:1 " +
            $"(foreground {foreground}, background {background}).");
    }
}
