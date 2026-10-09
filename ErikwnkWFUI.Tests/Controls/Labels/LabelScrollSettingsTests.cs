using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.LabelsTests;

/// <summary>
/// The movement of a label as one value (LabelScroll) handed to the factories or to the label, and as
/// single properties: what each way to make a LabelScroll holds, that every factory takes it, and that
/// the label gives the settings back. [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class LabelScrollSettingsTests
{
    private const string LongText = "A text that is much longer than the label it is shown in, so it has to move";

    [Fact]
    public void None_DoesNotMove()
    {
        Assert.Equal(LabelScrollMode.None, LabelScroll.None.Mode);
    }

    [Fact]
    public void BackAndForth_TakesTheMediumSettings_WhenNothingIsSaid()
    {
        LabelScroll scroll = LabelScroll.BackAndForth();

        Assert.Equal(LabelScrollMode.BackAndForth, scroll.Mode);
        Assert.Equal(LabelScrollSpeed.Medium, scroll.Speed);
        Assert.Equal(LabelScrollPause.Medium, scroll.Pause);
    }

    [Fact]
    public void BackAndForth_HoldsWhatItIsGiven()
    {
        LabelScroll scroll = LabelScroll.BackAndForth(LabelScrollSpeed.Fast, LabelScrollPause.Long);

        Assert.Equal(LabelScrollSpeed.Fast, scroll.Speed);
        Assert.Equal(LabelScrollPause.Long, scroll.Pause);
    }

    [Fact]
    public void Endless_TakesALeftDotAndTheMediumSpeed_WhenNothingIsSaid()
    {
        LabelScroll scroll = LabelScroll.Endless();

        Assert.Equal(LabelScrollMode.Endless, scroll.Mode);
        Assert.Equal(LabelScrollDirection.Left, scroll.Direction);
        Assert.Equal(LabelScrollSeparator.Dot, scroll.Separator);
        Assert.Equal(LabelScrollSpeed.Medium, scroll.Speed);
    }

    [Fact]
    public void Endless_HoldsWhatItIsGiven()
    {
        LabelScroll scroll = LabelScroll.Endless(LabelScrollDirection.Right, LabelScrollSeparator.Diamond, LabelScrollSpeed.Slow);

        Assert.Equal(LabelScrollDirection.Right, scroll.Direction);
        Assert.Equal(LabelScrollSeparator.Diamond, scroll.Separator);
        Assert.Equal(LabelScrollSpeed.Slow, scroll.Speed);
    }

    [Fact]
    public void EveryFactory_WithoutAScroll_MakesALabelThatDoesNotMove()
    {
        foreach (ThemedLabel label in new[]
        {
            UIStyles.Labels.CreateStandard("x"), UIStyles.Labels.CreatePrimary("x"),
            UIStyles.Labels.CreateTitle("x"), UIStyles.Labels.CreateMuted("x"),
            UIStyles.Labels.CreateStandard("x", scroll: null)
        })
        {
            using (label)
                Assert.Equal(LabelScrollMode.None, label.ScrollMode);
        }
    }

    [Fact]
    public void EveryFactory_TakesTheScroll_AndTheLabelHasAllItsSettings()
    {
        LabelScroll endless = LabelScroll.Endless(LabelScrollDirection.Right, LabelScrollSeparator.Bar, LabelScrollSpeed.Fast);
        LabelScroll backAndForth = LabelScroll.BackAndForth(LabelScrollSpeed.Slow, LabelScrollPause.Short);

        foreach (LabelScroll scroll in new[] { endless, backAndForth })
        {
            foreach (ThemedLabel label in new[]
            {
                UIStyles.Labels.CreateStandard("x", scroll: scroll), UIStyles.Labels.CreatePrimary("x", scroll),
                UIStyles.Labels.CreateTitle("x", scroll), UIStyles.Labels.CreateMuted("x", scroll),
                UIStyles.Labels.CreateStandard("x", UIStyles.Colors.Sets.Green, scroll)
            })
            {
                using (label)
                {
                    Assert.Equal(scroll.Mode, label.ScrollMode);
                    Assert.Equal(scroll.Speed, label.ScrollSpeed);
                    Assert.Equal(scroll.Pause, label.ScrollPause);
                    Assert.Equal(scroll.Direction, label.ScrollDirection);
                    Assert.Equal(scroll.Separator, label.ScrollSeparator);
                }
            }
        }
    }

    [Fact]
    public void TheScrollOfTheFactory_ReallyMakesALongTextMove()
    {
        using ThemedLabel label = UIStyles.Labels.CreateStandard(LongText, scroll: LabelScroll.Endless());
        label.Size = new Size(120, 24);

        Assert.True(label.GetPrivateField<int>("_cycle") > 0);
    }

    [Fact]
    public void TheScrollProperty_GivesBackTheSettingsOfTheCurrentMode()
    {
        using ThemedLabel label = UIStyles.Labels.CreateStandard("x");
        Assert.Equal(LabelScrollMode.None, label.Scroll.Mode);

        label.Scroll = LabelScroll.Endless(LabelScrollDirection.Right, LabelScrollSeparator.None, LabelScrollSpeed.Fast);
        LabelScroll endless = label.Scroll;

        Assert.Equal(LabelScrollMode.Endless, endless.Mode);
        Assert.Equal(LabelScrollDirection.Right, endless.Direction);
        Assert.Equal(LabelScrollSeparator.None, endless.Separator);
        Assert.Equal(LabelScrollSpeed.Fast, endless.Speed);

        label.Scroll = LabelScroll.BackAndForth(LabelScrollSpeed.Slow, LabelScrollPause.Long);
        LabelScroll backAndForth = label.Scroll;

        Assert.Equal(LabelScrollMode.BackAndForth, backAndForth.Mode);
        Assert.Equal(LabelScrollSpeed.Slow, backAndForth.Speed);
        Assert.Equal(LabelScrollPause.Long, backAndForth.Pause);
    }

    [Fact]
    public void ANullScroll_IsNoMovement()
    {
        using ThemedLabel label = UIStyles.Labels.CreateStandard("x", scroll: LabelScroll.Endless());

        label.Scroll = null!;

        Assert.Equal(LabelScrollMode.None, label.ScrollMode);
    }

    [Fact]
    public void OneScroll_CanServeManyLabels_AndTheLabelsChangeOnTheirOwn()
    {
        LabelScroll shared = LabelScroll.Endless();
        using ThemedLabel first = UIStyles.Labels.CreateStandard("x", scroll: shared);
        using ThemedLabel second = UIStyles.Labels.CreateStandard("y", scroll: shared);

        first.ScrollSpeed = LabelScrollSpeed.Fast;
        first.ScrollDirection = LabelScrollDirection.Right;

        Assert.Equal(LabelScrollSpeed.Medium, shared.Speed);
        Assert.Equal(LabelScrollDirection.Left, shared.Direction);
        Assert.Equal(LabelScrollSpeed.Medium, second.ScrollSpeed);
        Assert.Equal(LabelScrollDirection.Left, second.ScrollDirection);
    }

    [Fact]
    public void ASingleProperty_ChangesJustThatSetting()
    {
        using ThemedLabel label = UIStyles.Labels.CreateStandard("x", scroll: LabelScroll.Endless(LabelScrollDirection.Right, LabelScrollSeparator.Bar));

        label.ScrollSpeed = LabelScrollSpeed.Slow;

        Assert.Equal(LabelScrollMode.Endless, label.ScrollMode);
        Assert.Equal(LabelScrollDirection.Right, label.ScrollDirection);
        Assert.Equal(LabelScrollSeparator.Bar, label.ScrollSeparator);
        Assert.Equal(LabelScrollSpeed.Slow, label.ScrollSpeed);
    }
}
