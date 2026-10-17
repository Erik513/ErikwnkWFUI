using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using TrackBar = ErikwnkWFUI.Controls.TrackBar;

namespace ErikwnkWFUI.Tests.Controls.TrackBars;

/// <summary>What is really painted in the light theme (not the colors that are worked out), that a slider follows a change of the theme while it is shown, and that its popup can be read in both themes.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TrackBarLightThemeTests : IDisposable
{
    private static readonly Color LightSurface = Color.FromArgb(243, 243, 243);

    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    public static IEnumerable<object[]> Variants()
    {
        yield return new object[] { "Standard", (Func<TrackBar>)(() => UIStyles.TrackBars.CreateStandard(0.5)) };
        yield return new object[] { "Primary", (Func<TrackBar>)(() => UIStyles.TrackBars.CreatePrimary(0.5)) };
        yield return new object[] { "Yellow", (Func<TrackBar>)(() => UIStyles.TrackBars.CreateStandard(0.5, 1.0, UIStyles.Colors.Sets.Yellow)) };
        yield return new object[] { "Green", (Func<TrackBar>)(() => UIStyles.TrackBars.CreateStandard(0.5, 1.0, UIStyles.Colors.Sets.Green)) };
        yield return new object[] { "Disabled", (Func<TrackBar>)(() =>
        {
            TrackBar bar = UIStyles.TrackBars.CreatePrimary(0.5);
            bar.Enabled = false;

            return bar;
        }) };
    }

    private static Form Show(TrackBar bar, Color surface)
    {
        bar.TabStop = false;    // the form shown last is the active one: no focus cue (a bigger thumb) on only one of two
        bar.SetBounds(0, 0, 100, 20);
        Form form = OffscreenForm.Create(new Size(100, 20), surface);
        form.Controls.Add(bar);
        form.Show();
        Application.DoEvents();

        return form;
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void TheTrackTheFillAndTheThumb_AreSeenOnALightSurface(string name, Func<TrackBar> make)
    {
        UIColors.ApplyTheme(UIThemes.Light);
        using TrackBar bar = make();
        using Form form = Show(bar, LightSurface);
        using Bitmap picture = Draw(bar);

        Color fill = picture.GetPixel(25, 10);
        Color track = picture.GetPixel(85, 10);

        Assert.Equal(bar.GetFillColor().ToArgb(), fill.ToArgb());
        Assert.Equal(bar.GetTrackColor().ToArgb(), track.ToArgb());

        if (bar.Enabled)
        {
            Color thumb = picture.GetPixel(50, 10);
            Assert.Equal(bar.GetFillColor().ToArgb(), thumb.ToArgb());
            Assert.True(ColorContrastHelper.GetContrastRatio(thumb, LightSurface) >= 2.4, name + ": the thumb is lost on the surface");
        }

        Assert.True(ColorContrastHelper.GetContrastRatio(track, LightSurface) >= 1.4, name + ": the track is lost on the surface");
        Assert.True(ColorContrastHelper.GetContrastRatio(fill, LightSurface) >= 2.4, name + ": the filled part is lost on the surface");
        Assert.True(ColorContrastHelper.GetContrastRatio(fill, track) >= 1.4, name + ": the filled part does not stand out from the track");
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void AShownSlider_LooksAsOneMadeInTheLightTheme_AfterTheSwitch(string name, Func<TrackBar> make)
    {
        using TrackBar shown = make();
        using Form form = Show(shown, LightSurface);
        int paints = 0;
        shown.Invalidated += (s, e) => paints++;

        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();
        shown.Update();

        using TrackBar made = make();
        using Form madeForm = Show(made, LightSurface);

        Assert.True(paints > 0, name + ": not told to paint again");
        using Bitmap first = Draw(shown);
        using Bitmap second = Draw(made);
        Assert.True(SamePixels(first, second), name + ": looks different from one made after the switch");
    }

    [Fact]
    public void TheThumbGrowsWithTheFocus_AndTheSliderIsRepaintedForIt()
    {
        UIColors.ApplyTheme(UIThemes.Light);
        using TrackBar bar = UIStyles.TrackBars.CreatePrimary(0.5);
        using Form form = Show(bar, LightSurface);
        using Bitmap resting = Draw(bar);
        int paints = 0;
        bar.Invalidated += (s, e) => paints++;

        bar.InvokePrivate("OnGotFocus", EventArgs.Empty);
        Assert.True(paints > 0, "no repaint when the slider gets the focus");

        bar.InvokePrivate("OnLostFocus", EventArgs.Empty);
        Assert.True(paints > 1, "no repaint when the slider loses the focus");
    }

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void ThePopupText_CanBeRead_ForTheColorsOfTheSlider_InBothThemes(bool lightTheme)
    {
        Func<Form, bool> before = PopupSupport.OwnerIsActive;
        PopupSupport.OwnerIsActive = _ => true;

        ColorContrastTestSupport.Run(lightTheme, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in ColorContrastTestSupport.AllSets(includeDark: true))
            {
                using TrackBar bar = UIStyles.TrackBars.CreateStandard(0.5, 1.0, set, showValuePopup: true);
                using Form host = OffscreenForm.Create(new Size(100, 20), Color.Black);
                bar.SetBounds(0, 0, 100, 20);
                host.Controls.Add(bar);
                host.Show();
                Application.DoEvents();
                bar.InvokePrivate("OnMouseEnter", EventArgs.Empty);
                InfoPopupForm popup = bar.GetPrivateField<InfoPopupForm>("_popup")!;

                Color text = popup.InvokePrivate<Color>("GetForeColor");
                Color back = popup.InvokePrivate<Color>("GetBackColor");

                ColorContrastTestSupport.Check(shortfalls, "popup text for " + name, text, back, 4.5);
            }
        });

        PopupSupport.OwnerIsActive = before;
    }
}
