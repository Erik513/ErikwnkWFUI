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

        Assert.True(withFrame >= 5, "the text starts at column " + withFrame);
        Assert.True(withFrame - without >= 2, "framed " + withFrame + ", borderless " + without);
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

        Assert.True(framed - none >= 2, "framed " + framed + ", none " + none);
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

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, ref Rect rect);

    // How much room the text has at each side of the box: client area minus the formatting rectangle of the text.
    private static (int Left, int Right) Room(TextBox box)
    {
        Rect format = new Rect();
        SendMessage(box.Handle, 0x00B2, IntPtr.Zero, ref format);

        return (format.Left, box.ClientSize.Width - format.Right);
    }

    [Fact]
    public void TheTextHasRoomAtTheRightToo_ASmallOneWithoutAButton_AndTheButtonWithOne()
    {
        TextBox plain = _host.Show(UIStyles.TextBoxes.CreateStandard("Place", ""));
        TextBox password = _host.Show(UIStyles.TextBoxes.CreatePassword("Place", ""));
        TextBox borderless = _host.Show(UIStyles.TextBoxes.CreateBorderless("Place", ""));

        Assert.True(Room(plain).Right >= 5, "right room " + Room(plain).Right);
        Assert.True(Room(plain).Left >= 4, "left room " + Room(plain).Left);
        Assert.True(Room(password).Right >= 24, "the text keeps away from the eye: " + Room(password).Right);
        Assert.True(Room(borderless).Right < Room(plain).Right, "a box without a frame keeps the narrow margin");
    }

    [Fact]
    public void ALongTextEndsBeforeTheRightEdge_NotAtIt()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreateStandard(string.Join(" ", Enumerable.Repeat("a long text that does not fit", 4)), ""));

        using Bitmap picture = Draw(box);
        bool inkAtTheEdge = false;

        // The five columns before the frame at the right (the last column): nothing of the text may be painted there - the same room as at
        // the left, where the first letter begins five columns after the frame.
        for (int x = box.Width - 6; x < box.Width - 1 && !inkAtTheEdge; x++)
            for (int y = 4; y < box.Height - 4 && !inkAtTheEdge; y++)
                inkAtTheEdge = picture.GetPixel(x, y).ToArgb() != box.BackColor.ToArgb();

        Assert.False(inkAtTheEdge, "text touches the frame at the right");
    }

    // ---- the symbol in the box has the color of the set - the pencil as well as the check mark

    private static bool HasTint(TextBox box)
    {
        using Bitmap picture = Draw(box);
        int yellow = 0;

        for (int x = box.Width - 26; x < box.Width - 2; x++)
            for (int y = 4; y < box.Height - 4; y++)
            {
                Color c = picture.GetPixel(x, y);

                // (Yellow: red and green high, blue low. The colored fringes that the text smoothing leaves at the edges of gray letters are not.)
                if (c.R > 180 && c.G > 140 && c.B < 90)
                    yellow++;
            }

        // (A handful of pixels can be colored fringes of the text smoothing; a yellow symbol has many.)
        return yellow >= 10;
    }

    // The brightest red value in the place of the symbol: how light the symbol is.
    private static int Lightness(TextBox box)
    {
        using Bitmap picture = Draw(box);
        int lightest = 0;

        for (int x = box.Width - 26; x < box.Width - 2; x++)
            for (int y = 4; y < box.Height - 4; y++)
                lightest = Math.Max(lightest, picture.GetPixel(x, y).R);

        return lightest;
    }

    [Fact]
    public void TheSymbolsAreNeverColored_NotByAColorSetEither_AndTheOneThatIsSwitchedOnIsLighter()
    {
        ErikwnkWFUI.Controls.LockableTextBox yellowLocked = _host.Show(UIStyles.TextBoxes.CreateLockable("Text", "", true, UIStyles.Colors.Sets.Yellow));
        ErikwnkWFUI.Controls.LockableTextBox yellowUnlocked = _host.Show(UIStyles.TextBoxes.CreateLockable("Text", "", false, UIStyles.Colors.Sets.Yellow));
        ErikwnkWFUI.Controls.LockableTextBox plainLocked = _host.Show(UIStyles.TextBoxes.CreateLockable("Text"));
        ErikwnkWFUI.Controls.LockableTextBox plainUnlocked = _host.Show(UIStyles.TextBoxes.CreateLockable("Text", "", false));

        foreach (TextBox box in new TextBox[] { yellowLocked, yellowUnlocked, plainLocked, plainUnlocked })
            Assert.False(HasTint(box), "the symbol is colored");

        // The check mark of the box that is being edited is lighter than the pencil of the box that is locked - with and without a set, alike.
        Assert.True(Lightness(yellowUnlocked) > Lightness(yellowLocked), "with a set: " + Lightness(yellowUnlocked) + " against " + Lightness(yellowLocked));
        Assert.True(Lightness(plainUnlocked) > Lightness(plainLocked), "without: " + Lightness(plainUnlocked) + " against " + Lightness(plainLocked));
        Assert.Equal(Lightness(plainUnlocked), Lightness(yellowUnlocked));
        Assert.Equal(Lightness(plainLocked), Lightness(yellowLocked));
    }

    [Fact]
    public void ThePasswordEye_IsNotColored_Either_AndLighterWhileThePasswordIsShown()
    {
        ErikwnkWFUI.Controls.TextField yellow = (ErikwnkWFUI.Controls.TextField)_host.Show(UIStyles.TextBoxes.CreatePassword("secret", "", UIStyles.Colors.Sets.Yellow));
        TextBox plain = _host.Show(UIStyles.TextBoxes.CreatePassword("secret", ""));
        int hidden = Lightness(yellow);

        Assert.False(HasTint(yellow));
        Assert.False(HasTint(plain));
        Assert.Equal(Lightness(plain), hidden);

        yellow.GetType().GetProperty("IsPasswordShown")!.SetValue(yellow, true);

        Assert.True(Lightness(yellow) > hidden, "the eye of a shown password is lighter");
        Assert.False(HasTint(yellow));
    }
}