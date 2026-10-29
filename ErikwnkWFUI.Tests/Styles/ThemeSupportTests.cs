using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>ThemeColor (a color that follows a default until it is set) and SurfaceColor (the color a control's text stands on).</summary>
[Collection(AccentColorTestCollection.Name)]
public class ThemeSupportTests
{
    [Fact]
    public void ThemeColor_ReadsItsDefaultAnew_UntilItIsSet_AndGoesBackOnReset()
    {
        Color current = Color.Red;
        ThemeColor color = new ThemeColor(() => current);

        Assert.False(color.IsSet);
        Assert.Equal(Color.Red, color.Value);

        current = Color.Blue;
        Assert.Equal(Color.Blue, color.Value);

        color.Set(Color.Green);
        current = Color.Yellow;
        Assert.True(color.IsSet);
        Assert.Equal(Color.Green, color.Value);

        color.Reset();
        Assert.False(color.IsSet);
        Assert.Equal(Color.Yellow, color.Value);
    }

    [Fact]
    public void ThemeColor_AnotherDefault_IsFollowedLive_ButNeverReplacesAColorSetByHand()
    {
        Color other = Color.Orange;
        ThemeColor color = new ThemeColor(() => Color.Red);

        color.SetDefault(() => other);
        Assert.Equal(Color.Orange, color.Value);

        other = Color.Purple;
        Assert.Equal(Color.Purple, color.Value);

        color.Set(Color.Green);
        color.SetDefault(() => Color.Black);
        Assert.Equal(Color.Green, color.Value);
    }

    [Fact]
    public void SurfaceColor_IsTheOwnOpaqueBackground_OrTheFirstOpaqueOneBehindIt()
    {
        using Panel parent = new Panel { BackColor = Color.Navy };
        using Panel child = new Panel { BackColor = Color.Transparent, Parent = parent };
        using Panel own = new Panel { BackColor = Color.Teal, Parent = parent };

        Assert.Equal(Color.Teal, SurfaceColor.Of(own));
        Assert.Equal(Color.Navy, SurfaceColor.Of(child));
    }

    [Fact]
    public void TheColorOfThePage_IsTheFirstOpaqueBackgroundOfTheParents_NotTheOwn()
    {
        using Panel grandparent = new Panel { BackColor = Color.Navy };
        using Panel parent = new Panel { BackColor = Color.Transparent, Parent = grandparent };
        using Panel child = new Panel { BackColor = Color.Teal, Parent = parent };
        using Panel alone = new Panel { BackColor = Color.Teal };

        Assert.Equal(Color.Navy, InputField.GetPageColor(child));
        Assert.Equal(UIColors.BackgroundMedium, InputField.GetPageColor(alone));
    }

    [Fact]
    public void SurfaceColor_WhereNothingIsOpaque_Or_ForNoControl_IsTheMediumBackground()
    {
        using Panel alone = new Panel { BackColor = Color.Transparent };

        Assert.Equal(UIColors.BackgroundMedium, SurfaceColor.Of(alone));
        Assert.Equal(UIColors.BackgroundMedium, SurfaceColor.Of(null!));
    }
}
