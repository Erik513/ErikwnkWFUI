using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.CheckBox;

/// <summary>
/// The label of a check box and of a radio button: a ForeColor that was set colors it (dimmed when disabled), one that was not set leaves the
/// color of the theme, also when the parent has a ForeColor, and Color.Empty gives the color of the library back. Measured on the picture of the label.
/// [Collection]: the colors read the live theme.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class BoxLabelForeColorTests
{
    private static int ReddishPixels(ButtonBase box, bool enabled = true)
    {
        using Panel page = new Panel { Size = new Size(160, 30), BackColor = UIColors.BackgroundMedium };
        box.Enabled = enabled;
        box.Location = new Point(0, 2);
        page.Controls.Add(box);

        using Bitmap picture = Draw(box);
        int count = 0;

        // The label is at the right of the box: the part of the picture beyond the glyph column.
        for (int y = 0; y < picture.Height; y++)
        {
            for (int x = 28; x < picture.Width; x++)
            {
                Color pixel = picture.GetPixel(x, y);

                // (Strong red only: ClearType colors the edges of gray text a little.)
                if (pixel.R >= 170 && pixel.G <= 80 && pixel.B <= 80)
                    count++;
            }
        }

        // (Taken out again: the panel would dispose it, and the test measures the same box more than once.)
        page.Controls.Remove(box);

        return count;
    }

    public static IEnumerable<object[]> Boxes()
    {
        yield return new object[] { "CheckBox", (Func<ButtonBase>)(() => UIStyles.CheckBoxes.CreateStandard("Label text", true)) };
        yield return new object[] { "RadioButton", (Func<ButtonBase>)(() => UIStyles.RadioButtons.CreateStandard("Label text", true)) };
    }

    [Theory]
    [MemberData(nameof(Boxes))]
    public void ALabelWithoutAColorOfItsOwn_HasNoRed_AndASetColorColorsIt_AndEmptyGivesTheThemeBack(string name, Func<ButtonBase> create)
    {
        using ButtonBase box = create();
        int baseline = ReddishPixels(box);

        box.ForeColor = Color.Red;
        Assert.True(ReddishPixels(box) > baseline + 15, name + ": the label is not red");

        box.ForeColor = Color.Empty;
        Assert.Equal(baseline, ReddishPixels(box));
    }

    [Theory]
    [MemberData(nameof(Boxes))]
    public void AColorOfTheParent_DoesNotColorTheLabel_OnlyOneSetOnTheBox(string name, Func<ButtonBase> create)
    {
        using ButtonBase box = create();
        using Panel page = new Panel { Size = new Size(160, 30), BackColor = UIColors.BackgroundMedium, ForeColor = Color.Red };
        box.Location = new Point(0, 2);
        page.Controls.Add(box);

        using Bitmap picture = Draw(box);
        int red = 0;

        for (int y = 0; y < picture.Height; y++)
            for (int x = 28; x < picture.Width; x++)
                if (picture.GetPixel(x, y).R >= 170 && picture.GetPixel(x, y).G <= 80 && picture.GetPixel(x, y).B <= 80)
                    red++;

        page.Controls.Remove(box);

        Assert.True(red <= 3, name + ": the label took the color of the parent");
    }

    [Theory]
    [MemberData(nameof(Boxes))]
    public void ADisabledBox_KeepsTheColorThatWasSet_Dimmed(string name, Func<ButtonBase> create)
    {
        using ButtonBase box = create();
        int baseline = ReddishPixels(box, enabled: false);
        box.ForeColor = Color.Red;

        Assert.True(ReddishPixels(box, enabled: false) > baseline + 8, name + ": a disabled label lost the color that was set");
    }

    [Fact]
    public void TheColorOfTheLabel_IsTheOneThatWasSet_Dimmed_WhenDisabled_AndTheThemeOne_WithoutIt()
    {
        using System.Windows.Forms.CheckBox box = UIStyles.CheckBoxes.CreateStandard("x", true);
        Color page = UIColors.BackgroundMedium;
        Color own = Color.FromArgb(200, 40, 40);

        Assert.Equal(own, global::ErikwnkWFUI.Controls.BoxLabel.GetTextColor(box, page, own));
        Assert.Equal(UIColors.TextPrimary, global::ErikwnkWFUI.Controls.BoxLabel.GetTextColor(box, page));

        box.Enabled = false;
        Assert.Equal(UIColors.BlendToward(own, page, 0.35), global::ErikwnkWFUI.Controls.BoxLabel.GetTextColor(box, page, own));
    }
}
