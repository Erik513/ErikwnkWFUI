using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Buttons;

/// <summary>
/// A button made with a color set (<c>CreateStandard(colors: ...)</c>): the
/// fill and the text for every state come from the set, whichever color it
/// was made from. The sample colors are the hard cases for the calculation
/// (light, dark, mid). [Collection]: the sets read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ColoredButtonTests
{
    public static IEnumerable<object[]> SampleColors()
    {
        yield return new object[] { "Orange", 230, 120, 20 };
        yield return new object[] { "Yellow", 250, 220, 60 };
        yield return new object[] { "White", 250, 250, 250 };
        yield return new object[] { "Teal", 0, 128, 128 };
        yield return new object[] { "Purple", 110, 50, 160 };
        yield return new object[] { "Black", 15, 15, 15 };
    }

    private static Button Create(Color color, string text = "Text")
    {
        return UIStyles.Buttons.CreateStandard(text, colors: UIStyles.Colors.CreateSet(color));
    }

    private static void Raise(Button button, string handler, params object[] args)
    {
        button.InvokePrivate(handler, args);
    }

    private static void Enter(Button button) => Raise(button, "OnMouseEnter", EventArgs.Empty);

    private static void Leave(Button button) => Raise(button, "OnMouseLeave", EventArgs.Empty);

    private static void Press(Button button) => Raise(button, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));

    private static void Release(Button button) => Raise(button, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));

    [Theory]
    [MemberData(nameof(SampleColors))]
    public void FillsAndTexts_ComeFromTheSet_ForEveryState(string name, int r, int g, int b)
    {
        Color color = Color.FromArgb(r, g, b);
        UIColorSet set = UIStyles.Colors.CreateSet(color);
        using Button button = Create(color);

        Assert.True(set.Basic == button.BackColor, name + ": resting fill");
        Assert.Equal(set.BasicText, button.ForeColor);
        Assert.Equal(set.Hover, button.FlatAppearance.MouseOverBackColor);
        Assert.Equal(set.Pressed, button.FlatAppearance.MouseDownBackColor);

        Enter(button);
        Assert.Equal(set.HoverText, button.ForeColor);

        Press(button);
        Assert.Equal(set.PressedText, button.ForeColor);

        Release(button);
        Assert.Equal(set.HoverText, button.ForeColor);

        Leave(button);
        Assert.Equal(set.BasicText, button.ForeColor);

        button.Enabled = false;
        Assert.Equal(set.Disabled, button.BackColor);
        Assert.Equal(set.DisabledText, button.ForeColor);

        button.Enabled = true;
        Assert.Equal(set.Basic, button.BackColor);
        Assert.Equal(set.BasicText, button.ForeColor);
    }

    [Fact]
    public void TheTextFlips_WhenAStateNeedsTheOtherColor()
    {
        // Light text at rest and hovered, dark on a pressed fill so bright that light
        // text would be hard to read (see UIColorSetTests).
        UIColorSet set = UIStyles.Colors.CreateSet(Color.FromArgb(20, 100, 50), Color.FromArgb(30, 150, 70), Color.FromArgb(80, 220, 120));
        using Button button = UIStyles.Buttons.CreateStandard("Go", colors: set);

        Assert.Equal(UIColors.LightForeColor, button.ForeColor);
        Enter(button);
        Assert.Equal(UIColors.LightForeColor, button.ForeColor);
        Press(button);
        Assert.Equal(UIColors.DarkForeColor, button.ForeColor);
    }

    [Fact]
    public void TheGreenButton_KeepsItsWhiteText_WhilePressed()
    {
        // Pressing is a moment and white on the bright green is still readable.
        using Button button = UIStyles.Buttons.CreateGreen("Go");

        Press(button);

        Assert.Equal(UIColors.LightForeColor, button.ForeColor);
    }

    [Fact]
    public void HoverText_IsTheSetsOwn_WhenTheHoverFillNeedsTheOtherColor()
    {
        // A dark fill at rest with light text, a pale hover fill that needs dark text.
        UIColorSet set = UIStyles.Colors.CreateSet(
            Color.FromArgb(20, 20, 80),
            Color.FromArgb(250, 250, 200),
            Color.FromArgb(200, 200, 150));
        using Button button = UIStyles.Buttons.CreateStandard("Go", colors: set);

        Assert.Equal(UIColors.LightForeColor, button.ForeColor);

        Enter(button);
        Assert.Equal(UIColors.DarkForeColor, button.ForeColor);

        Press(button);
        Assert.Equal(UIColors.DarkForeColor, button.ForeColor);

        Leave(button);
        Assert.Equal(UIColors.LightForeColor, button.ForeColor);
    }

    [Fact]
    public void TheDisabledLook_IsTheSetsOwn_NotWhatAPlainButtonWouldWorkOut()
    {
        // A mid-gray fill whose disabled shade sits right where a rule of
        // thumb says "light text" but the true contrast says dark text.
        UIColorSet set = UIStyles.Colors.CreateSet(Color.FromArgb(150, 150, 150));
        using Button button = UIStyles.Buttons.CreateStandard("Go", colors: set);

        button.Enabled = false;

        Assert.Equal(set.Disabled, button.BackColor);
        Assert.Equal(set.DisabledText, button.ForeColor);
        Assert.True(
            ColorContrastHelper.GetContrastRatio(button.ForeColor, button.BackColor) >= 3.0,
            "disabled text is hard to read on a mid-gray");
    }

    [Fact]
    public void ATextColorSetByTheCaller_ComesBackAtRest()
    {
        using Button button = UIStyles.Buttons.CreateStandard("Go", colors: UIStyles.Colors.Sets.Teal);
        button.ForeColor = Color.Orange;

        Enter(button);
        Press(button);
        Release(button);
        Leave(button);

        Assert.Equal(Color.Orange, button.ForeColor);
    }

    [Fact]
    public void DisablingWhileHoveredOrPressed_GivesTheDisabledLook_AndTheRestLookBack()
    {
        UIColorSet set = UIStyles.Colors.Sets.Purple;
        using Button button = UIStyles.Buttons.CreateStandard("Go", colors: set);

        Enter(button);
        Press(button);
        button.Enabled = false;
        Release(button);

        Assert.Equal(set.DisabledText, button.ForeColor);

        button.Enabled = true;

        Assert.Equal(set.BasicText, button.ForeColor);
        Assert.Equal(set.Basic, button.BackColor);
    }

    [Fact]
    public void ADisabledButton_DoesNotReactToTheMouse()
    {
        UIColorSet set = UIStyles.Colors.Sets.Teal;
        using Button button = UIStyles.Buttons.CreateStandard("Go", colors: set);
        button.Enabled = false;

        Enter(button);
        Press(button);

        Assert.Equal(set.DisabledText, button.ForeColor);
    }

    [Fact]
    public void ASetButton_LooksTheSame_InBothThemes()
    {
        (Color, Color, Color, Color, Color, Color, Color, Color) Look()
        {
            using Button button = Create(Color.FromArgb(230, 120, 20));
            Color back = button.BackColor;
            Color fore = button.ForeColor;
            Color over = button.FlatAppearance.MouseOverBackColor;
            Color down = button.FlatAppearance.MouseDownBackColor;
            Color border = button.FlatAppearance.BorderColor;
            Press(button);
            Color pressed = button.ForeColor;
            Release(button);
            button.Enabled = false;

            return (back, fore, over, down, border, pressed, button.BackColor, button.ForeColor);
        }

        try
        {
            UIColors.ApplyTheme(UIThemes.Dark);
            var dark = Look();

            UIColors.ApplyTheme(UIThemes.Light);
            var light = Look();

            Assert.Equal(dark, light);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }

    [Fact]
    public void AColor_CanBePassedDirectly()
    {
        Color color = Color.FromArgb(200, 60, 110);

        using Button direct = UIStyles.Buttons.CreateStandard("Go", colors: color);
        using Button viaSet = Create(color, "Go");

        Assert.Equal(viaSet.BackColor, direct.BackColor);
        Assert.Equal(viaSet.ForeColor, direct.ForeColor);
        Assert.Equal(viaSet.FlatAppearance.MouseDownBackColor, direct.FlatAppearance.MouseDownBackColor);
    }

    [Fact]
    public void WithoutASet_TheButtonIsStillTheNeutralOne()
    {
        using Button plain = UIStyles.Buttons.CreateStandard("Go");

        Assert.Equal(UIColors.BackgroundMedium, plain.BackColor);
        Assert.Equal(UIColors.TextPrimary, plain.ForeColor);

        // Hovering changes nothing about the text of a neutral button.
        Enter(plain);
        Assert.Equal(UIColors.TextPrimary, plain.ForeColor);
    }

    [Fact]
    public void TheSetButtons_AreReadable_OnEveryStateInBothThemes()
    {
        try
        {
            foreach (bool light in new[] { false, true })
            {
                UIColors.ApplyTheme(light ? UIThemes.Light : UIThemes.Dark);

                foreach (object[] sample in SampleColors())
                {
                    Color color = Color.FromArgb((int)sample[1], (int)sample[2], (int)sample[3]);
                    using Button button = Create(color);

                    Assert.True(
                        ColorContrastHelper.GetContrastRatio(button.ForeColor, button.BackColor) >= 4.0,
                        $"{sample[0]} text at rest ({(light ? "Light" : "Dark")})");

                    Enter(button);
                    Assert.True(
                        ColorContrastHelper.GetContrastRatio(button.ForeColor, button.FlatAppearance.MouseOverBackColor) >= 3.0,
                        $"{sample[0]} text hovered ({(light ? "Light" : "Dark")})");

                    Press(button);
                    Assert.True(
                        ColorContrastHelper.GetContrastRatio(button.ForeColor, button.FlatAppearance.MouseDownBackColor) >= 2.0,
                        $"{sample[0]} text pressed ({(light ? "Light" : "Dark")})");
                }
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }
}
