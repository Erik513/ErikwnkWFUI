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
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(260, 140) };
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
}