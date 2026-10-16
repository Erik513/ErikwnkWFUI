using System.Drawing;
using System.Reflection;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// The frame the colour tests of the controls share: both themes as theory data, every color set (and the default gray), a run in
/// one theme that puts the theme and the accent back, and the check of one pair of colors that collects what falls short. Use it with
/// <c>using static</c>; the data of the theories with <c>[MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]</c>.
/// </summary>
internal static class ColorContrastTestSupport
{
    /// <summary>false (dark) and true (light), for a theory over both themes.</summary>
    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { false };
        yield return new object[] { true };
    }

    /// <summary>The default gray (no set) and every color set of the library; the dark set - the fill of the theme itself - only when asked for.</summary>
    public static IEnumerable<(string Name, UIColorSet? Set)> AllSets(bool includeDark = false)
    {
        yield return ("default gray", null);

        foreach (PropertyInfo property in typeof(UIStyles.Colors.Sets).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (!includeDark && property.Name == "Dark")
                continue;

            yield return (property.Name, (UIColorSet)property.GetValue(null)!);
        }
    }

    /// <summary>
    /// Runs <paramref name="collect"/> in the dark or the light theme, always puts the dark theme and the blue accent back, and fails with
    /// everything that fell short (what <see cref="Check"/> collected).
    /// </summary>
    public static void Run(bool lightTheme, Action<List<string>> collect)
    {
        List<string> shortfalls = new List<string>();

        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);
            collect(shortfalls);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
            UIColors.SetAccent(UIAccentColors.Blue);
        }

        Assert.True(shortfalls.Count == 0, (lightTheme ? "Light" : "Dark") + " theme:\n" + string.Join("\n", shortfalls));
    }

    /// <summary>Adds a line to <paramref name="shortfalls"/> when <paramref name="foreground"/> on <paramref name="background"/> has less than <paramref name="minimum"/>:1.</summary>
    public static void Check(List<string> shortfalls, string what, Color foreground, Color background, double minimum)
    {
        double ratio = ColorContrastHelper.GetContrastRatio(foreground, background);

        if (ratio < minimum)
            shortfalls.Add($"{what}: {ratio:F2}:1, needs {minimum:F1}:1 (front {foreground}, back {background})");
    }
}
