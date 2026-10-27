using System.Drawing;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using TrackBar = ErikwnkWFUI.Controls.TrackBar;
using VolumeControl = ErikwnkWFUI.Controls.VolumeControl;

namespace ErikwnkWFUI.Tests.Controls.VolumeControls;

/// <summary>The volume control: the mouse wheel over the button, and the percentage beside the slider.</summary>
[Collection(LanguageTestCollection.Name)]
public class VolumeControlWheelAndPercentageTests
{
    private static Form Show(VolumeControl control)
    {
        return OffscreenForm.Show(control, surface: Color.Black);
    }

    private static Label Percent(VolumeControl control) => control.GetPrivateField<Label>("_percent")!;

    private static HandledMouseEventArgs Wheel(int notches) => new HandledMouseEventArgs(MouseButtons.None, 0, 5, 5, notches * 120);

    // ---- the wheel over the button ----

    [Fact]
    public void TheWheelOverTheButton_TurnsTheVolumeLikeTheWheelOverTheSlider_AndTheParentDoesNotScroll()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
        using Form host = Show(control);
        int events = 0;
        control.VolumeChanged += (s, e) => events++;
        HandledMouseEventArgs up = Wheel(1);

        control.MuteButton.InvokePrivate("OnMouseWheel", up);

        Assert.Equal(0.5 + control.Slider.SmallChange, control.Volume, 6);
        Assert.Equal(1, events);
        Assert.True(up.Handled);

        control.MuteButton.InvokePrivate("OnMouseWheel", Wheel(-2));
        Assert.Equal(0.5 - control.Slider.SmallChange, control.Volume, 6);
        Assert.Equal(2, events);
    }

    [Fact]
    public void TheWheelBesideTheParts_TurnsTheVolumeToo()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
        using Form host = Show(control);

        control.InvokePrivate("OnMouseWheel", Wheel(1));

        Assert.Equal(0.5 + control.Slider.SmallChange, control.Volume, 6);
    }

    [Fact]
    public void TheWheelOnAMutedControl_TurnsTheSoundOn()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
        using Form host = Show(control);
        control.IsMuted = true;

        control.MuteButton.InvokePrivate("OnMouseWheel", Wheel(1));

        Assert.False(control.IsMuted);
        Assert.Equal(control.Slider.SmallChange, control.Volume, 6);
    }

    [Fact]
    public void TheWheel_DoesNothing_OnADisabledControl()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
        using Form host = Show(control);
        control.Enabled = false;

        control.MuteButton.InvokePrivate("OnMouseWheel", Wheel(1));
        control.InvokePrivate("OnMouseWheel", Wheel(1));

        Assert.Equal(0.5, control.Volume);
    }

    [Fact]
    public void TheWheelOverTheButton_IsPassedOnToTheTarget()
    {
        AudioVolumeState player = new AudioVolumeState(0.5);
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(target: player);
        using Form host = Show(control);

        control.MuteButton.InvokePrivate("OnMouseWheel", Wheel(1));

        Assert.Equal(0.5 + control.Slider.SmallChange, player.Volume, 6);
    }

    // ---- the percentage ----

    [Fact]
    public void ThePercentage_IsOffByDefault_AndTheSliderHasItsPopup()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.6);
        using Form host = Show(control);

        Assert.False(control.ShowPercentage);
        Assert.Null(control.GetPrivateField<Label>("_percent"));
        Assert.True(control.Slider.ShowValuePopup);
    }

    [Fact]
    public void WithThePercentage_TheNumberStandsBesideTheSlider_AndThePopupIsOff()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.6, showPercentage: true);
        using Form host = Show(control);
        Label percent = Percent(control);

        Assert.True(control.ShowPercentage);
        Assert.True(percent.Visible);
        Assert.False(control.Slider.ShowValuePopup);
        Assert.Equal(TrackBar.FormatPercent(0.6), percent.Text);

        // Right of the slider, inside the control, at its right edge; nothing overlaps.
        Assert.True(percent.Left >= control.Slider.Right, "the text overlaps the slider");
        Assert.True(percent.Right <= control.ClientSize.Width);
        Assert.True(percent.Width >= TextRenderer.MeasureText(TrackBar.FormatPercent(1.0), percent.Font).Width - 8, "no room for 100 %");
        Assert.Equal(control.ClientSize.Width, percent.Right);
    }

    [Fact]
    public void ThePercentage_FollowsTheSlider_AndIsZeroWhileMuted()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreatePrimary(0.6, showPercentage: true);
        using Form host = Show(control);
        Label percent = Percent(control);

        control.Volume = 0.25;
        Assert.Equal(TrackBar.FormatPercent(0.25), percent.Text);

        control.IsMuted = true;
        Assert.Equal(TrackBar.FormatPercent(0.0), percent.Text);

        control.IsMuted = false;
        Assert.Equal(TrackBar.FormatPercent(0.25), percent.Text);
    }

    [Fact]
    public void TurningThePercentageOff_TakesTheTextAway_AndGivesTheSliderItsRoomAndItsPopupBack()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.6);
        using Form host = Show(control);
        int fullWidth = control.Slider.Width;

        control.ShowPercentage = true;
        Assert.True(control.Slider.Width < fullWidth, "the slider keeps all the room");

        control.ShowPercentage = false;
        Assert.False(Percent(control).Visible);
        Assert.Equal(fullWidth, control.Slider.Width);
        Assert.True(control.Slider.ShowValuePopup);
    }

    [Fact]
    public void ThePercentage_IsWrittenInTheLanguage_AndFollowsAChangeOfIt()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.42, showPercentage: true);
        using Form host = Show(control);

        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                Assert.Equal((0.42).ToString("P0", UIStrings.NumberCulture), Percent(control).Text);
            });
        }
    }

    [Fact]
    public void TheFactories_CanSwitchThePercentageOn()
    {
        using VolumeControl standard = UIStyles.VolumeControls.CreateStandard(showPercentage: true);
        using VolumeControl primary = UIStyles.VolumeControls.CreatePrimary(showPercentage: true);

        Assert.True(standard.ShowPercentage);
        Assert.True(primary.ShowPercentage);
    }

    [Fact]
    public void AControlSmallerThanThePercentage_DoesNotThrow_AndKeepsTheSliderInside()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.6, size: new Size(40, 30), showPercentage: true);
        using Form host = Show(control);

        Assert.True(control.Slider.Width >= 1);
        Assert.True(Percent(control).Left >= 0);
    }
}
