using System.Drawing;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>A control with a transparent background paints what its parent has behind it: the parent's own painting, moved to where the control is.</summary>
public class ParentBackgroundTests
{
    [Fact]
    public void WithoutAParent_NothingIsPainted()
    {
        using Control child = new Control();
        using Bitmap bitmap = new Bitmap(50, 50);
        using Graphics graphics = Graphics.FromImage(bitmap);
        int calls = 0;

        ParentBackground.Paint(child, graphics, e => calls++);

        Assert.Equal(0, calls);
    }

    [Fact]
    public void WithAParent_ItsPaintingCoversTheParent_ShiftedBehindTheChild_AndTheGraphicsIsPutBack()
    {
        using Control parent = new Control { Size = new Size(120, 80) };
        using Control child = new Control { Location = new Point(10, 20), Size = new Size(30, 30), Parent = parent };
        using Bitmap bitmap = new Bitmap(50, 50);
        using Graphics graphics = Graphics.FromImage(bitmap);
        Rectangle area = Rectangle.Empty;
        float shiftX = 0;
        float shiftY = 0;

        ParentBackground.Paint(child, graphics, e =>
        {
            area = e.ClipRectangle;
            shiftX = e.Graphics.Transform.OffsetX;
            shiftY = e.Graphics.Transform.OffsetY;
        });

        Assert.Equal(parent.ClientRectangle, area);
        Assert.Equal(-10, shiftX);
        Assert.Equal(-20, shiftY);
        Assert.Equal(0, graphics.Transform.OffsetX);
        Assert.Equal(0, graphics.Transform.OffsetY);
    }

    [Fact]
    public void WhenThePaintingThrows_TheGraphicsIsStillPutBack()
    {
        using Control parent = new Control { Size = new Size(120, 80) };
        using Control child = new Control { Location = new Point(10, 20), Parent = parent };
        using Bitmap bitmap = new Bitmap(50, 50);
        using Graphics graphics = Graphics.FromImage(bitmap);

        Assert.Throws<InvalidOperationException>(() => ParentBackground.Paint(child, graphics, e => throw new InvalidOperationException()));

        Assert.Equal(0, graphics.Transform.OffsetX);
    }
}
