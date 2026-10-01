using System.Drawing;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// Color sets: any color in, every state of a control out. The sample colors
/// cover the cases that are hard for such a calculation - light, dark, mid
/// and neutral colors. Text has to stay readable on every fill of every
/// set: at rest 4:1, while hovered or pressed and when disabled 3:1.
/// [Collection] (see AccentColorTestCollection) - Primary reads the live
/// accent, which UIColorsTests changes via SetAccent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class UIColorSetTests
{
    private static readonly (string Name, Color Color)[] Samples =
    {
        ("Orange", Color.FromArgb(230, 120, 20)),
        ("Yellow", Color.FromArgb(250, 220, 60)),
        ("White", Color.FromArgb(250, 250, 250)),
        ("Teal", Color.FromArgb(0, 128, 128)),
        ("Purple", Color.FromArgb(110, 50, 160)),
        ("Black", Color.FromArgb(15, 15, 15)),
        ("Pink", Color.FromArgb(255, 105, 180)),
        ("LimeGreen", Color.FromArgb(120, 200, 40)),
        ("SkyBlue", Color.FromArgb(100, 180, 240)),
        ("MidGray", Color.FromArgb(128, 128, 128)),
        ("Red", Color.FromArgb(220, 30, 30)),
        ("Navy", Color.FromArgb(10, 20, 100)),
    };

    private static (string Name, UIColorSet Set)[] Predefined() => new[]
    {
        ("Primary", UIStyles.Colors.Sets.Primary),
        ("Red", UIStyles.Colors.Sets.Red),
        ("Orange", UIStyles.Colors.Sets.Orange),
        ("Yellow", UIStyles.Colors.Sets.Yellow),
        ("Green", UIStyles.Colors.Sets.Green),
        ("Teal", UIStyles.Colors.Sets.Teal),
        ("Blue", UIStyles.Colors.Sets.Blue),
        ("Purple", UIStyles.Colors.Sets.Purple),
        ("Pink", UIStyles.Colors.Sets.Pink),
        ("Brown", UIStyles.Colors.Sets.Brown),
        ("Gray", UIStyles.Colors.Sets.Gray),
    };

    private static double Ratio(Color text, Color fill) => ColorContrastHelper.GetContrastRatio(text, fill);

    private static void AssertReadable(string name, UIColorSet set)
    {
        List<string> problems = new List<string>();

        void Check(string what, Color text, Color fill, double minimum)
        {
            double ratio = Ratio(text, fill);

            if (ratio < minimum)
                problems.Add($"{what} {ratio:F2}:1 < {minimum:F1}:1 (text {text}, fill {fill})");
        }

        Check("at rest", set.BasicText, set.Basic, 4.0);
        Check("hovered", set.HoverText, set.Hover, 3.0);
        Check("pressed", set.PressedText, set.Pressed, 3.0);
        Check("disabled", set.DisabledText, set.Disabled, 3.0);

        Assert.True(problems.Count == 0, name + ": " + string.Join("; ", problems));
    }

    [Fact]
    public void ForAnyColor_TheTextIsReadable_OnEveryState()
    {
        foreach ((string name, Color color) in Samples)
        {
            AssertReadable(name, UIStyles.Colors.CreateSet(color));
        }
    }

    [Fact]
    public void EveryReadyMadeSet_IsReadable_OnEveryState()
    {
        foreach ((string name, UIColorSet set) in Predefined())
        {
            AssertReadable(name, set);
        }
    }

    [Fact]
    public void AStateIsVisiblyDifferent_FromTheOneBefore()
    {
        foreach ((string name, Color color) in Samples)
        {
            UIColorSet set = UIStyles.Colors.CreateSet(color);

            Assert.True(
                Math.Abs(set.Hover.GetBrightness() - set.Basic.GetBrightness()) >= 0.04f,
                name + ": hover looks like rest");
            Assert.True(
                Math.Abs(set.Pressed.GetBrightness() - set.Hover.GetBrightness()) >= 0.04f,
                name + ": pressed looks like hover");
        }
    }

    [Fact]
    public void ADarkColor_GetsLighterWhenHoveredAndPressed_ALightOneGetsDarker()
    {
        UIColorSet dark = UIStyles.Colors.CreateSet(Color.FromArgb(110, 50, 160));

        Assert.True(dark.Basic.GetBrightness() < dark.Hover.GetBrightness());
        Assert.True(dark.Hover.GetBrightness() < dark.Pressed.GetBrightness());
        Assert.Equal(Color.FromArgb(110, 50, 160), dark.Hover);

        Color yellow = Color.FromArgb(250, 220, 60);
        UIColorSet light = UIStyles.Colors.CreateSet(yellow);

        Assert.Equal(yellow, light.Basic);
        Assert.True(light.Hover.GetBrightness() < light.Basic.GetBrightness());
        Assert.True(light.Pressed.GetBrightness() < light.Hover.GetBrightness());
    }

    [Fact]
    public void TheText_KeepsItsColor_UntilItWouldNotBeReadable()
    {
        // Teal: light text works on all three fills, so it never flips.
        UIColorSet teal = UIStyles.Colors.CreateSet(Color.FromArgb(0, 128, 128));
        Assert.Equal(teal.BasicText, teal.HoverText);

        // Green: light text is fine at rest and hovered but fails on the
        // bright pressed fill, where the dark one takes over.
        UIColorSet green = UIStyles.Colors.Sets.Green;
        Assert.Equal(UIColors.LightForeColor, green.BasicText);
        Assert.Equal(UIColors.LightForeColor, green.HoverText);
        Assert.Equal(UIColors.DarkForeColor, green.PressedText);
    }

    [Fact]
    public void ASet_LooksTheSame_InBothThemes()
    {
        try
        {
            foreach ((string name, Color color) in Samples)
            {
                UIColors.ApplyTheme(UIThemes.Dark);
                UIColorSet dark = UIStyles.Colors.CreateSet(color);

                UIColors.ApplyTheme(UIThemes.Light);
                UIColorSet light = UIStyles.Colors.CreateSet(color);

                Assert.True(SameLook(dark, light), name + " differs between the themes");
            }

            UIColors.ApplyTheme(UIThemes.Dark);
            UIColorSet orangeDark = UIStyles.Colors.Sets.Orange;
            UIColors.ApplyTheme(UIThemes.Light);

            Assert.True(SameLook(orangeDark, UIStyles.Colors.Sets.Orange));
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }

    private static bool SameLook(UIColorSet a, UIColorSet b)
    {
        return a.Basic == b.Basic && a.Hover == b.Hover && a.Pressed == b.Pressed && a.Disabled == b.Disabled
            && a.BasicText == b.BasicText && a.HoverText == b.HoverText && a.PressedText == b.PressedText
            && a.DisabledText == b.DisabledText;
    }

    [Fact]
    public void Primary_FollowsTheAccent_TheOthersDoNot()
    {
        UIColorSet orangeBefore = UIStyles.Colors.Sets.Orange;

        try
        {
            UIColors.SetAccent(UIAccentColors.Purple);

            UIColorSet primary = UIStyles.Colors.Sets.Primary;

            Assert.Equal(UIColors.PrimaryDark, primary.Basic);
            Assert.Equal(UIColors.Primary, primary.Hover);
            Assert.Equal(UIColors.PrimaryLight, primary.Pressed);
            Assert.Equal(UIAccentColors.Purple, primary.Hover);
            Assert.True(SameLook(orangeBefore, UIStyles.Colors.Sets.Orange));
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Fact]
    public void RedGreenAndYellow_AreTheExistingColorFamilies()
    {
        Assert.Equal(UIColors.RedDark, UIStyles.Colors.Sets.Red.Basic);
        Assert.Equal(UIColors.Red, UIStyles.Colors.Sets.Red.Hover);
        Assert.Equal(UIColors.RedLight, UIStyles.Colors.Sets.Red.Pressed);
        Assert.Equal(UIColors.GreenDark, UIStyles.Colors.Sets.Green.Basic);
        Assert.Equal(UIColors.GreenLight, UIStyles.Colors.Sets.Green.Pressed);
        Assert.Equal(UIColors.YellowDark, UIStyles.Colors.Sets.Yellow.Basic);
        Assert.Equal(UIColors.YellowLight, UIStyles.Colors.Sets.Yellow.Pressed);
    }

    [Fact]
    public void YourOwnFills_AreKept_AndTheRestIsWorkedOut()
    {
        Color basic = Color.FromArgb(40, 40, 120);
        Color hover = Color.FromArgb(70, 70, 170);
        Color pressed = Color.FromArgb(230, 230, 255);

        UIColorSet set = UIStyles.Colors.CreateSet(basic, hover, pressed);

        Assert.Equal(basic, set.Basic);
        Assert.Equal(hover, set.Hover);
        Assert.Equal(pressed, set.Pressed);
        Assert.Equal(UIColors.LightForeColor, set.BasicText);
        Assert.Equal(UIColors.DarkForeColor, set.PressedText);
        AssertReadable("custom", set);
    }

    [Fact]
    public void AColor_CanBePassedWhereASetIsExpected()
    {
        Color color = Color.FromArgb(200, 60, 110);

        UIColorSet converted = color;

        Assert.True(SameLook(UIStyles.Colors.CreateSet(color), converted));
    }

    [Fact]
    public void ShiftLightness_KeepsTheHue_AndStaysInRange()
    {
        Color blue = Color.FromArgb(0, 90, 158);

        Color lighter = UIColors.ShiftLightness(blue, 0.12);
        Color darker = UIColors.ShiftLightness(blue, -0.12);

        Assert.True(lighter.GetBrightness() > blue.GetBrightness());
        Assert.True(darker.GetBrightness() < blue.GetBrightness());
        Assert.InRange(Math.Abs(lighter.GetHue() - blue.GetHue()), 0, 3);
        Assert.InRange(Math.Abs(darker.GetHue() - blue.GetHue()), 0, 3);

        Assert.Equal(Color.FromArgb(255, 255, 255), UIColors.ShiftLightness(blue, 5));
        Assert.Equal(Color.FromArgb(0, 0, 0), UIColors.ShiftLightness(blue, -5));
        Assert.Equal(Color.FromArgb(151, 151, 151), UIColors.ShiftLightness(Color.FromArgb(128, 128, 128), 0.09));
    }
}
