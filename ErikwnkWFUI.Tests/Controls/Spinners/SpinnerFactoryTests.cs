using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Spinners;

/// <summary>What the factories of the spinners make, and the settings they preset. [Collection]: the primary set reads the live accent.</summary>
[Collection(AccentColorTestCollection.Name)]
public class SpinnerFactoryTests : IDisposable
{
    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    // The color the arc is painted in (what the spinner works out while it paints).
    private static Color ArcOf(Spinner spinner) => spinner.InvokePrivate<Color>("ResolveArcColor");

    // What the arc of a spinner is for a color: moved just far enough to stand out from the track (3:1).
    private static Color Ensured(Spinner spinner, Color raw) => UIColors.EnsureContrast(raw, spinner.GetTrackColor(), 3.0);

    public static IEnumerable<object[]> All()
    {
        yield return new object[] { "CreateStandard", (Func<Spinner>)(() => UIStyles.Spinners.CreateStandard()), false };
        yield return new object[] { "CreateGreen", (Func<Spinner>)(() => UIStyles.Spinners.CreateGreen()), false };
        yield return new object[] { "CreatePrimary", (Func<Spinner>)(() => UIStyles.Spinners.CreatePrimary()), false };
        yield return new object[] { "CreateStatus", (Func<Spinner>)(() => UIStyles.Spinners.CreateStatus()), false };
        yield return new object[] { "CreateProgressStandard", (Func<Spinner>)(() => UIStyles.Spinners.CreateProgressStandard()), true };
        yield return new object[] { "CreateProgressGreen", (Func<Spinner>)(() => UIStyles.Spinners.CreateProgressGreen()), true };
        yield return new object[] { "CreateProgressPrimary", (Func<Spinner>)(() => UIStyles.Spinners.CreateProgressPrimary()), true };
        yield return new object[] { "CreateProgressStatus", (Func<Spinner>)(() => UIStyles.Spinners.CreateProgressStatus()), true };
    }

    [Theory]
    [MemberData(nameof(All))]
    public void EveryFactoryMethod_MakesASquareSpinner_WithOrWithoutThePercentage(string name, Func<Spinner> make, bool withPercentage)
    {
        using Spinner spinner = make();

        Assert.Equal(withPercentage ? new Size(40, 40) : new Size(24, 24), spinner.Size);
        Assert.Equal(0, spinner.Thickness);
        Assert.Equal(withPercentage, spinner.ShowPercentage);
        Assert.Equal(0, spinner.Value);
    }

    [Fact]
    public void ASizeAndAThickness_AreGiven_ByEveryFactoryMethod()
    {
        foreach (Spinner spinner in new[]
        {
            UIStyles.Spinners.CreateStandard(40, 5), UIStyles.Spinners.CreateGreen(40, 5), UIStyles.Spinners.CreatePrimary(40, 5),
            UIStyles.Spinners.CreateProgressStandard(40, 5), UIStyles.Spinners.CreateProgressGreen(40, 5),
            UIStyles.Spinners.CreateProgressPrimary(40, 5), UIStyles.Spinners.CreateProgressStatus(40, 5), UIStyles.Spinners.CreateStatus(40, 5),
            UIStyles.Spinners.CreateStandard(40, 5, UIStyles.Colors.Sets.Teal)
        })
        {
            using (spinner)
            {
                Assert.Equal(new Size(40, 40), spinner.Size);
                Assert.Equal(5, spinner.Thickness);
            }
        }
    }

    [Fact]
    public void WithoutAColorSet_TheArcIsTheNeutralGray_AsItWas()
    {
        using Spinner standard = UIStyles.Spinners.CreateStandard();
        using Spinner progress = UIStyles.Spinners.CreateProgressStandard();

        Assert.Equal(Ensured(standard, UIColors.DisabledGray), ArcOf(standard));
        Assert.Equal(Ensured(progress, UIColors.DisabledGray), ArcOf(progress));
        Assert.Null(standard.Colors);
        Assert.True(standard.ArcColor.IsEmpty);
    }

    [Fact]
    public void WithAColorSet_TheArcIsTheMainColorOfTheSet_AlsoForThePlainColorAndTheProgressSpinner()
    {
        UIColorSet teal = UIStyles.Colors.Sets.Teal;

        using Spinner standard = UIStyles.Spinners.CreateStandard(24, 0, teal);
        using Spinner progress = UIStyles.Spinners.CreateProgressStandard(24, 0, teal);
        using Spinner plain = UIStyles.Spinners.CreateStandard(24, 0, UIStyles.Colors.CreateSet(Color.FromArgb(210, 110, 20)));

        Assert.Equal(Ensured(standard, teal.Hover), ArcOf(standard));
        Assert.Equal(Ensured(progress, teal.Hover), ArcOf(progress));
        Assert.Same(teal, standard.Colors);
        Assert.Equal(Ensured(plain, Color.FromArgb(210, 110, 20)), ArcOf(plain));
    }

    [Fact]
    public void Green_IsTheStandardSpinnerInTheGreenSet_AndPrimaryFollowsTheAccent()
    {
        using Spinner green = UIStyles.Spinners.CreateGreen();
        using Spinner progressGreen = UIStyles.Spinners.CreateProgressGreen();
        using Spinner primary = UIStyles.Spinners.CreatePrimary();
        using Spinner progressPrimary = UIStyles.Spinners.CreateProgressPrimary();

        Assert.Equal(Ensured(green, UIColors.Green), ArcOf(green));
        Assert.Equal(Ensured(progressGreen, UIColors.Green), ArcOf(progressGreen));

        Assert.Equal(Ensured(primary, UIColors.PrimarySet.Hover), ArcOf(primary));
        Assert.Equal(Ensured(progressPrimary, UIColors.PrimarySet.Hover), ArcOf(progressPrimary));

        Color before = ArcOf(primary);
        UIColors.SetAccent(Color.OrangeRed);

        Assert.NotEqual(before, ArcOf(primary));
        Assert.Equal(Ensured(primary, UIColors.PrimarySet.Hover), ArcOf(primary));
        Assert.Equal(Ensured(progressPrimary, UIColors.PrimarySet.Hover), ArcOf(progressPrimary));
    }

    [Fact]
    public void TheStatusSpinner_ColorsTheArcByTheValue_FromRedOverYellowToGreen()
    {
        using Spinner status = UIStyles.Spinners.CreateProgressStatus();

        Assert.True(status.UseStatusGradient);
        Assert.True(status.ShowPercentage);

        using Spinner plainStatus = UIStyles.Spinners.CreateStatus();
        Assert.True(plainStatus.UseStatusGradient);
        Assert.False(plainStatus.ShowPercentage);
        plainStatus.Value = 100;
        Assert.Equal(Ensured(plainStatus, UIColors.Green), ArcOf(plainStatus));

        status.Value = 0;
        Color red = ArcOf(status);
        status.Value = 65;
        Color yellow = ArcOf(status);
        status.Value = 100;
        Color green = ArcOf(status);

        Assert.Equal(Ensured(status, UIColors.Red), red);
        Assert.Equal(Ensured(status, UIColors.Yellow), yellow);
        Assert.Equal(Ensured(status, UIColors.Green), green);
    }

    [Fact]
    public void AColorSetByHand_WinsOverTheSet_AndOverTheStatus_AndEmptyGivesTheRulesBack()
    {
        using Spinner status = UIStyles.Spinners.CreateProgressStatus();
        status.Value = 100;

        status.ArcColor = Color.Purple;
        Assert.Equal(Color.Purple, ArcOf(status));

        status.ArcColor = Color.Empty;
        Assert.Equal(Ensured(status, UIColors.Green), ArcOf(status));

        using Spinner set = UIStyles.Spinners.CreateStandard(24, 0, UIStyles.Colors.Sets.Teal);
        set.ArcColor = Color.Purple;
        Assert.Equal(Color.Purple, ArcOf(set));
    }

    [Fact]
    public void ADisabledSpinner_HasTheDisabledColor_WhateverTheSet()
    {
        using Spinner spinner = UIStyles.Spinners.CreateStandard(24, 0, UIStyles.Colors.Sets.Teal);
        spinner.Enabled = false;

        Assert.Equal(UIColors.EnsureContrast(UIColors.GetDisabledText(UIColors.BackgroundMedium), spinner.GetTrackColor(), 3.0), ArcOf(spinner));
    }

    [Fact]
    public void TheValue_IsLimitedToZeroUpToAHundred_AndThePercentageIsOffByDefault()
    {
        using Spinner spinner = new Spinner();

        Assert.False(spinner.ShowPercentage);

        spinner.Value = 250;
        Assert.Equal(100, spinner.Value);

        spinner.Value = -5;
        Assert.Equal(0, spinner.Value);
    }
}
