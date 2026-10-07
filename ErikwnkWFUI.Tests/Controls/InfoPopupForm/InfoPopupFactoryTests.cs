using System.Drawing;
using System.Reflection;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>
/// The look of the info popup through its factory: the neutral gray without a color set, the
/// set's basic color and text with one, the primary set for CreatePrimary, the fixed size.
/// [Collection]: the sets read the live accent, and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class InfoPopupFactoryTests
{
    private static Color Fore(InfoPopupForm popup) => popup.InvokePrivate<Color>("GetForeColor");

    private static Color Dim(InfoPopupForm popup) => popup.InvokePrivate<Color>("GetDimForeColor");

    [Fact]
    public void CreateStandard_IsTheNeutralGray_AndSizedToItsContent()
    {
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard();
        UIColorSet gray = UIStyles.Colors.Sets.Gray;

        Assert.Null(popup.Colors);
        Assert.Equal(gray.Basic, popup.BackColor);
        Assert.Equal(gray.BasicText, Fore(popup));
        Assert.True(popup.FixedSize.IsEmpty);
    }

    [Fact]
    public void WithASet_TheBackgroundAndTheTextAreItsColors()
    {
        UIColorSet set = UIStyles.Colors.Sets.Green;
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard(set);

        Assert.Same(set, popup.Colors);
        Assert.Equal(set.Basic, popup.BackColor);
        Assert.Equal(set.BasicText, Fore(popup));

        // The text under a header is the quieter one: moved toward the background, but still reading.
        Assert.NotEqual(set.BasicText, Dim(popup));
        Assert.True(ColorContrastHelper.GetContrastRatio(Dim(popup), popup.BackColor) >= 4.5);
    }

    [Fact]
    public void ASetChangedLater_IsFollowedAtOnce_AndTakenAwayAgain()
    {
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard();

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
            using InfoPopupForm blue = UIStyles.InfoPopups.CreatePrimary();
            Assert.Equal(UIColors.PrimarySet.Basic, blue.BackColor);

            UIColors.SetAccent(UIAccentColors.Purple);
            using InfoPopupForm purple = UIStyles.InfoPopups.CreatePrimary();

            Assert.Equal(UIColors.PrimarySet.Basic, purple.BackColor);
            Assert.NotEqual(blue.BackColor, purple.BackColor);
        }
        finally
        {
            UIColors.SetAccent(accent);
        }
    }

    [Fact]
    public void TheFixedSize_IsKept_AndTakesTheSameColors()
    {
        using InfoPopupForm standard = UIStyles.InfoPopups.CreateStandard(fixedSize: new Size(70, 32));
        using InfoPopupForm primary = UIStyles.InfoPopups.CreatePrimary(new Size(80, 30));
        using InfoPopupForm green = UIStyles.InfoPopups.CreateStandard(UIStyles.Colors.Sets.Green, new Size(70, 32));

        Assert.Equal(new Size(70, 32), standard.FixedSize);
        Assert.Equal(new Size(70, 32), standard.ClientSize);
        Assert.Equal(new Size(80, 30), primary.ClientSize);
        Assert.Equal(UIStyles.Colors.Sets.Gray.Basic, standard.BackColor);
        Assert.Equal(UIColors.PrimarySet.Basic, primary.BackColor);
        Assert.Equal(UIStyles.Colors.Sets.Green.Basic, green.BackColor);
        Assert.Equal(UIStyles.Colors.Sets.Green.BasicText, Fore(green));
    }

    [Fact]
    public void ASetWhoseTextDoesNotRead_GetsABackgroundThatCarriesIt()
    {
        UIColorSet yellow = UIStyles.Colors.Sets.Yellow;
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard(yellow);

        Assert.NotEqual(yellow.Basic, popup.BackColor);
        Assert.True(ColorContrastHelper.GetContrastRatio(yellow.BasicText, popup.BackColor) >= 4.5);
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

            using (InfoPopupForm dark = UIStyles.InfoPopups.CreateStandard(UIStyles.Colors.Sets.Orange))
                darkBack = dark.BackColor;

            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);

            foreach (PropertyInfo property in typeof(UIStyles.Colors.Sets).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard((UIColorSet)property.GetValue(null)!);

                double header = ColorContrastHelper.GetContrastRatio(Fore(popup), popup.BackColor);
                double text = ColorContrastHelper.GetContrastRatio(Dim(popup), popup.BackColor);

                if (header < 4.5)
                    shortfalls.Add(property.Name + " header " + header.ToString("0.0"));

                if (text < 4.5)
                    shortfalls.Add(property.Name + " text " + text.ToString("0.0"));
            }

            using (InfoPopupForm orange = UIStyles.InfoPopups.CreateStandard(UIStyles.Colors.Sets.Orange))
                Assert.Equal(darkBack, orange.BackColor);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Assert.True(shortfalls.Count == 0, (lightTheme ? "Light: " : "Dark: ") + string.Join(", ", shortfalls));
    }
}
