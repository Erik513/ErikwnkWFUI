using System.Drawing;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>What a control keeps of its color set: the set that was given, or a source asked for again each time (the last one given wins).</summary>
public class ColorSetHolderTests
{
    [Fact]
    public void WithoutAnything_TheValueIsNull()
    {
        Assert.Null(new ColorSetHolder().Value);
    }

    [Fact]
    public void AGivenSet_IsTheValue()
    {
        ColorSetHolder holder = new ColorSetHolder();
        UIColorSet teal = UIStyles.Colors.Sets.Teal;

        holder.Value = teal;

        Assert.Same(teal, holder.Value);
    }

    [Fact]
    public void ASource_IsAskedForAgainOnEveryRead()
    {
        ColorSetHolder holder = new ColorSetHolder();
        UIColorSet current = UIStyles.Colors.Sets.Green;
        int asked = 0;
        holder.Follow(() =>
        {
            asked++;

            return current;
        });

        Assert.Same(current, holder.Value);

        current = UIStyles.Colors.Sets.Yellow;
        Assert.Same(UIStyles.Colors.Sets.Yellow, holder.Value);
        Assert.Equal(2, asked);
    }

    [Fact]
    public void TheLastOneGivenWins_ASetTakesTheSourceAway_AndASourceTheSet()
    {
        ColorSetHolder holder = new ColorSetHolder();
        UIColorSet red = UIStyles.Colors.Sets.Red;
        UIColorSet blue = UIStyles.Colors.Sets.Blue;

        holder.Follow(() => red);
        holder.Value = blue;
        Assert.Same(blue, holder.Value);

        holder.Follow(() => red);
        Assert.Same(red, holder.Value);

        holder.Value = null!;
        Assert.Null(holder.Value);
    }

    [Fact]
    public void ASourceThatAnswersNull_FallsBackToNothing_NotToAnOldSet()
    {
        ColorSetHolder holder = new ColorSetHolder();
        holder.Value = UIStyles.Colors.Sets.Teal;

        holder.Follow(() => null!);

        Assert.Null(holder.Value);
    }
}
