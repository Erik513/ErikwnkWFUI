using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.CheckBox;

/// <summary>The private bitmap the box is drawn into: kept between paints, made again for a new size, freed with the box.</summary>
public class CheckBoxBufferTests
{
    private static Bitmap? Buffer(Control box) => box.GetPrivateField<Bitmap>("_buffer");

    private static void Paint(Control box)
    {
        using Bitmap picture = new Bitmap(box.Width, box.Height);
        box.DrawToBitmap(picture, new Rectangle(Point.Empty, box.Size));
    }

    [Fact]
    public void TheSameBitmap_IsUsedForEveryPaint_OfTheSameSize()
    {
        using System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("Label");

        Assert.Null(Buffer(box));
        Paint(box);
        Bitmap? first = Buffer(box);
        Paint(box);
        Paint(box);

        Assert.NotNull(first);
        Assert.Same(first, Buffer(box));
    }

    [Fact]
    public void ANewSize_GetsANewBitmap_OfThatSize()
    {
        using System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("Label");
        Paint(box);
        Bitmap? small = Buffer(box);

        box.Size = new Size(box.Width + 40, box.Height + 10);
        Paint(box);

        Assert.NotSame(small, Buffer(box));
        Assert.Equal(box.Width, Buffer(box)!.Width);
        Assert.Equal(box.Height, Buffer(box)!.Height);
    }

    [Fact]
    public void TheBitmap_IsFreedWithTheBox()
    {
        System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("Label");
        Paint(box);

        box.Dispose();

        Assert.Null(Buffer(box));
    }

    [Fact]
    public void WhatWasDrawnBefore_DoesNotShowThrough_WhenTheBitmapIsUsedAgain()
    {
        using System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("", true);
        using Bitmap checkedBox = new Bitmap(box.Width, box.Height);
        box.DrawToBitmap(checkedBox, new Rectangle(Point.Empty, box.Size));

        ((System.Windows.Forms.CheckBox)box).Checked = false;
        using Bitmap empty = new Bitmap(box.Width, box.Height);
        box.DrawToBitmap(empty, new Rectangle(Point.Empty, box.Size));
        using System.Windows.Forms.CheckBox fresh = UIStyles.CheckBoxes.CreateStandard("", false);
        using Bitmap freshEmpty = new Bitmap(fresh.Width, fresh.Height);
        fresh.DrawToBitmap(freshEmpty, new Rectangle(Point.Empty, fresh.Size));

        for (int y = 0; y < empty.Height; y++)
            for (int x = 0; x < empty.Width; x++)
                Assert.Equal(freshEmpty.GetPixel(x, y).ToArgb(), empty.GetPixel(x, y).ToArgb());
    }
}
