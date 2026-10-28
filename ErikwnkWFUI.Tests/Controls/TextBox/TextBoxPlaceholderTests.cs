using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using TextBox = System.Windows.Forms.TextBox;

namespace ErikwnkWFUI.Tests.Controls.TextBoxes;

/// <summary>The placeholder of the text box is painted over it, and is never its text: no PasswordChar masks it, no MaxLength cuts it, no TextChanged tells of it, and it goes when the box has a text or the focus.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TextBoxPlaceholderTests : IDisposable
{
    private readonly TextBoxHost _host = new TextBoxHost();

    public void Dispose()
    {
        _host.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    // Whether anything is painted in the box that is not its background: the placeholder, or a text.
    private static bool HasInk(TextBox box)
    {
        using Bitmap picture = Draw(box);
        Rectangle inside = new Rectangle(4, 4, box.Width - 8, Math.Min(box.Height, 20) - 6);

        for (int x = inside.Left; x < inside.Right; x++)
        {
            for (int y = inside.Top; y < inside.Bottom; y++)
            {
                if (picture.GetPixel(x, y).ToArgb() != box.BackColor.ToArgb())
                    return true;
            }
        }

        return false;
    }

    [Fact]
    public void AnEmptyBox_ShowsThePlaceholder_AndWithoutOneNothing()
    {
        TextBox with = _host.Show(UIStyles.TextBoxes.CreateStandard("", "Type here"));
        TextBox without = _host.Show(UIStyles.TextBoxes.CreateStandard("", ""));

        Assert.True(HasInk(with), "the placeholder is not painted");
        Assert.False(HasInk(without));
    }

    [Fact]
    public void ABoxWithAText_ShowsTheText_NotThePlaceholder_AndTheTextIsAsGiven()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreateStandard("x", "Type here"));
        TextBox plain = _host.Show(UIStyles.TextBoxes.CreateStandard("x", ""));
        using Bitmap first = Draw(box);
        using Bitmap second = Draw(plain);

        Assert.Equal("x", box.Text);
        Assert.True(SamePixels(first, second), "the placeholder is painted under a text");
    }

    [Fact]
    public void TypingAText_TakesThePlaceholderAway_AndClearingItBringsItBack()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreateStandard("", "Type here"));
        TextBox plain = _host.Show(UIStyles.TextBoxes.CreateStandard("", ""));
        using Bitmap empty = Draw(box);

        box.Text = "a";
        plain.Text = "a";
        using Bitmap typed = Draw(box);
        using Bitmap typedPlain = Draw(plain);
        Assert.True(SamePixels(typed, typedPlain));

        box.Text = "";
        using Bitmap cleared = Draw(box);
        Assert.True(SamePixels(empty, cleared));
    }

    [Fact]
    public void ThePlaceholder_RaisesNoTextChanged_AndIsNotMaskedByAPasswordChar_NorCutByMaxLength()
    {
        int changes = 0;
        TextBox plainBox = _host.Show(UIStyles.TextBoxes.CreateStandard("", "A long placeholder"));
        TextBox box = _host.Show(UIStyles.TextBoxes.CreateStandard("", "A long placeholder"));
        box.TextChanged += (s, e) => changes++;
        box.PasswordChar = '*';
        box.MaxLength = 3;
        Application.DoEvents();
        using Bitmap picture = Draw(box);
        using Bitmap reference = Draw(plainBox);

        Assert.Equal(0, changes);
        Assert.Equal(string.Empty, box.Text);
        Assert.True(SamePixels(picture, reference), "the placeholder looks different with a PasswordChar or a MaxLength");
    }

    [Fact]
    public void ThePlaceholder_IsReadable_InBothThemes()
    {
        foreach (bool light in new[] { false, true })
        {
            UIColors.ApplyTheme(light ? UIThemes.Light : UIThemes.Dark);
            TextBox box = _host.Show(UIStyles.TextBoxes.CreateStandard("", "Type here"));
            using Bitmap picture = Draw(box);
            double strongest = 1.0;

            for (int x = 4; x < box.Width - 4; x++)
            {
                for (int y = 4; y < 18; y++)
                    strongest = Math.Max(strongest, ColorContrastHelper.GetContrastRatio(picture.GetPixel(x, y), box.BackColor));
            }

            Assert.True(strongest >= 2.5, (light ? "light" : "dark") + " theme: the placeholder is faint (" + strongest.ToString("F2") + ":1)");
        }
    }

    [Fact]
    public void ThePlaceholder_FollowsAThemeChange_WhileTheBoxIsShown()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreateStandard("", "Type here"));
        using Bitmap dark = Draw(box);

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();
        TextBox made = _host.Show(UIStyles.TextBoxes.CreateStandard("", "Type here"));
        using Bitmap shown = Draw(box);
        using Bitmap reference = Draw(made);

        Assert.False(SamePixels(dark, shown));
        Assert.True(SamePixels(shown, reference), "a shown box looks different from one made after the change");
    }

    // The box that is painted: the first and the last pixel that is not the background, inside the frame.
    private static Rectangle InkBounds(TextBox box)
    {
        using Bitmap picture = Draw(box);
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;

        // From the second column: the first one is the frame - a placeholder that touches it (instead of standing where a text would) is seen.
        for (int x = 1; x < box.Width - 3; x++)
        {
            for (int y = 3; y < box.Height - 3; y++)
            {
                if (picture.GetPixel(x, y).ToArgb() == box.BackColor.ToArgb())
                    continue;

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThePlaceholder_StandsWhereATextWouldStand(bool multiline)
    {
        TextBox typed = UIStyles.TextBoxes.CreateStandard("Place", "");
        TextBox hint = UIStyles.TextBoxes.CreateStandard("", "Place");
        typed.Multiline = multiline;
        hint.Multiline = multiline;
        typed.Height = multiline ? 60 : typed.Height;
        hint.Height = multiline ? 60 : hint.Height;
        _host.Show(typed);
        _host.Show(hint);

        Rectangle text = InkBounds(typed);
        Rectangle placeholder = InkBounds(hint);

        string where = "text " + text + ", placeholder " + placeholder;
        Assert.True(Math.Abs(text.X - placeholder.X) <= 1, "left: " + where);
        Assert.True(placeholder.X >= 2, "the placeholder touches the frame: " + where);
        Assert.True(Math.Abs(text.Y - placeholder.Y) <= 1, "top: " + where);
    }

    [Fact]
    public void AMultilineBox_ShowsThePlaceholderAtTheTop()
    {
        TextBox box = UIStyles.TextBoxes.CreateStandard("", "Type here");
        box.Multiline = true;
        box.Height = 80;
        _host.Show(box);

        Assert.True(HasInk(box));
    }

#if NET
    // The PlaceholderText of the standard box (.NET 5 and later) is the placeholder of the library, not the hint that the system draws in a gray of
    // its own: code that sets it (an application that was written for the standard box) gets the look of the library.
    [Fact]
    public void ThePlaceholderTextOfTheStandardBox_IsThePlaceholderOfTheLibrary_NotTheSystemHint()
    {
        TextBox box = _host.Show(UIStyles.TextBoxes.CreateStandard());
        TextBox reference = _host.Show(UIStyles.TextBoxes.CreateStandard("", "Type here"));

        box.PlaceholderText = "Type here";

        Assert.Equal("Type here", box.PlaceholderText);
        Assert.Equal("Type here", ((ErikwnkWFUI.Controls.TextField)box).Placeholder);
        Assert.Equal(string.Empty, box.Text);
        Assert.Equal(IntPtr.Zero, SystemHint(box));
        using Bitmap set = Draw(box);
        using Bitmap made = Draw(reference);
        Assert.True(SamePixels(set, made), "the hint looks different from the placeholder of the library");

        box.PlaceholderText = null!;
        Assert.Equal(string.Empty, box.PlaceholderText);
    }

    // A box that notes what the standard box asks it for while it paints (WM_PAINT).
    private sealed class ProbeField : ErikwnkWFUI.Controls.TextField
    {
        public readonly List<string> AskedWhilePainting = new List<string>();
        private bool _painting;

        public override string PlaceholderText
        {
            get
            {
                string value = base.PlaceholderText;

                if (_painting)
                    AskedWhilePainting.Add(value);

                return value;
            }
            set { base.PlaceholderText = value; }
        }

        protected override void WndProc(ref Message m)
        {
            _painting = m.Msg == 0x000F;

            try
            {
                base.WndProc(ref m);
            }
            finally
            {
                _painting = false;
            }
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    // The standard box paints a placeholder of its own as soon as its PlaceholderText says there is one (on the screen, in WM_PAINT): on
    // top of the one of the library there were two texts. What it asks for while it paints is empty; from outside the property still says it.
    [Fact]
    public void WhileTheStandardBoxPaints_ItIsToldThereIsNoPlaceholder_SoItPaintsNoneOfItsOwn()
    {
        ProbeField box = new ProbeField();
        box.Placeholder = "Type here";
        _host.Show(box);

        SendMessage(box.Handle, 0x000F, IntPtr.Zero, IntPtr.Zero);
        SendMessage(box.Handle, 0x000F, IntPtr.Zero, IntPtr.Zero);

        Assert.All(box.AskedWhilePainting, value => Assert.Equal(string.Empty, value));
        Assert.Equal("Type here", box.PlaceholderText);
        Assert.Equal("Type here", box.Placeholder);
    }
    // The hint that the system keeps for the box (EM_GETCUEBANNER): none, as the library paints the placeholder itself.
    private static IntPtr SystemHint(TextBox box)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder(64);
        SendMessageText(box.Handle, 0x1502, text, text.Capacity);

        return text.Length == 0 ? IntPtr.Zero : (IntPtr)1;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern IntPtr SendMessageText(IntPtr handle, int message, System.Text.StringBuilder text, int length);
#endif

    [Fact]
    public void ADisabledBox_StillShowsThePlaceholder()
    {
        TextBox box = UIStyles.TextBoxes.CreateStandard("", "Type here");
        box.Enabled = false;
        _host.Show(box);

        Assert.True(HasInk(box));
    }
}
