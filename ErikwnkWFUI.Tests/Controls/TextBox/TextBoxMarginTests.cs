using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using TextBox = System.Windows.Forms.TextBox;

namespace ErikwnkWFUI.Tests.Controls.TextBoxes;

/// <summary>The room between the frame of a text box and its text, and the placeholder, which stands where a text would.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TextBoxMarginTests : IDisposable
{
    private readonly TextBoxHost _host = new TextBoxHost();

    public void Dispose()
    {
        _host.Dispose();
    }

    // The first column (from the second one: the first is the frame) in which something is painted that is not the background.
    private static int FirstInkColumn(TextBox box)
    {
        using Bitmap picture = Draw(box);

        for (int x = 1; x < box.Width - 3; x++)
        {
            for (int y = 3; y < box.Height - 3; y++)
            {
                if (picture.GetPixel(x, y).ToArgb() != box.BackColor.ToArgb())
                    return x;
            }
        }

        return -1;
    }

    [Fact]
    public void ABoxWithAFrame_HasRoomBetweenTheFrameAndTheText_AndOneWithoutAFrameKeepsTheNarrowMargin()
    {
        TextBox framed = _host.Show(UIStyles.TextBoxes.CreateStandard("Place", ""));
        TextBox borderless = _host.Show(UIStyles.TextBoxes.CreateBorderless("Place", ""));

        int withFrame = FirstInkColumn(framed);
        int without = FirstInkColumn(borderless);

        Assert.True(withFrame >= 6, "the text starts at column " + withFrame);
        Assert.True(withFrame - without >= 4, "framed " + withFrame + ", borderless " + without);
    }

    [Fact]
    public void TheMarginFollowsTheBorderStyle_WhenItIsChangedWhileTheBoxIsShown()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreateStandard("Place", ""));
        int framed = FirstInkColumn(box);

        box.BorderStyle = BorderStyle.None;
        int none = FirstInkColumn(box);

        box.BorderStyle = BorderStyle.FixedSingle;
        int back = FirstInkColumn(box);

        Assert.True(framed - none >= 4, "framed " + framed + ", none " + none);
        Assert.Equal(framed, back);
    }

    [Fact]
    public void ThePlaceholderStandsWhereTheTextStands_AlsoWithTheMargin_ForEveryKindOfBox()
    {
        foreach (TextBox[] pair in new[]
        {
            new[] { UIStyles.TextBoxes.CreateStandard("Place", ""), UIStyles.TextBoxes.CreateStandard("", "Place") },
            new[] { UIStyles.TextBoxes.CreatePassword("Place", ""), UIStyles.TextBoxes.CreatePassword("", "Place") },
            new[] { UIStyles.TextBoxes.CreateLockable("Place", ""), UIStyles.TextBoxes.CreateLockable("", "Place") },
            new[] { UIStyles.TextBoxes.CreateBorderless("Place", ""), UIStyles.TextBoxes.CreateBorderless("", "Place") }
        })
        {
            // (A password box hides its text as dots, the placeholder is the readable word: only where they begin is compared.)
            TextBox typed = _host.Show(pair[0]);
            TextBox hint = _host.Show(pair[1]);

            Assert.True(Math.Abs(FirstInkColumn(typed) - FirstInkColumn(hint)) <= 2, typed.GetType().Name + ": text " + FirstInkColumn(typed) + ", placeholder " + FirstInkColumn(hint));
        }
    }
}