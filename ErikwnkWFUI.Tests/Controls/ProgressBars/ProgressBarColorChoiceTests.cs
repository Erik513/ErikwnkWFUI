using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.ProgressBars;

/// <summary>Every color of the progress bars can be chosen on its own: the border, the text on the track and on the fill, the text of the slim bar.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ProgressBarColorChoiceTests : IDisposable
{
    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private static ErikwnkWFUI.Controls.ProgressBar Thick(Color surface)
    {
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
        bar.Value = 50;
        bar.Text = "Uploading the file";
        bar.TextAlign = HorizontalAlignment.Center;

        return bar;
    }

    private static (System.Windows.Forms.Panel Panel, Form Host) OnPanel(Control bar, Color surface, int height)
    {
        System.Windows.Forms.Panel panel = new System.Windows.Forms.Panel { BackColor = surface, Size = new Size(300, height + 10) };
        bar.SetBounds(0, 5, 300, height);
        panel.Controls.Add(bar);
        Form host = OffscreenForm.Create(panel.Size, surface);
        host.Controls.Add(panel);
        host.Show();
        Application.DoEvents();

        return (panel, host);
    }

    // Whether a picture has a pixel of that strong color in a region (the letters are drawn smooth: look for the color, not for one exact value).
    private static bool Has(Bitmap picture, Rectangle region, Func<Color, bool> strong)
    {
        for (int y = region.Top; y < region.Bottom; y++)
            for (int x = region.Left; x < region.Right; x++)
                if (strong(picture.GetPixel(x, y)))
                    return true;

        return false;
    }

    private static bool IsRed(Color c) => c.R > 200 && c.G < 90 && c.B < 90;

    private static bool IsLime(Color c) => c.G > 200 && c.R < 90 && c.B < 90;

    // --- The border ---

    [Fact]
    public void TheBorderColor_IsTheQuietLineOfThePanel_UntilOneIsSet_AndFollowsThePanel()
    {
        ErikwnkWFUI.Controls.ProgressBar bar = Thick(Color.Black);
        (System.Windows.Forms.Panel panel, Form host) = OnPanel(bar, UIColors.BackgroundLight, 23);

        using (host)
        {
            Assert.Equal(UIColors.GetLineColor(UIColors.BackgroundLight), bar.BorderColor);

            panel.BackColor = Color.Purple;
            Assert.Equal(UIColors.GetLineColor(Color.Purple), bar.BorderColor);

            bar.BorderColor = Color.Orange;
            panel.BackColor = UIColors.BackgroundDark;
            Assert.Equal(Color.Orange, bar.BorderColor);

            using (Bitmap picture = Draw(bar))
                Assert.Equal(Color.Orange.ToArgb(), picture.GetPixel(0, 11).ToArgb());

            bar.BorderColor = Color.Empty;
            Assert.Equal(UIColors.GetLineColor(UIColors.BackgroundDark), bar.BorderColor);
        }
    }

    [Fact]
    public void TheBorderColor_OnlyMattersWithABorder_AndTheDesignerWritesItOnlyWhenSet()
    {
        ErikwnkWFUI.Controls.ProgressBar bar = Thick(Color.Black);
        (System.Windows.Forms.Panel panel, Form host) = OnPanel(bar, UIColors.BackgroundLight, 23);

        using (host)
        {
            bar.BorderColor = Color.Orange;
            bar.BorderStyle = BorderStyle.None;
            Assert.Equal(bar.BackColor, bar.EdgeColor);

            bar.BorderStyle = BorderStyle.FixedSingle;
            Assert.Equal(Color.Orange, bar.EdgeColor);
        }

        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(bar)["BorderColor"]!;
        bar.BorderColor = Color.Empty;
        Assert.False(descriptor.ShouldSerializeValue(bar));
        descriptor.SetValue(bar, Color.Teal);
        Assert.True(descriptor.ShouldSerializeValue(bar));
        descriptor.ResetValue(bar);
        Assert.False(descriptor.ShouldSerializeValue(bar));
    }

    // --- The text ---

    [Fact]
    public void TheTextOfTheBar_HasTheColorsThatWereChosen_OnTheTrackAndOnTheFill()
    {
        ErikwnkWFUI.Controls.ProgressBar bar = Thick(Color.Black);
        bar.Value = 50;
        (System.Windows.Forms.Panel panel, Form host) = OnPanel(bar, Color.Black, 23);

        using (host)
        {
            // The text is centered on the middle of the bar, where the fill ends: both colors are seen.
            Rectangle trackSide = new Rectangle(152, 2, 140, 19);
            Rectangle fillSide = new Rectangle(8, 2, 140, 19);

            using (Bitmap automatic = Draw(bar))
            {
                Assert.False(Has(automatic, trackSide, IsRed), "a red text without being asked");
                Assert.False(Has(automatic, fillSide, IsLime), "a lime text without being asked");
            }

            bar.TextColor = Color.Red;
            bar.TextOnFillColor = Color.Lime;
            Application.DoEvents();

            using (Bitmap chosen = Draw(bar))
            {
                Assert.True(Has(chosen, trackSide, IsRed), "the text on the track is not in the color that was chosen");
                Assert.True(Has(chosen, fillSide, IsLime), "the text on the fill is not in the color that was chosen");
            }

            // A disabled bar has the quiet text of the library, whatever was chosen.
            bar.Enabled = false;
            Application.DoEvents();

            using (Bitmap disabled = Draw(bar))
            {
                Assert.False(Has(disabled, trackSide, IsRed));
                Assert.False(Has(disabled, fillSide, IsLime));
            }

            bar.Enabled = true;
            bar.TextColor = Color.Empty;
            bar.TextOnFillColor = Color.Empty;
            Application.DoEvents();

            using (Bitmap back = Draw(bar))
            {
                Assert.False(Has(back, trackSide, IsRed));
                Assert.False(Has(back, fillSide, IsLime));
            }
        }
    }

    [Fact]
    public void TheTextOfTheSlimBar_HasTheColorThatWasChosen_UntilItIsGivenBack()
    {
        SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        slim.Value = 50;
        slim.Text = "Uploading the file";
        slim.ShowPercentage = true;
        (System.Windows.Forms.Panel panel, Form host) = OnPanel(slim, Color.Black, 26);

        using (host)
        {
            Rectangle text = new Rectangle(0, 0, 150, 14);

            using (Bitmap automatic = Draw(panel))
                Assert.False(Has(automatic, text, IsRed));

            slim.TextColor = Color.Red;
            Application.DoEvents();

            using (Bitmap chosen = Draw(panel))
                Assert.True(Has(chosen, text, IsRed), "the text is not in the color that was chosen");

            slim.Enabled = false;
            Application.DoEvents();

            using (Bitmap disabled = Draw(panel))
                Assert.False(Has(disabled, text, IsRed));

            slim.Enabled = true;
            slim.TextColor = Color.Empty;
            Application.DoEvents();

            using (Bitmap back = Draw(panel))
                Assert.False(Has(back, text, IsRed));
        }
    }

    [Fact]
    public void TheDefaultColorsOfTheText_AreNotWrittenByTheDesigner()
    {
        ErikwnkWFUI.Controls.ProgressBar thick = Thick(Color.Black);
        SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();

        foreach ((Control bar, string property) in new (Control, string)[] { (thick, "TextColor"), (thick, "TextOnFillColor"), (slim, "TextColor") })
        {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(bar)[property]!;

            Assert.False(descriptor.ShouldSerializeValue(bar), property + " is written by default");

            descriptor.SetValue(bar, Color.Teal);
            Assert.True(descriptor.ShouldSerializeValue(bar));

            descriptor.ResetValue(bar);
            Assert.False(descriptor.ShouldSerializeValue(bar));
        }
    }

    // --- A screen reader ---

    [Fact]
    public void TheSlimBar_GivesAScreenReaderItsValue_ItsRole_AndItsText()
    {
        SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        slim.Value = 42;
        slim.Text = "Uploading";
        using Form host = OffscreenForm.Create(new Size(200, 40), Color.Black);
        host.Controls.Add(slim);
        host.Show();

        AccessibleObject accessible = slim.AccessibilityObject;

        Assert.Equal(AccessibleRole.ProgressBar, accessible.Role);
        Assert.Equal("42%", accessible.Value);
        Assert.Equal("Uploading", accessible.Name);

        slim.Value = 80;
        Assert.Equal("80%", slim.AccessibilityObject.Value);

        // A name that was set stays the name.
        slim.AccessibleName = "Upload";
        Assert.Equal("Upload", slim.AccessibilityObject.Name);

        // The marquee has no value.
        slim.Style = ProgressBarStyle.Marquee;
        Assert.Null(slim.AccessibilityObject.Value);
    }
}
