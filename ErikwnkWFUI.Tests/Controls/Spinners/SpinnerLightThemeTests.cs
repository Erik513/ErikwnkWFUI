using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.Spinners;

/// <summary>What is really painted in the light theme (not the colors that are worked out), and that a spinner follows a change of the theme while it is shown.</summary>
[Collection(AccentColorTestCollection.Name)]
public class SpinnerLightThemeTests : IDisposable
{
    private static readonly Color LightSurface = Color.FromArgb(243, 243, 243);

    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    public static IEnumerable<object[]> Variants()
    {
        yield return new object[] { "Standard", (Func<Spinner>)(() => UIStyles.Spinners.CreateStandard(60, 8)) };
        yield return new object[] { "Primary", (Func<Spinner>)(() => UIStyles.Spinners.CreatePrimary(60, 8)) };
        yield return new object[] { "Yellow", (Func<Spinner>)(() => UIStyles.Spinners.CreateStandard(60, 8, UIStyles.Colors.Sets.Yellow)) };
        yield return new object[] { "Green", (Func<Spinner>)(() => UIStyles.Spinners.CreateGreen(60, 8)) };
        yield return new object[] { "Status", (Func<Spinner>)(() => UIStyles.Spinners.CreateStatus(60, 8)) };
        yield return new object[] { "Disabled", (Func<Spinner>)(() =>
        {
            Spinner spinner = UIStyles.Spinners.CreatePrimary(60, 8);
            spinner.Enabled = false;

            return spinner;
        }) };
    }

    // A known load of half: from the top clockwise, so the right side of the ring is the arc and the left side the track. Stopped, so
    // that two pictures are the same.
    private static Form Show(Spinner spinner, Color surface)
    {
        spinner.IsIndeterminate = false;
        spinner.IsSpinning = false;
        spinner.Value = 50;
        spinner.SetBounds(0, 0, 60, 60);
        Form form = OffscreenForm.Create(new Size(60, 60), surface);
        form.Controls.Add(spinner);
        form.Show();
        Application.DoEvents();

        return form;
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void TheArcAndTheTrack_AreSeenOnALightSurface(string name, Func<Spinner> make)
    {
        UIColors.ApplyTheme(UIThemes.Light);
        using Spinner spinner = make();
        using Form form = Show(spinner, LightSurface);
        using Bitmap picture = Draw(spinner);

        Color arc = picture.GetPixel(55, 30);
        Color track = picture.GetPixel(5, 30);

        Assert.Equal(spinner.InvokePrivate<Color>("ResolveArcColor").ToArgb(), arc.ToArgb());
        Assert.Equal(spinner.GetTrackColor().ToArgb(), track.ToArgb());
        Assert.True(ColorContrastHelper.GetContrastRatio(track, LightSurface) >= 1.4, name + ": the track is lost on the surface");
        Assert.True(ColorContrastHelper.GetContrastRatio(arc, LightSurface) >= 2.0, name + ": the arc is lost on the surface");
        Assert.True(ColorContrastHelper.GetContrastRatio(arc, track) >= 2.9, name + ": the arc does not stand out from the track");
    }

    [Fact]
    public void TheDigitsAndTheText_AreDarkOnALightSurface()
    {
        UIColors.ApplyTheme(UIThemes.Light);
        using Spinner spinner = UIStyles.Spinners.CreateProgressPrimary(60, 8);
        spinner.Text = "Loading data";
        spinner.Width = 200;
        using Form form = OffscreenForm.Create(new Size(200, 60), LightSurface);
        spinner.IsIndeterminate = false;
        spinner.IsSpinning = false;
        spinner.Value = 50;
        spinner.Top = 0;
        form.Controls.Add(spinner);
        form.Show();
        Application.DoEvents();
        using Bitmap picture = Draw(spinner);

        double Strongest(Rectangle area)
        {
            double best = 0;

            for (int x = area.Left; x < area.Right; x++)
            {
                for (int y = area.Top; y < area.Bottom; y++)
                    best = Math.Max(best, ColorContrastHelper.GetContrastRatio(picture.GetPixel(x, y), LightSurface));
            }

            return best;
        }

        Assert.True(Strongest(new Rectangle(22, 22, 16, 16)) >= 4.5, "the digits are faint");
        Assert.True(Strongest(new Rectangle(70, 10, 120, 40)) >= 4.5, "the text is faint");
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void AShownSpinner_LooksAsOneMadeInTheLightTheme_AfterTheSwitch(string name, Func<Spinner> make)
    {
        using Spinner shown = make();
        using Form form = Show(shown, LightSurface);
        int paints = 0;
        shown.Invalidated += (s, e) => paints++;

        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();
        shown.Update();

        using Spinner made = make();
        using Form madeForm = Show(made, LightSurface);

        Assert.True(paints > 0, name + ": not told to paint again");
        using Bitmap first = Draw(shown);
        using Bitmap second = Draw(made);
        Assert.True(SamePixels(first, second), name + ": looks different from one made after the switch");
    }
}
