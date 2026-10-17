using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// The disabled text of a theme: seen against every surface of the theme (WCAG does not ask for more than that
/// for disabled text, but it must not vanish), and weaker than the muted text, which it must not be mistaken for.
/// </summary>
public class ThemeDisabledTextTests
{
    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { "Dark", UIThemes.Dark };
        yield return new object[] { "Light", UIThemes.Light };
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void DisabledText_IsSeenOnTheSurfacesOfTheTheme(string name, UIColorTheme theme)
    {
        foreach (var surface in new[] { theme.BackgroundDark, theme.BackgroundDarkElevated, theme.BackgroundMedium, theme.BackgroundMediumElevated, theme.BackgroundLight })
            Assert.True(ColorContrastHelper.MeetsMinimumContrast(theme.TextDisabled, surface, 2.4), $"{name}: {surface} gives {ColorContrastHelper.GetContrastRatio(theme.TextDisabled, surface):F2}");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void DisabledText_IsWeakerThanTheMutedText(string name, UIColorTheme theme)
    {
        double disabled = ColorContrastHelper.GetContrastRatio(theme.TextDisabled, theme.BackgroundMedium);
        double muted = ColorContrastHelper.GetContrastRatio(theme.TextMuted, theme.BackgroundMedium);

        Assert.True(disabled < muted, $"{name}: disabled {disabled:F2}, muted {muted:F2}");
    }
}
