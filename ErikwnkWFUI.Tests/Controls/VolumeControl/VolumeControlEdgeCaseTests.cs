using System.Drawing;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;
using VolumeControl = ErikwnkWFUI.Controls.VolumeControl;

namespace ErikwnkWFUI.Tests.Controls.VolumeControls;

/// <summary>The volume control with odd input: values that are not numbers, the ends, tiny sizes, a window that is made again, a handler that answers.</summary>
[Collection(AccentColorTestCollection.Name)]
public class VolumeControlEdgeCaseTests
{
    private static Form Show(VolumeControl control)
    {
        return OffscreenForm.Show(control, surface: Color.Black);
    }

    private static HandledMouseEventArgs Wheel(int notches) => new HandledMouseEventArgs(MouseButtons.None, 0, 5, 5, notches * 120);

    // ---- values ----

    [Fact]
    public void AVolumeThatIsNoNumber_IsIgnored()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.4);

        control.Volume = double.NaN;

        Assert.Equal(0.4, control.Volume);
        Assert.False(control.IsMuted);
        Assert.Equal(0.4, control.Slider.Value, 6);
    }

    [Theory]
    [InlineData(double.PositiveInfinity, 1.0, false)]
    [InlineData(5.0, 1.0, false)]
    [InlineData(double.NegativeInfinity, 0.4, true)]
    [InlineData(-3.0, 0.4, true)]
    [InlineData(0.0, 0.4, true)]
    public void AVolumeOutsideTheRange_IsLimited_AndZeroOrLessMutes(double given, double remembered, bool muted)
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.4);

        control.Volume = given;

        Assert.Equal(remembered, control.Volume);
        Assert.Equal(muted, control.IsMuted);
        Assert.Equal(muted ? 0.0 : remembered, control.EffectiveVolume);
        Assert.InRange(control.Slider.Value, 0.0, 1.0);
    }

    [Fact]
    public void AStartVolumeOutsideTheRange_DoesNotBreakTheFactories()
    {
        using VolumeControl high = UIStyles.VolumeControls.CreateStandard(7);
        using VolumeControl low = UIStyles.VolumeControls.CreatePrimary(-1);
        using VolumeControl none = UIStyles.VolumeControls.CreateStandard(double.NaN);

        Assert.Equal(1.0, high.Volume);
        Assert.True(low.IsMuted);
        Assert.Equal(1.0, none.Volume);
    }

    [Fact]
    public void TheSameValueAgain_RaisesNothingAndPushesNothingNew()
    {
        AudioVolumeState player = new AudioVolumeState(0.5);
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(target: player);
        int events = 0;
        int playerEvents = 0;
        control.VolumeChanged += (s, e) => events++;
        player.VolumeChanged += (s, e) => playerEvents++;

        control.Volume = 0.5;
        control.IsMuted = false;

        Assert.Equal(0, events);
        Assert.Equal(0, playerEvents);
    }

    [Fact]
    public void ATargetGivenToADisposedControl_IsNotListenedTo()
    {
        AudioVolumeState player = new AudioVolumeState(0.5);
        VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        control.Dispose();

        Exception? failure = Record.Exception(() => control.Target = player);

        Assert.Null(failure);

        // (Nothing is subscribed: the player does not keep the dead control alive.)
        System.Reflection.FieldInfo field = typeof(AudioVolumeState).GetField("VolumeChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        Assert.Null(field.GetValue(player));
        Assert.Null(control.Target);
    }

    // ---- the wheel at the ends ----

    [Fact]
    public void TheWheelAtTheTop_AndAtTheBottom_RaisesNoEvent()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(1.0);
        using Form host = Show(control);
        int events = 0;
        control.VolumeChanged += (s, e) => events++;

        control.MuteButton.InvokePrivate("OnMouseWheel", Wheel(1));
        Assert.Equal(0, events);
        Assert.Equal(1.0, control.Volume);

        control.IsMuted = true;
        control.MuteButton.InvokePrivate("OnMouseWheel", Wheel(-1));
        Assert.Equal(0, events);
        Assert.True(control.IsMuted);
    }

    // ---- sizes ----

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(10, 400)]
    [InlineData(500, 4)]
    [InlineData(2000, 120)]
    public void AnyOddSize_DoesNotThrow_AndTheButtonStaysInside(int width, int height)
    {
        using VolumeControl control = UIStyles.VolumeControls.CreatePrimary(0.5, new Size(width, height), showPercentage: true);
        using Form host = Show(control);

        control.Size = new Size(width + 1, height + 1);
        control.PerformLayout();

        Assert.True(control.MuteButton.Width >= 1);
        Assert.True(control.MuteButton.Right <= Math.Max(control.ClientSize.Width, 1) + 1);
        Assert.True(control.Slider.Width >= 1);
    }

    // ---- the window is made again ----

    [Fact]
    public void AControlWhoseWindowIsMadeAgain_KeepsItsStateAndStillWorks()
    {
        AudioVolumeState player = new AudioVolumeState(0.6);
        using VolumeControl control = UIStyles.VolumeControls.CreatePrimary(target: player, showPercentage: true);
        using Form host = Show(control);
        control.IsMuted = true;

        control.InvokePrivate("RecreateHandle");
        Application.DoEvents();

        Assert.True(control.IsMuted);
        Assert.Equal(0.6, control.Volume);
        Assert.Equal(0.0, control.Slider.Value);

        control.MuteButton.InvokePrivate("OnClick", EventArgs.Empty);
        Assert.False(player.IsMuted);
        Assert.Equal(0.6, control.Slider.Value, 6);
    }

    [Fact]
    public void AControlTakenOutOfTheFormAndPutBack_StillFollowsThePlayer()
    {
        AudioVolumeState player = new AudioVolumeState(0.6);
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(target: player);
        using Form host = Show(control);

        host.Controls.Remove(control);
        player.Volume = 0.2;
        host.Controls.Add(control);
        Application.DoEvents();

        Assert.Equal(0.2, control.Volume, 6);
        Assert.Equal(0.2, control.Slider.Value, 6);
    }

    // ---- the percentage ----

    [Fact]
    public void SwitchingThePercentageOnAndOffAgain_NeverMakesASecondText()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
        using Form host = Show(control);
        int parts = control.Controls.Count;

        for (int i = 0; i < 5; i++)
        {
            control.ShowPercentage = true;
            control.ShowPercentage = false;
        }

        control.ShowPercentage = true;

        Assert.Equal(parts + 1, control.Controls.Count);
    }

    [Fact]
    public void ThePercentageWhileDisabled_IsDisabledToo_AndComesBackWithTheControl()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5, showPercentage: true);
        using Form host = Show(control);
        Label percent = control.GetPrivateField<Label>("_percent")!;

        control.Enabled = false;
        Assert.False(percent.Enabled);

        control.Enabled = true;
        Assert.True(percent.Enabled);
    }

    // ---- a handler that answers ----

    [Fact]
    public void AHandlerThatSetsTheVolume_DoesNotMakeItLoop()
    {
        AudioVolumeState player = new AudioVolumeState(0.5);
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(target: player);
        using Form host = Show(control);
        int events = 0;

        // (Like an application that limits the volume: it sets another value from the handler.)
        control.VolumeChanged += (s, e) =>
        {
            events++;

            if (control.Volume > 0.8)
                control.Volume = 0.8;
        };

        control.MuteButton.InvokePrivate("OnMouseWheel", Wheel(1));
        for (int i = 0; i < 10; i++)
            control.MuteButton.InvokePrivate("OnMouseWheel", Wheel(1));

        Assert.Equal(0.8, control.Volume, 6);
        Assert.Equal(0.8, player.Volume, 6);
        Assert.InRange(events, 1, 20);
    }

    [Fact]
    public void AHandlerThatDisposesTheControl_DoesNotThrow()
    {
        VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
        Form host = Show(control);
        control.VolumeChanged += (s, e) => control.Dispose();

        Exception? failure = Record.Exception(() => control.MuteButton.InvokePrivate("OnClick", EventArgs.Empty));

        Assert.Null(failure);
        Assert.True(control.IsDisposed);
        host.Dispose();
    }

    [Fact]
    public void TheDefaultsTheDesignerKnows_AreTheOnesTheControlHas_AndTheColorsAreCode()
    {
        using VolumeControl control = new VolumeControl();
        System.ComponentModel.PropertyDescriptorCollection properties = System.ComponentModel.TypeDescriptor.GetProperties(control);

        Assert.False(properties["Colors"]!.IsBrowsable);
        Assert.False(properties["Target"]!.IsBrowsable);

        foreach (System.ComponentModel.PropertyDescriptor property in properties)
        {
            if (property.Attributes[typeof(System.ComponentModel.DefaultValueAttribute)] is System.ComponentModel.DefaultValueAttribute attribute)
                Assert.True(Equals(attribute.Value, property.GetValue(control)), property.Name + " is not its default");
        }
    }
}
