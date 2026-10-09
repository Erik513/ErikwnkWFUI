using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.LabelsTests;

/// <summary>
/// The factory API of the labels: CreateStandard (gray body text, with an optional color set),
/// CreatePrimary (the same in the accent), CreateTitle and CreateMuted as they were, and the old
/// name CreateNormal as an alias. [Collection]: the sets read the live accent, and the tests
/// switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class LabelFactoryTests
{
    // What the label would paint its text in, over the page it sits on.
    private static Color TextColor(Label label)
    {
        return label.InvokePrivate<Color>("GetTextColor");
    }

    private static Label OnPage(Label label, Color page)
    {
        Panel host = new Panel { BackColor = page, Size = new Size(200, 40) };
        host.Controls.Add(label);

        return label;
    }

    [Fact]
    public void CreateStandard_IsTheGrayBodyText_AsCreateNormalWas()
    {
        using Label label = UIStyles.Labels.CreateStandard("Body");

        Assert.Equal("Body", label.Text);
        Assert.Equal(UIFonts.Normal, label.Font);
        Assert.Equal(ContentAlignment.MiddleLeft, label.TextAlign);
        Assert.Equal(Color.Transparent, label.BackColor);
        Assert.True(label.AutoEllipsis);
        Assert.False(label.AutoSize);
        Assert.False(label.UseMnemonic);
        Assert.Equal(UIColors.TextSecondary, TextColor(label));
    }

    [Fact]
    public void WithoutAText_TheLabelIsEmpty_NotNull()
    {
        using Label standard = UIStyles.Labels.CreateStandard();
        using Label nullText = UIStyles.Labels.CreateStandard(null!);
        using Label title = UIStyles.Labels.CreateTitle(null!);
        using Label muted = UIStyles.Labels.CreateMuted(null!);

        Assert.Equal("", standard.Text);
        Assert.Equal("", nullText.Text);
        Assert.Equal("", title.Text);
        Assert.Equal("", muted.Text);
    }

    [Fact]
    public void TheTitle_IsBoldAndCentered_AndTheMutedLabelIsSmaller()
    {
        using Label title = UIStyles.Labels.CreateTitle("T");
        using Label muted = UIStyles.Labels.CreateMuted("M");

        Assert.Equal(UIFonts.Title, title.Font);
        Assert.Equal(ContentAlignment.MiddleCenter, title.TextAlign);
        Assert.Equal(UIColors.TextPrimary, TextColor(title));
        Assert.Equal(Color.Transparent, title.BackColor);

        Assert.Equal(UIFonts.Small, muted.Font);
        Assert.Equal(UIColors.TextMuted, TextColor(muted));
    }

    [Fact]
    public void EveryLabel_HasATransparentBackground_SoItShowsTheSurfaceItSitsOn()
    {
        foreach (Label label in new[]
        {
            UIStyles.Labels.CreateStandard("x"), UIStyles.Labels.CreatePrimary("x"),
            UIStyles.Labels.CreateTitle("x"), UIStyles.Labels.CreateMuted("x")
        })
        {
            using (label)
                Assert.Equal(Color.Transparent, label.BackColor);
        }
    }

    [Fact]
    public void ALabelWithoutASet_IgnoresThePageBehindIt()
    {
        using Label label = OnPage(UIStyles.Labels.CreateStandard("x"), Color.White);

        Assert.Equal(UIColors.TextSecondary, TextColor(label));
    }

    [Fact]
    public void WithASet_TheTextTakesItsMainColor_WhereItReads()
    {
        // The seed of a set from one color is its main color; on a dark page it needs no change.
        Color seed = Color.FromArgb(120, 200, 120);
        UIColorSet set = UIStyles.Colors.CreateSet(seed);
        using Label label = OnPage(UIStyles.Labels.CreateStandard("x", set), Color.FromArgb(30, 30, 30));

        Assert.Equal(set.Hover, TextColor(label));
    }

    [Fact]
    public void WithASet_TheTextIsMovedUntilItReads_OnADarkAndOnALightPage()
    {
        UIColorSet set = UIStyles.Colors.CreateSet(Color.FromArgb(128, 128, 128));

        foreach (Color page in new[] { Color.Black, Color.White, Color.FromArgb(60, 60, 60), Color.FromArgb(235, 235, 235) })
        {
            using Label label = OnPage(UIStyles.Labels.CreateStandard("x", set), page);

            Assert.True(
                ColorContrastHelper.GetContrastRatio(TextColor(label), page) >= 4.49,
                "unreadable on " + page + ": " + TextColor(label));
        }
    }

    [Fact]
    public void EverySet_ReadsOnTheDarkAndOnTheLightTheme()
    {
        List<string> shortfalls = new List<string>();

        try
        {
            foreach (UIColorTheme theme in new[] { UIThemes.Dark, UIThemes.Light })
            {
                UIColors.ApplyTheme(theme);

                foreach (PropertyInfo property in typeof(UIStyles.Colors.Sets).GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    using Label label = OnPage(UIStyles.Labels.CreateStandard("x", (UIColorSet)property.GetValue(null)!), UIColors.BackgroundMedium);
                    double ratio = ColorContrastHelper.GetContrastRatio(TextColor(label), UIColors.BackgroundMedium);

                    if (ratio < 4.49)
                        shortfalls.Add(property.Name + " on " + UIColors.BackgroundMedium + " " + ratio.ToString("0.0"));
                }
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Assert.True(shortfalls.Count == 0, string.Join(", ", shortfalls));
    }

    [Fact]
    public void ADisabledLabel_IsAlwaysTheGrayOfTheLibrary_WithOrWithoutASet()
    {
        using Label plain = UIStyles.Labels.CreateStandard("x");
        using Label colored = UIStyles.Labels.CreateStandard("x", UIStyles.Colors.Sets.Green);
        using Label primary = UIStyles.Labels.CreatePrimary("x");
        using Label title = UIStyles.Labels.CreateTitle("x");
        using Label muted = UIStyles.Labels.CreateMuted("x");

        foreach (Label label in new[] { plain, colored, primary, title, muted })
        {
            label.Enabled = false;

            Assert.Equal(UIColors.DisabledGray, TextColor(label));
        }
    }

    [Fact]
    public void CreatePrimary_IsTheStandardWithThePrimarySet_OfTheAccentAtCreation()
    {
        Color before = UIColors.Primary;

        try
        {
            foreach (Color accent in new[] { Color.FromArgb(200, 40, 40), Color.FromArgb(40, 160, 90) })
            {
                UIColors.SetAccent(accent);
                using Label primary = OnPage(UIStyles.Labels.CreatePrimary("x"), UIColors.BackgroundMedium);
                using Label expected = OnPage(UIStyles.Labels.CreateStandard("x", UIStyles.Colors.Sets.Primary), UIColors.BackgroundMedium);

                Assert.Equal(TextColor(expected), TextColor(primary));
                Assert.NotEqual(UIColors.TextSecondary, TextColor(primary));
            }
        }
        finally
        {
            UIColors.SetAccent(before);
        }
    }

    [Fact]
    public void CreateNormal_IsTheOldNameOfCreateStandard()
    {
#pragma warning disable CS0618
        using Label old = UIStyles.Labels.CreateNormal("x");
#pragma warning restore CS0618
        using Label standard = UIStyles.Labels.CreateStandard("x");

        Assert.Equal(standard.Font, old.Font);
        Assert.Equal(standard.TextAlign, old.TextAlign);
        Assert.Equal(TextColor(standard), TextColor(old));
    }
}
