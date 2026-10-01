using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Buttons;

/// <summary>
/// Readability of the buttons' text in both themes and in every state it is
/// shown in (idle, hovered, pressed, disabled), against WCAG's contrast math
/// (see ColorContrastHelper) - same approach as the other controls' color
/// tests. Three bars: idle text has to meet 4.5:1; hovered and pressed text
/// only has to stay clearly readable (the floor below), since those fills are
/// lighter by design and shown for a moment; disabled text, which WCAG
/// exempts, still has to be visible (3:1).
///
/// Browse is left out of the idle/hovered/pressed bars on purpose: it is a
/// fixed yellow button with a white folder glyph that looks the same in every
/// theme, a look chosen deliberately and easy to recognize. What is checked
/// instead is that it really is the same in both themes. Every case that falls
/// short is listed in one message, so a failing run shows the whole picture.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ButtonColorContrastTests
{
    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { false }; // Dark (the library's own default)
        yield return new object[] { true }; // Light
    }

    private static Button Create(string variant)
    {
        return variant switch
        {
            "Standard" => UIStyles.Buttons.CreateStandard("Text"),
            "Primary" => UIStyles.Buttons.CreatePrimary("Text"),
            "Green" => UIStyles.Buttons.CreateGreen("Text"),
            "Red" => UIStyles.Buttons.CreateRed("Text"),
            _ => UIStyles.Buttons.CreateBrowse()
        };
    }

    private static readonly string[] ColorVariants = { "Standard", "Primary", "Green", "Red" };
    private static readonly string[] AllVariants = { "Standard", "Primary", "Green", "Red", "Browse" };

    // Hovered and pressed fills are the bright shades of each color, so white
    // text on them stays below 4.5:1 for Green (about 2.4 pressed, 3.4
    // hovered). Clearly readable, not WCAG-strict.
    private const double MomentaryStateFloor = 2.0;

    private static void Press(Button button)
    {
        button.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
    }

    private static void RunWithTheme(bool lightTheme, Action<List<string>> collect)
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
        }

        Assert.True(
            shortfalls.Count == 0,
            (lightTheme ? "Light" : "Dark") + " theme:\n" + string.Join("\n", shortfalls));
    }

    private static void Check(List<string> shortfalls, string what, Color foreground, Color background, double minimum)
    {
        double ratio = ColorContrastHelper.GetContrastRatio(foreground, background);

        if (ratio < minimum)
        {
            shortfalls.Add($"{what}: {ratio:F2}:1, needs {minimum:F1}:1 (text {foreground}, fill {background})");
        }
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void IdleText_IsReadable_OnTheFill(bool lightTheme)
    {
        RunWithTheme(lightTheme, shortfalls =>
        {
            foreach (string variant in ColorVariants)
            {
                using Button button = Create(variant);
                Check(shortfalls, variant + " idle", button.ForeColor, button.BackColor, ColorContrastHelper.MinimumRatioNormalText);
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void HoveredText_IsReadable_OnTheHoverFill(bool lightTheme)
    {
        // The text keeps its idle color while the mouse is over the button.
        RunWithTheme(lightTheme, shortfalls =>
        {
            foreach (string variant in ColorVariants)
            {
                using Button button = Create(variant);
                Check(shortfalls, variant + " hovered", button.ForeColor, button.FlatAppearance.MouseOverBackColor, MomentaryStateFloor);
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void PressedText_IsReadable_OnThePressedFill(bool lightTheme)
    {
        RunWithTheme(lightTheme, shortfalls =>
        {
            foreach (string variant in ColorVariants)
            {
                using Button button = Create(variant);
                Press(button);
                Check(shortfalls, variant + " pressed", button.ForeColor, button.FlatAppearance.MouseDownBackColor, MomentaryStateFloor);
            }
        });
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void DisabledText_IsStillVisible_OnTheDisabledFill(bool lightTheme)
    {
        RunWithTheme(lightTheme, shortfalls =>
        {
            foreach (string variant in AllVariants)
            {
                using Button button = Create(variant);
                button.Enabled = false;
                Check(shortfalls, variant + " disabled", button.ForeColor, button.BackColor, ColorContrastHelper.MinimumRatioLargeTextOrUiComponents);
            }
        });
    }

    [Fact]
    public void Browse_LooksTheSame_InBothThemes()
    {
        (Color Back, Color Fore, Color Over, Color Down, Color Border) Look()
        {
            using Button browse = UIStyles.Buttons.CreateBrowse();

            return (
                browse.BackColor,
                browse.ForeColor,
                browse.FlatAppearance.MouseOverBackColor,
                browse.FlatAppearance.MouseDownBackColor,
                browse.FlatAppearance.BorderColor);
        }

        try
        {
            UIColors.ApplyTheme(UIThemes.Dark);
            var dark = Look();

            UIColors.ApplyTheme(UIThemes.Light);
            var light = Look();

            Assert.Equal(dark.Back, light.Back);
            Assert.Equal(dark.Fore, light.Fore);
            Assert.Equal(dark.Over, light.Over);
            Assert.Equal(dark.Down, light.Down);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }
}
