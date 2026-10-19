using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using TrackBar = ErikwnkWFUI.Controls.TrackBar;
using VolumeControl = ErikwnkWFUI.Controls.VolumeControl;

namespace ErikwnkWFUI.Tests.Controls.VolumeControls;

/// <summary>The volume control: the mute button and the slider as one (what a click, the slider and the keys do), the remembered volume, the events and the layout.</summary>
[Collection(AccentColorTestCollection.Name)]
public class VolumeControlTests : IDisposable
{
    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static Form Show(VolumeControl control)
    {
        Form host = OffscreenForm.Create(new Size(Math.Max(control.Width, 20), Math.Max(control.Height, 20)), Color.Black);
        host.Controls.Add(control);
        host.Show();
        control.PerformLayout();
        Application.DoEvents();

        return host;
    }

    // (PerformClick does nothing on a button that takes no focus.)
    private static void Click(VolumeControl control) => control.MuteButton.InvokePrivate("OnClick", EventArgs.Empty);

    private static MouseEventArgs Mouse(int x) => new MouseEventArgs(MouseButtons.Left, 1, x, 10, 0);

    // The slider is 106 px narrower than... its track starts 7 px from its left end: x of a value at the 150 px control.
    private static int XOf(VolumeControl control, double value)
    {
        TrackBar slider = control.Slider;

        return slider.Left + 7 + (int)Math.Round(value * (slider.Width - 14));
    }

    private static void SlideTo(VolumeControl control, params double[] values)
    {
        TrackBar slider = control.Slider;
        slider.InvokePrivate("OnMouseDown", Mouse(XOf(control, values[0]) - slider.Left));

        foreach (double value in values)
            slider.InvokePrivate("OnMouseMove", Mouse(XOf(control, value) - slider.Left));

        slider.InvokePrivate("OnMouseUp", Mouse(XOf(control, values[values.Length - 1]) - slider.Left));
    }

    // ---- what the factories make ----

    [Fact]
    public void TheFactoryMethods_MakeAControlWithAThePlaceForButtonAndSlider_AndTheVolumeGiven()
    {
        using VolumeControl standard = UIStyles.VolumeControls.CreateStandard();
        Assert.Equal(new Size(150, 30), standard.Size);
        Assert.Equal(1.0, standard.Volume);
        Assert.False(standard.IsMuted);
        Assert.Equal(1.0, standard.EffectiveVolume);
        Assert.Equal(1.0, standard.Slider.Value);
        Assert.True(standard.Slider.ShowValuePopup);
        Assert.Equal(UIStyles.Symbols.Volume, standard.MuteButton.Text);

        using VolumeControl quiet = UIStyles.VolumeControls.CreatePrimary(0.4, new Size(240, 40));
        Assert.Equal(new Size(240, 40), quiet.Size);
        Assert.Equal(0.4, quiet.Volume);
        Assert.Equal(0.4, quiet.Slider.Value);
    }

    [Fact]
    public void AControlStartedAtZero_IsMuted_AndTheVolumeIsTheOneThatComesBack()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.0);

        Assert.True(control.IsMuted);
        Assert.Equal(0.0, control.EffectiveVolume);
        Assert.Equal(0.0, control.Slider.Value);
        Assert.Equal(UIStyles.Symbols.Mute, control.MuteButton.Text);
        Assert.Equal(1.0, control.Volume);

        control.IsMuted = false;
        Assert.Equal(1.0, control.EffectiveVolume);
    }

    [Fact]
    public void Standard_IsGray_Primary_FollowsTheAccent_AndAColorSetColorsTheSlider()
    {
        using VolumeControl standard = UIStyles.VolumeControls.CreateStandard();
        using VolumeControl primary = UIStyles.VolumeControls.CreatePrimary();
        using VolumeControl yellow = UIStyles.VolumeControls.CreateStandard(0.5, UIStyles.Colors.Sets.Yellow);
        using VolumeControl single = UIStyles.VolumeControls.CreateStandard(0.5, Color.MediumPurple);

        Assert.Null(standard.Colors);
        Assert.Equal(UIColors.PrimarySet.Hover, primary.Colors.Hover);
        Assert.Same(UIStyles.Colors.Sets.Yellow, yellow.Colors);
        Assert.Equal(Color.MediumPurple, single.Colors.Hover);

        UIColors.SetAccent(Color.OrangeRed);
        Assert.Equal(UIColors.PrimarySet.Hover, primary.Colors.Hover);
    }

    // ---- the button ----

    [Fact]
    public void AClickOnTheButton_MutesAndBringsTheVolumeBack_AndTellsTheApplicationOnce()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.4);
        using Form host = Show(control);
        int events = 0;
        control.VolumeChanged += (s, e) => events++;

        Click(control);

        Assert.True(control.IsMuted);
        Assert.Equal(0.0, control.EffectiveVolume);
        Assert.Equal(0.0, control.Slider.Value);
        Assert.Equal(0.4, control.Volume);
        Assert.Equal(UIStyles.Symbols.Mute, control.MuteButton.Text);
        Assert.Equal(1, events);

        Click(control);

        Assert.False(control.IsMuted);
        Assert.Equal(0.4, control.EffectiveVolume);
        Assert.Equal(0.4, control.Slider.Value, 6);
        Assert.Equal(UIStyles.Symbols.Volume, control.MuteButton.Text);
        Assert.Equal(2, events);
    }

    [Fact]
    public void TheButtonTakesNoFocus_AndSaysWhatItDoes()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        using Form host = Show(control);

        Assert.False(control.MuteButton.TabStop);
        Assert.Equal("Mute", control.MuteButton.AccessibleName);

        Click(control);
        Assert.Equal("Unmute", control.MuteButton.AccessibleName);
    }

    [Fact]
    public void TheTooltipOfTheButton_NamesTheKeyM_AndFollowsTheState()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        using Form host = Show(control);

        Assert.Equal("Mute (M)", ButtonToolTipReader.Text(control.MuteButton));

        Click(control);
        Assert.Equal("Unmute (M)", ButtonToolTipReader.Text(control.MuteButton));
    }

    // ---- the slider ----

    [Fact]
    public void SlidingToZero_Mutes_AndMovingAwayFromZeroTurnsTheSoundOnAgain()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.6);
        using Form host = Show(control);
        int events = 0;
        control.VolumeChanged += (s, e) => events++;

        SlideTo(control, 0.6, 0.3, 0.0);

        Assert.True(control.IsMuted);
        Assert.Equal(0.0, control.EffectiveVolume);
        Assert.Equal(UIStyles.Symbols.Mute, control.MuteButton.Text);
        Assert.True(events >= 1);

        // From zero up: the sound is on at the level the slider shows.
        SlideTo(control, 0.0, 0.25);

        Assert.False(control.IsMuted);
        Assert.Equal(0.25, control.EffectiveVolume, 2);
        Assert.Equal(UIStyles.Symbols.Volume, control.MuteButton.Text);
    }

    [Fact]
    public void AfterASlideToZero_TheButtonBringsBackTheVolumeFromBeforeTheSlide_NotTheLastSliverOfIt()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.6);
        using Form host = Show(control);

        SlideTo(control, 0.6, 0.4, 0.2, 0.05, 0.0);
        Assert.True(control.IsMuted);

        Click(control);

        Assert.False(control.IsMuted);
        Assert.Equal(0.6, control.EffectiveVolume, 2);
        Assert.Equal(0.6, control.Slider.Value, 2);
    }

    [Fact]
    public void ASlideThatDoesNotEndAtZero_ChangesTheVolume_AndKeepsTheSoundOn()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.6);
        using Form host = Show(control);

        SlideTo(control, 0.6, 0.9, 0.2);

        Assert.False(control.IsMuted);
        Assert.Equal(0.2, control.Volume, 2);
        Assert.Equal(0.2, control.EffectiveVolume, 2);
    }

    // ---- the keys ----

    [Fact]
    public void TheKeyM_OnTheSlider_Mutes_AndOnlyWithoutModifiers()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
        using Form host = Show(control);

        control.Slider.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.M | Keys.Control));
        Assert.False(control.IsMuted);

        KeyEventArgs plain = new KeyEventArgs(Keys.M);
        control.Slider.InvokePrivate("OnKeyDown", plain);
        Assert.True(control.IsMuted);
        Assert.True(plain.Handled);

        control.Slider.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.M));
        Assert.False(control.IsMuted);
        Assert.Equal(0.5, control.EffectiveVolume, 6);
    }

    [Fact]
    public void TheArrowKeysDownToZero_Mute_AndTheNextMuteBringsBackTheLevel()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.1);
        using Form host = Show(control);
        control.Slider.SmallChange = 0.1;

        control.Slider.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.Left));

        Assert.True(control.IsMuted);
        Assert.Equal(0.0, control.EffectiveVolume);

        control.Slider.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.Right));
        Assert.False(control.IsMuted);
        Assert.Equal(0.1, control.EffectiveVolume, 6);
    }

    // ---- code ----

    [Fact]
    public void TheVolumeSetByCode_MovesTheSlider_WithoutAnEvent_AndDoesNotTurnTheSoundOn()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
        using Form host = Show(control);
        int events = 0;
        control.VolumeChanged += (s, e) => events++;

        control.Volume = 0.8;
        Assert.Equal(0.8, control.Slider.Value);

        control.IsMuted = true;
        control.Volume = 0.3;
        Assert.True(control.IsMuted);
        Assert.Equal(0.0, control.Slider.Value);

        control.IsMuted = false;
        Assert.Equal(0.3, control.EffectiveVolume);

        // 0 mutes and leaves the remembered level; more than 1 is 1; not a number is ignored.
        control.Volume = 0;
        Assert.True(control.IsMuted);
        control.Volume = double.NaN;
        control.IsMuted = false;
        Assert.Equal(0.3, control.Volume);
        control.Volume = 7;
        Assert.Equal(1.0, control.Volume);

        Assert.Equal(0, events);
    }

    // ---- state ----

    [Fact]
    public void ADisabledControl_ChangesNothing_AndItsPartsAreDisabled()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
        using Form host = Show(control);
        int events = 0;
        control.VolumeChanged += (s, e) => events++;

        control.Enabled = false;
        Assert.False(control.Slider.Enabled);
        Assert.False(control.MuteButton.Enabled);

        control.Slider.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.M));
        Click(control);

        Assert.False(control.IsMuted);
        Assert.Equal(0, events);
    }

    // ---- layout ----

    [Fact]
    public void TheButtonIsAsHighAsTheControl_AndTheSliderTakesTheRest()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
        using Form host = Show(control);

        Assert.Equal(new Size(30, 30), control.MuteButton.Size);
        Assert.Equal(0, control.MuteButton.Left);
        Assert.True(control.Slider.Left >= control.MuteButton.Right);
        Assert.Equal(control.Width, control.Slider.Right);
        Assert.InRange(control.Slider.Top + control.Slider.Height / 2, control.Height / 2 - 1, control.Height / 2 + 1);
    }

    [Fact]
    public void AControlThatIsResized_GetsAButtonOfItsHeight_WithTheSameSymbolAndState()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
        using Form host = Show(control);
        Click(control);

        control.Size = new Size(300, 50);
        control.PerformLayout();

        Assert.Equal(new Size(50, 50), control.MuteButton.Size);
        Assert.Equal(UIStyles.Symbols.Mute, control.MuteButton.Text);
        Assert.Equal("Unmute", control.MuteButton.AccessibleName);

        Click(control);
        Assert.False(control.IsMuted);
        Assert.Equal(0.5, control.EffectiveVolume, 6);

        // Only one mute button is left in the control (the old one is disposed).
        Assert.Single(control.Controls.OfType<Button>());
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(40, 30)]
    [InlineData(300, 8)]
    public void EverySize_CanBeLaidOutAndDrawn(int width, int height)
    {
        using VolumeControl control = UIStyles.VolumeControls.CreatePrimary(0.5, new Size(width, height));
        using Form host = Show(control);
        using Bitmap picture = new Bitmap(Math.Max(1, control.Width), Math.Max(1, control.Height));

        control.DrawToBitmap(picture, new Rectangle(Point.Empty, control.Size));

        Assert.True(control.MuteButton.Width >= 1);
        Assert.True(control.Slider.Width >= 1);
    }

    // ---- life ----

    [Fact]
    public void ADisposedControl_NoLongerListensForTheLanguage()
    {
        VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        Form host = Show(control);
        host.Dispose();
        control.Dispose();

        Assert.Null(Record.Exception(() =>
        {
            ErikwnkWFUI.Localization.UIStrings.Language = ErikwnkWFUI.Localization.UIStrings.Language == UILanguage.German ? UILanguage.English : UILanguage.German;
            ErikwnkWFUI.Localization.UIStrings.Language = UILanguage.English;
        }));
    }
}
