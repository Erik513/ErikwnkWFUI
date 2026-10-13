using System.Drawing;
using System.Drawing.Drawing2D;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>The path of a rectangle with rounded corners: it fills the rectangle, its corners are cut, a pill has round ends.</summary>
public class RoundedRectangleTests
{
    [Fact]
    public void ThePath_ReachesTheEdgesOfTheRectangle()
    {
        Rectangle rect = new Rectangle(10, 20, 50, 30);
        using GraphicsPath path = RoundedRectangle.CreatePath(rect, 6);
        RectangleF bounds = path.GetBounds();

        Assert.InRange(bounds.Left, rect.Left - 1, rect.Left + 1);
        Assert.InRange(bounds.Top, rect.Top - 1, rect.Top + 1);
        Assert.InRange(bounds.Right, rect.Right - 1, rect.Right + 1);
        Assert.InRange(bounds.Bottom, rect.Bottom - 1, rect.Bottom + 1);
    }

    [Fact]
    public void TheCorners_AreCutOff_TheMiddleOfTheEdgesIsNot()
    {
        Rectangle rect = new Rectangle(0, 0, 40, 40);
        using GraphicsPath path = RoundedRectangle.CreatePath(rect, 10);

        Assert.False(path.IsVisible(0, 0));
        Assert.False(path.IsVisible(39, 0));
        Assert.False(path.IsVisible(0, 39));
        Assert.False(path.IsVisible(39, 39));
        Assert.True(path.IsVisible(20, 1));
        Assert.True(path.IsVisible(1, 20));
        Assert.True(path.IsVisible(20, 20));
    }

    [Fact]
    public void ARadiusOfHalfTheHeight_MakesAPill()
    {
        Rectangle rect = new Rectangle(0, 0, 60, 20);
        using GraphicsPath path = RoundedRectangle.CreatePath(rect, 10);

        Assert.False(path.IsVisible(0, 0));
        Assert.False(path.IsVisible(1, 1));
        Assert.True(path.IsVisible(10, 10));
        Assert.True(path.IsVisible(50, 10));
        Assert.True(path.IsVisible(30, 1));
    }
}
