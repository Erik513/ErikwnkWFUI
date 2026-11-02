using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using VolumeControl = ErikwnkWFUI.Controls.VolumeControl;

namespace ErikwnkWFUI.Tests.Controls.VolumeControls;

/// <summary>What is really painted in the light theme: the symbol of the button and the slider are seen on a light surface, and a control that is shown follows a change of the theme.</summary>
[Collection(AccentColorTestCollection.Name)]
public class VolumeControlLightThemeTests : IDisposable
{
    private static readonly Color LightSurface = Color.FromArgb(243, 243, 243);

    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    public static IEnumerable<object[]> Variants()
    {
        yield return new object[] { "Standard", (Func<VolumeControl>)(() => UIStyles.VolumeControls.CreateStandard(0.5)) };
        yield return new object[] { "Primary", (Func<VolumeControl>)(() => UIStyles.VolumeControls.CreatePrimary(0.5)) };
        yield return new object[] { "Yellow", (Func<VolumeControl>)(() => UIStyles.VolumeControls.CreateStandard(0.5, UIStyles.Colors.Sets.Yellow)) };
        yield return new object[] { "Percentage", (Func<VolumeControl>)(() => UIStyles.VolumeControls.CreatePrimary(0.5, showPercentage: true)) };
        yield return new object[] { "Muted", (Func<VolumeControl>)(() =>
        {
            VolumeControl control = UIStyles.VolumeControls.CreatePrimary(0.5);
            control.IsMuted = true;

            return control;
        }) };
        yield return new object[] { "Disabled", (Func<VolumeControl>)(() =>
        {
            VolumeControl control = UIStyles.VolumeControls.CreatePrimary(0.5);
            control.Enabled = false;

            return control;
        }) };
    }

    private static Form Show(VolumeControl control, Color surface)
    {
        control.Slider.TabStop = false;    // no focus cue (a bigger thumb) on only one of two controls that are compared
        return OffscreenForm.Show(control, surface: surface);
    }

    // The most different color of the area from the surface: what the symbol (or the thumb) is painted in.
    private static double StrongestContrast(Bitmap picture, Rectangle area, Color surface)
    {
        double strongest = 1.0;

        for (int x = area.Left; x < area.Right; x++)
        {
            for (int y = area.Top; y < area.Bottom; y++)
                strongest = Math.Max(strongest, ColorContrastHelper.GetContrastRatio(picture.GetPixel(x, y), surface));
        }

        return strongest;
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void TheSymbolAndTheSlider_AreSeenOnALightSurface(string name, Func<VolumeControl> make)
    {
        UIColors.ApplyTheme(UIThemes.Light);
        using VolumeControl control = make();
        using Form form = Show(control, LightSurface);
        using Bitmap picture = Draw(control);

        // (A disabled one is meant to be faint; it must still be there.)
        double symbolMinimum = control.Enabled ? 3.0 : 1.4;
        double symbol = StrongestContrast(picture, control.MuteButton.Bounds, LightSurface);
        double slider = StrongestContrast(picture, control.Slider.Bounds, LightSurface);

        Assert.True(symbol >= symbolMinimum, name + ": the symbol is lost on the surface (" + symbol.ToString("F2") + ":1)");
        Assert.True(slider >= (control.Enabled ? 2.4 : 1.4), name + ": the slider is lost on the surface (" + slider.ToString("F2") + ":1)");
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void AShownControl_LooksAsOneMadeInTheLightTheme_AfterTheSwitch(string name, Func<VolumeControl> make)
    {
        using VolumeControl shown = make();
        using Form form = Show(shown, LightSurface);
        int sliderPaints = 0;
        int buttonPaints = 0;
        shown.Slider.Invalidated += (s, e) => sliderPaints++;
        shown.MuteButton.Invalidated += (s, e) => buttonPaints++;

        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();
        shown.Update();

        using VolumeControl made = make();
        using Form madeForm = Show(made, LightSurface);

        Assert.True(sliderPaints > 0, name + ": the slider is not told to paint again");
        Assert.True(buttonPaints > 0, name + ": the button is not told to paint again");

        using Bitmap first = Draw(shown);
        using Bitmap second = Draw(made);
        Assert.True(SamePixels(first, second), name + ": looks different from one made after the switch");
    }
}
