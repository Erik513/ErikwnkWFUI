using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>
/// The look of the info popup through its factory: the neutral gray without a color set, the
/// set's basic color and text with one, the primary set for CreatePrimary, the fixed-size
/// compact form. [Collection]: the sets read the live accent, and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class InfoPopupFactoryTests
{
    private static Label TitleLabel(InfoPopupForm popup) => popup.GetPrivateField<Label>("_titleLabel")!;

    private static Label TextLabel(InfoPopupForm popup) => popup.GetPrivateField<Label>("_textLabel")!;

    [Fact]
    public void CreateStandard_IsTheNeutralGray_WithTheTitle()
    {
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard("Info");
        UIColorSet gray = UIStyles.Colors.Sets.Gray;

        Assert.Null(popup.Colors);
        Assert.Equal(gray.Basic, popup.BackColor);
        Assert.Equal(gray.BasicText, TitleLabel(popup).ForeColor);
        Assert.Equal("Info", TitleLabel(popup).Text);
        Assert.False(popup.Compact);
    }

    [Fact]
    public void WithASet_TheBackgroundAndTheTextAreItsColors()
    {
        UIColorSet set = UIStyles.Colors.Sets.Green;
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard("Info", set);

        Assert.Same(set, popup.Colors);
        Assert.Equal(set.Basic, popup.BackColor);
        Assert.Equal(set.BasicText, TitleLabel(popup).ForeColor);

        // The body text is the quieter one: the text color moved toward the background, but
        // not so far that it stops reading.
        Color body = TextLabel(popup).ForeColor;
        Assert.NotEqual(set.BasicText, body);
        Assert.True(ColorContrastHelper.GetContrastRatio(body, popup.BackColor) >= 4.5);
    }

    [Fact]
    public void ASetWhoseTextDoesNotRead_GetsABackgroundThatCarriesIt()
    {
        UIColorSet yellow = UIStyles.Colors.Sets.Yellow;
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard("Info", yellow);

        Assert.NotEqual(yellow.Basic, popup.BackColor);
        Assert.True(ColorContrastHelper.GetContrastRatio(yellow.BasicText, popup.BackColor) >= 4.5);
    }

    [Fact]
    public void ASetChangedLater_IsFollowedAtOnce_AndTakenAwayAgain()
    {
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard("Info");

        popup.Colors = UIStyles.Colors.Sets.Red;
        Assert.Equal(UIStyles.Colors.Sets.Red.Basic, popup.BackColor);

        popup.Colors = null;
        Assert.Equal(UIStyles.Colors.Sets.Gray.Basic, popup.BackColor);
    }

    [Fact]
    public void CreatePrimary_IsTheStandardWithThePrimarySet_OfTheAccentAtCreation()
    {
        Color accent = UIColors.Primary;

        try
        {
            using InfoPopupForm blue = UIStyles.InfoPopups.CreatePrimary("Info");
            Assert.Equal(UIColors.PrimarySet.Basic, blue.BackColor);

            UIColors.SetAccent(UIAccentColors.Purple);
            using InfoPopupForm purple = UIStyles.InfoPopups.CreatePrimary("Info");

            Assert.Equal(UIColors.PrimarySet.Basic, purple.BackColor);
            Assert.NotEqual(blue.BackColor, purple.BackColor);
        }
        finally
        {
            UIColors.SetAccent(accent);
        }
    }

    [Fact]
    public void TheCompactForms_AreFixedSize_AndTakeTheSameColors()
    {
        using InfoPopupForm standard = UIStyles.InfoPopups.CreateStandardCompact(new Size(70, 32));
        using InfoPopupForm primary = UIStyles.InfoPopups.CreatePrimaryCompact();
        using InfoPopupForm green = UIStyles.InfoPopups.CreateStandardCompact(colors: UIStyles.Colors.Sets.Green);

        Assert.True(standard.Compact);
        Assert.Equal(new Size(70, 32), standard.CompactSize);
        Assert.Equal(new Size(48, 24), primary.CompactSize);
        Assert.Equal(UIStyles.Colors.Sets.Gray.Basic, standard.BackColor);
        Assert.Equal(UIColors.PrimarySet.Basic, primary.BackColor);
        Assert.Equal(UIStyles.Colors.Sets.Green.Basic, green.BackColor);
        Assert.Equal(UIStyles.Colors.Sets.Green.BasicText, TextLabel(green).ForeColor);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASet_LooksTheSame_InBothThemes_AndItsTextReads(bool lightTheme)
    {
        List<string> shortfalls = new List<string>();

        try
        {
            UIColors.ApplyTheme(UIThemes.Dark);
            Color darkBack;

            using (InfoPopupForm dark = UIStyles.InfoPopups.CreateStandard("Info", UIStyles.Colors.Sets.Orange))
                darkBack = dark.BackColor;

            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);

            foreach (PropertyInfo property in typeof(UIStyles.Colors.Sets).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard("Info", (UIColorSet)property.GetValue(null)!);

                double title = ColorContrastHelper.GetContrastRatio(TitleLabel(popup).ForeColor, popup.BackColor);
                double body = ColorContrastHelper.GetContrastRatio(TextLabel(popup).ForeColor, popup.BackColor);

                if (title < 4.5)
                    shortfalls.Add(property.Name + " title " + title.ToString("0.0"));

                if (body < 4.5)
                    shortfalls.Add(property.Name + " text " + body.ToString("0.0"));
            }

            using (InfoPopupForm orange = UIStyles.InfoPopups.CreateStandard("Info", UIStyles.Colors.Sets.Orange))
                Assert.Equal(darkBack, orange.BackColor);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Assert.True(shortfalls.Count == 0, (lightTheme ? "Light: " : "Dark: ") + string.Join(", ", shortfalls));
    }
}
