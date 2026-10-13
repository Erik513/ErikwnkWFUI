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

    [Fact]
    public void CreateStandard_IsTheNeutralGray_AndSizedToItsContent()
    {
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard();
        UIColorSet gray = UIStyles.Colors.Sets.Gray;

        Assert.Null(popup.Colors);
        Assert.Equal(gray.Basic, popup.BackColor);
        Assert.Equal(Color.White, Fore(popup));
        Assert.True(popup.FixedSize.IsEmpty);
    }

    [Fact]
    public void WithASet_TheBackgroundAndTheTextAreItsColors()
    {
        UIColorSet set = UIStyles.Colors.Sets.Green;
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard(set);

        Assert.Same(set, popup.Colors);
        Assert.Equal(set.Basic, popup.BackColor);
        Assert.Equal(Color.White, Fore(popup));
    }

    [Fact]
    public void TheTransparency_IsMediumByDefault_AndTheFourLevelsAreOrdered()
    {
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard();

        Assert.Equal(PopupTransparency.Medium, popup.Transparency);
        Assert.Equal(0.80, popup.Opacity, 2);

        popup.Transparency = PopupTransparency.Opaque;
        Assert.Equal(1.0, popup.Opacity, 2);
        double opaque = popup.Opacity;

        popup.Transparency = PopupTransparency.Low;
        double low = popup.Opacity;

        popup.Transparency = PopupTransparency.High;
        double high = popup.Opacity;

        Assert.True(opaque > low && low > 0.80 && 0.80 > high && high > 0.4, "the levels are not ordered");
    }

    [Fact]
    public void TheFactoryMethods_TakeTheTransparency()
    {
        using InfoPopupForm standard = UIStyles.InfoPopups.CreateStandard(transparency: PopupTransparency.Opaque);
        using InfoPopupForm primary = UIStyles.InfoPopups.CreatePrimary(transparency: PopupTransparency.High);
        using InfoPopupForm fixedSize = UIStyles.InfoPopups.CreatePrimary(new Size(70, 32), PopupTransparency.Low);

        Assert.Equal(PopupTransparency.Opaque, standard.Transparency);
        Assert.Equal(1.0, standard.Opacity, 2);
        Assert.Equal(PopupTransparency.High, primary.Transparency);
        Assert.Equal(PopupTransparency.Low, fixedSize.Transparency);
        Assert.Equal(new Size(70, 32), fixedSize.ClientSize);
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
    public void APrimaryPopup_FollowsAChangeOfTheAccent_AlsoAfterItWasMade()
    {
        Color accent = UIColors.Primary;

        try
        {
            using InfoPopupForm popup = UIStyles.InfoPopups.CreatePrimary();
            using InfoPopupForm gray = UIStyles.InfoPopups.CreateStandard();
            using InfoPopupForm green = UIStyles.InfoPopups.CreateStandard(UIStyles.Colors.Sets.Green);
            _ = popup.Handle;
            Color before = popup.BackColor;

            UIColors.SetAccent(UIAccentColors.Purple);

            Assert.Equal(UIColors.PrimarySet.Basic, popup.Colors!.Basic);
            Assert.NotEqual(before, popup.BackColor);
            Assert.Equal(popup.InvokePrivate<Color>("GetBackColor"), popup.BackColor);
            Assert.Equal(UIStyles.Colors.Sets.Gray.Basic, gray.BackColor);
            Assert.Equal(UIStyles.Colors.Sets.Green.Basic, green.BackColor);
        }
        finally
        {
            UIColors.SetAccent(accent);
        }
    }

    [Fact]
    public void APrimaryPopup_ThatHasNoWindowYet_HasTheCurrentAccent_WhenItIsPlaced()
    {
        Color accent = UIColors.Primary;

        try
        {
            using InfoPopupForm popup = UIStyles.InfoPopups.CreatePrimary();

            UIColors.SetAccent(UIAccentColors.Purple);

            // The color of a show is worked out when it is shown (Place), from the set as it is then.
            Assert.Equal(UIColors.PrimarySet.Basic, popup.Colors!.Basic);
            Assert.Equal(popup.InvokePrivate<Color>("GetBackColor"), PopupColor(popup));
        }
        finally
        {
            UIColors.SetAccent(accent);
        }
    }

    private static Color PopupColor(InfoPopupForm popup) => ErikwnkWFUI.Helpers.PopupSupport.GetBackground(popup.Colors!, popup.Opacity);

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
        Assert.Equal(Color.White, Fore(green));
    }

    [Fact]
    public void OnABackgroundTooLightForWhite_TheTextIsBlack_AndTheBackgroundStaysAsItIs()
    {
        UIColorSet yellow = UIStyles.Colors.Sets.Yellow;
        UIColorSet white = UIStyles.Colors.CreateSet(Color.White);
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard(yellow);
        using InfoPopupForm pale = UIStyles.InfoPopups.CreateStandard(white);

        Assert.Equal(yellow.Basic, popup.BackColor);
        Assert.Equal(Color.Black, Fore(popup));
        Assert.Equal(white.Basic, pale.BackColor);
        Assert.Equal(Color.Black, Fore(pale));
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

                double ratio = ColorContrastHelper.GetContrastRatio(Fore(popup), popup.BackColor);

                if (ratio < 4.5)
                    shortfalls.Add(property.Name + " " + ratio.ToString("0.0"));

                if (Fore(popup) != Color.White && Fore(popup) != Color.Black)
                    shortfalls.Add(property.Name + " is neither white nor black");
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
