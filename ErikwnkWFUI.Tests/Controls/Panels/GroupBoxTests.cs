using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.Panels;

/// <summary>The group box: its frame, the title, the colors and what it keeps of the standard control.</summary>
[Collection(AccentColorTestCollection.Name)]
public class GroupBoxTests : IDisposable
{
    private readonly List<Form> _hosts = new List<Form>();

    public void Dispose()
    {
        foreach (Form host in _hosts)
            host.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private ErikwnkWFUI.Controls.GroupBox Shown(ErikwnkWFUI.Controls.GroupBox box)
    {
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(260, 140), BackColor = UIColors.BackgroundMedium };
        box.SetBounds(10, 10, 220, 110);
        host.Controls.Add(box);
        host.Show();
        _hosts.Add(host);

        return box;
    }

    [Fact]
    public void TheFactory_GivesALibraryGroupBox_WithItsTitle()
    {
        using System.Windows.Forms.GroupBox box = UIStyles.GroupBoxes.CreateStandard("Options");

        Assert.IsType<ErikwnkWFUI.Controls.GroupBox>(box);
        Assert.Equal("Options", box.Text);
    }

    [Fact]
    public void TheChildren_SitBelowTheTitle_AndAreLaidOutAsInTheStandardBox()
    {
        ErikwnkWFUI.Controls.GroupBox box = Shown((ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("Options"));
        using System.Windows.Forms.Button child = new System.Windows.Forms.Button { Dock = DockStyle.Top };
        box.Controls.Add(child);

        Assert.True(box.DisplayRectangle.Top >= box.Font.Height / 2);
        Assert.Equal(box.DisplayRectangle.Top, child.Top);
    }

    [Fact]
    public void TheFrameColor_FollowsTheTheme_UntilItIsSet_AndEmptyGivesThatBack()
    {
        ErikwnkWFUI.Controls.GroupBox box = (ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("T");
        Shown(box);

        UIColors.ApplyTheme(UIThemes.Dark);
        Color dark = box.BorderColor;
        UIColors.ApplyTheme(UIThemes.Light);
        Assert.NotEqual(dark, box.BorderColor);

        box.BorderColor = Color.Red;
        UIColors.ApplyTheme(UIThemes.Dark);
        Assert.Equal(Color.Red, box.BorderColor);

        box.BorderColor = Color.Empty;
        Assert.Equal(dark, box.BorderColor);
    }

    [Fact]
    public void TheTitleColor_IsSetAndResetLikeTheFrame()
    {
        ErikwnkWFUI.Controls.GroupBox box = (ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("T");

        // (On a page of the theme: the color of the title is read against the surface it stands on.)
        using Panel page = new Panel { BackColor = UIColors.BackgroundMedium };
        page.Controls.Add(box);

        Assert.Equal(UIColors.TextPrimary, box.TitleColor);

        box.TitleColor = Color.Orange;
        Assert.Equal(Color.Orange, box.TitleColor);

        box.TitleColor = Color.Empty;
        Assert.Equal(UIColors.TextPrimary, box.TitleColor);
        box.Dispose();
    }

    [Fact]
    public void ASet_ColorsTheFrame_AndThePrimaryOneFollowsTheAccent()
    {
        ErikwnkWFUI.Controls.GroupBox plain = (ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("T");
        ErikwnkWFUI.Controls.GroupBox green = (ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("T", UIStyles.Colors.Sets.Green);
        ErikwnkWFUI.Controls.GroupBox primary = (ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreatePrimary("T");
        Shown(plain);
        Shown(green);
        Shown(primary);

        Assert.NotEqual(plain.BorderColor, green.BorderColor);
        Assert.NotEqual(plain.BorderColor, primary.BorderColor);

        UIColors.SetAccent(UIAccentColors.Red);
        try
        {
            Color red = primary.BorderColor;
            UIColors.SetAccent(UIAccentColors.Blue);
            Assert.NotEqual(red, primary.BorderColor);
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Fact]
    public void TheFrameIsSeenOnEverySurface_InBothThemes()
    {
        foreach (UIColorTheme theme in new[] { UIThemes.Dark, UIThemes.Light })
        {
            UIColors.ApplyTheme(theme);

            foreach (Color surface in new[] { Color.Black, Color.White, UIStyles.Colors.Sets.Green.Basic, UIStyles.Colors.Sets.Yellow.Basic })
            {
                ErikwnkWFUI.Controls.GroupBox box = (ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("T");
                Shown(box);
                box.BackColor = surface;

                Assert.True(ColorContrastHelper.GetContrastRatio(box.BorderColor, surface) >= 1.45, $"{surface} -> {box.BorderColor}");
            }
        }
    }

    [Fact]
    public void ADisabledBox_LooksDifferent_AndAnEmptyTitleStillPaints()
    {
        ErikwnkWFUI.Controls.GroupBox enabled = Shown((ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("Title"));
        ErikwnkWFUI.Controls.GroupBox disabled = Shown((ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("Title"));
        disabled.Enabled = false;

        using Bitmap a = Draw(enabled);
        using Bitmap b = Draw(disabled);
        Assert.False(SamePixels(a, b));

        enabled.Text = "";
        using Bitmap noTitle = Draw(enabled);
        Assert.False(SamePixels(a, noTitle));
    }

    [Fact]
    public void TheFrame_HasAGapBehindTheTitle_AndNoneWithoutATitle()
    {
        ErikwnkWFUI.Controls.GroupBox titled = Shown((ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("Title"));
        ErikwnkWFUI.Controls.GroupBox plain = Shown((ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard(""));
        Color surface = titled.BackColor;

        // The upper edge of the frame is at half the height of a line of text; the title starts 10 px (at 96 dpi) from the left, and the gap
        // begins 4 px before it: the pixel is in the gap, left of the first letter.
        int edge = titled.Font.Height / 2;
        int inside = (int)(titled.DeviceDpi / 96.0 * 8);

        using Bitmap withTitle = Draw(titled);
        using Bitmap without = Draw(plain);

        Assert.Equal(surface.ToArgb(), withTitle.GetPixel(inside, edge).ToArgb());
        Assert.NotEqual(surface.ToArgb(), without.GetPixel(inside, edge).ToArgb());

        // Further right, past the title, the frame is there again.
        Assert.NotEqual(surface.ToArgb(), withTitle.GetPixel(titled.Width - 30, edge).ToArgb());
    }

    [Fact]
    public void ATinyBoxOrAVeryLongTitle_DoesNotThrow()
    {
        ErikwnkWFUI.Controls.GroupBox box = Shown((ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard(new string('W', 300)));

        using Bitmap longTitle = Draw(box);
        box.Size = new Size(1, 1);
        using Bitmap tiny = Draw(box);
        box.Size = new Size(0, 0);
        box.Invalidate();
    }

    [Fact]
    public void ARadioButtonInTheBox_FormsAGroupOfItsOwn()
    {
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false };
        _hosts.Add(host);
        System.Windows.Forms.GroupBox box = UIStyles.GroupBoxes.CreateStandard("T");
        System.Windows.Forms.RadioButton inside = UIStyles.RadioButtons.CreateStandard("In", true);
        System.Windows.Forms.RadioButton outside = UIStyles.RadioButtons.CreateStandard("Out", true);
        box.Controls.Add(inside);
        host.Controls.Add(box);
        host.Controls.Add(outside);
        host.Show();

        Assert.True(inside.Checked);
        Assert.True(outside.Checked);
    }

    [Fact]
    public void TheTitle_IsReadableOnASurfaceOfYourOwn_IsTheThemeColorOnThePageOfTheTheme_AndAColorThatWasSetStays()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        ErikwnkWFUI.Controls.GroupBox box = (ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("Title");

        // (A panel of your own: bright in the dark theme, and a mid gray, where light text alone does not reach 4.5:1.)
        foreach (Color surface in new[] { Color.White, Color.FromArgb(128, 128, 128), Color.FromArgb(230, 190, 20) })
        {
            using Panel page = new Panel { BackColor = surface };
            page.Controls.Add(box);

            Assert.True(UIColors.GetContrastRatio(box.TitleColor, surface) >= 4.5, "the title is not readable on " + surface);
        }

        using Panel themed = new Panel { BackColor = UIColors.BackgroundMedium };
        themed.Controls.Add(box);
        Assert.Equal(UIColors.TextPrimary, box.TitleColor);

        box.TitleColor = Color.Red;
        Assert.Equal(Color.Red, box.TitleColor);
        box.Dispose();
    }

    [Fact]
    public void TheDesignerAsks_WhetherTheColorsWereSet_AndCanResetThem()
    {
        using ErikwnkWFUI.Controls.GroupBox box = (ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("T");

        Assert.False(box.InvokePrivate<bool>("ShouldSerializeTitleColor"));
        Assert.False(box.InvokePrivate<bool>("ShouldSerializeBorderColor"));

        box.TitleColor = Color.Red;
        box.BorderColor = Color.Blue;
        Assert.True(box.InvokePrivate<bool>("ShouldSerializeTitleColor"));
        Assert.True(box.InvokePrivate<bool>("ShouldSerializeBorderColor"));

        box.InvokePrivate("ResetTitleColor");
        box.InvokePrivate("ResetBorderColor");

        Assert.False(box.InvokePrivate<bool>("ShouldSerializeTitleColor"));
        Assert.False(box.InvokePrivate<bool>("ShouldSerializeBorderColor"));
    }

    [Fact]
    public void AnotherFont_IsPaintedAtOnce_AsAnotherPicture()
    {
        ErikwnkWFUI.Controls.GroupBox box = Shown((ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("Title"));
        using Bitmap before = Draw(box);
        Font original = box.Font;
        using Font big = new Font("Segoe UI", 16f);

        box.Font = big;
        using Bitmap after = Draw(box);
        box.Font = original;

        Assert.False(SamePixels(before, after));
    }
}
