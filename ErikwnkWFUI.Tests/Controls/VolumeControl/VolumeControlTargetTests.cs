using System.Drawing;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using VolumeControl = ErikwnkWFUI.Controls.VolumeControl;

namespace ErikwnkWFUI.Tests.Controls.VolumeControls;

/// <summary>A player coupled to the volume control (<c>Target</c>): the control sets it, and follows it when it changes by itself.</summary>
[Collection(AccentColorTestCollection.Name)]
public class VolumeControlTargetTests
{
    // A player of the kind of the interface: it keeps the two values, and raises the event whenever it is told to (even for the same value,
    // as a careless player does - the control must not loop on that).
    private sealed class FakePlayer : IAudioVolume
    {
        private double _volume = 0.5;
        private bool _muted;

        public int VolumeSets;
        public int MutedSets;

        public double Volume
        {
            get { return _volume; }
            set
            {
                VolumeSets++;
                _volume = value;
                VolumeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsMuted
        {
            get { return _muted; }
            set
            {
                MutedSets++;
                _muted = value;
                VolumeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? VolumeChanged;

        public int Listeners => VolumeChanged?.GetInvocationList().Length ?? 0;

        // The player changes by itself: a key of the keyboard, another place of the program.
        public void ChangeByItself(double volume, bool muted)
        {
            _volume = volume;
            _muted = muted;
            VolumeChanged?.Invoke(this, EventArgs.Empty);
        }
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

    private static void Click(VolumeControl control) => control.MuteButton.InvokePrivate("OnClick", EventArgs.Empty);

    private static MouseEventArgs Mouse(int x) => new MouseEventArgs(MouseButtons.Left, 1, x, 10, 0);

    private static void SlideTo(VolumeControl control, params double[] values)
    {
        System.Windows.Forms.Control slider = control.Slider;

        int X(double value) => 7 + (int)Math.Round(value * (slider.Width - 14));

        slider.InvokePrivate("OnMouseDown", Mouse(X(values[0])));

        foreach (double value in values)
            slider.InvokePrivate("OnMouseMove", Mouse(X(value)));

        slider.InvokePrivate("OnMouseUp", Mouse(X(values[values.Length - 1])));
    }

    // ---- assigning ----

    [Fact]
    public void Assigning_TakesOverTheVolumeAndTheMuteOfThePlayer_WithoutAnEvent()
    {
        FakePlayer player = new FakePlayer();
        player.ChangeByItself(0.3, true);
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        using Form host = Show(control);
        int events = 0;
        control.VolumeChanged += (s, e) => events++;

        control.Target = player;

        Assert.Equal(0.3, control.Volume);
        Assert.True(control.IsMuted);
        Assert.Equal(0.0, control.Slider.Value);
        Assert.Equal(UIStyles.Symbols.Mute, control.MuteButton.Text);
        Assert.Equal(0, events);
        Assert.Same(player, control.Target);
        Assert.Equal(1, player.Listeners);
        Assert.Equal(0, player.VolumeSets + player.MutedSets);
    }

    [Fact]
    public void APlayerWithVolumeZero_IsMuted_AndTheRememberedLevelStays()
    {
        FakePlayer player = new FakePlayer();
        player.ChangeByItself(0.0, false);
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.7);

        control.Target = player;

        Assert.True(control.IsMuted);
        Assert.Equal(0.7, control.Volume);

        player.ChangeByItself(double.NaN, false);
        Assert.Equal(0.7, control.Volume);
    }

    // ---- the control sets the player ----

    [Fact]
    public void AClickOnTheButton_SetsTheMuteOfThePlayer_AndKeepsItsVolume()
    {
        FakePlayer player = new FakePlayer();
        player.ChangeByItself(0.4, false);
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        using Form host = Show(control);
        control.Target = player;

        Click(control);

        Assert.True(player.IsMuted);
        Assert.Equal(0.4, player.Volume);

        Click(control);

        Assert.False(player.IsMuted);
        Assert.Equal(0.4, player.Volume);
    }

    [Fact]
    public void TheSlider_SetsTheVolumeOfThePlayer_AndASlideToZeroMutesItWithTheVolumeFromBeforeTheSlide()
    {
        FakePlayer player = new FakePlayer();
        player.ChangeByItself(0.6, false);
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        using Form host = Show(control);
        control.Target = player;

        SlideTo(control, 0.6, 0.3);
        Assert.Equal(0.3, player.Volume, 2);
        Assert.False(player.IsMuted);

        SlideTo(control, 0.3, 0.1, 0.0);
        Assert.True(player.IsMuted);
        Assert.Equal(0.3, player.Volume, 2);

        // From zero up: on again, at the level of the slider.
        SlideTo(control, 0.0, 0.5);
        Assert.False(player.IsMuted);
        Assert.Equal(0.5, player.Volume, 2);
    }

    [Fact]
    public void TheVolumeAndTheMuteSetByCode_AreGivenToThePlayerToo()
    {
        FakePlayer player = new FakePlayer();
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        control.Target = player;

        control.Volume = 0.8;
        Assert.Equal(0.8, player.Volume);

        control.IsMuted = true;
        Assert.True(player.IsMuted);

        control.Volume = 0;
        Assert.True(player.IsMuted);
        Assert.Equal(0.8, player.Volume);
    }

    // ---- the player changes by itself ----

    [Fact]
    public void WhenThePlayerChangesByItself_TheControlFollows_WithoutAnEvent()
    {
        FakePlayer player = new FakePlayer();
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        using Form host = Show(control);
        control.Target = player;
        int events = 0;
        control.VolumeChanged += (s, e) => events++;

        player.ChangeByItself(0.2, false);
        Assert.Equal(0.2, control.Volume);
        Assert.Equal(0.2, control.Slider.Value, 6);
        Assert.Equal(UIStyles.Symbols.Volume, control.MuteButton.Text);

        player.ChangeByItself(0.2, true);
        Assert.True(control.IsMuted);
        Assert.Equal(0.0, control.Slider.Value);
        Assert.Equal(UIStyles.Symbols.Mute, control.MuteButton.Text);

        Assert.Equal(0, events);
    }

    [Fact]
    public void WhenThePlayerChangesOnAnotherThread_TheControlFollowsOnItsOwn()
    {
        FakePlayer player = new FakePlayer();
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        using Form host = Show(control);
        control.Target = player;
        int events = 0;
        control.VolumeChanged += (s, e) => events++;

        // (The event of the player is raised on a thread of its own: the control must not touch itself there.)
        Exception? failure = null;
        Thread sound = new Thread(() =>
        {
            try
            {
                player.ChangeByItself(0.25, false);
            }
            catch (Exception e)
            {
                failure = e;
            }
        });
        sound.Start();
        sound.Join();
        Pump(150);

        Assert.Null(failure);

        Assert.Equal(0.25, control.Volume);
        Assert.Equal(0.25, control.Slider.Value, 6);
        Assert.Equal(0, events);
    }

    [Fact]
    public void APlayerThatRaisesTheEventForEverySet_DoesNotMakeTheControlLoop()
    {
        FakePlayer player = new FakePlayer();
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        using Form host = Show(control);
        control.Target = player;

        Click(control);
        SlideTo(control, 1.0, 0.4);
        control.Volume = 0.9;

        // Each action sets the volume and the mute once; nothing comes back.
        Assert.InRange(player.VolumeSets, 1, 10);
        Assert.InRange(player.MutedSets, 1, 10);
    }

    // ---- letting go ----

    [Fact]
    public void ANullTarget_AndAnotherTarget_LetGoOfThePlayerBefore()
    {
        FakePlayer first = new FakePlayer();
        FakePlayer second = new FakePlayer();
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        using Form host = Show(control);

        control.Target = first;
        control.Target = second;
        Assert.Equal(0, first.Listeners);
        Assert.Equal(1, second.Listeners);

        // The first one no longer moves the control, and the control no longer sets it.
        first.ChangeByItself(0.1, true);
        Assert.False(control.IsMuted);
        control.Volume = 0.77;
        Assert.NotEqual(0.77, first.Volume);

        control.Target = null;
        Assert.Equal(0, second.Listeners);
        Assert.Null(control.Target);
        Click(control);
        Assert.False(second.IsMuted);
    }

    [Fact]
    public void ADisposedControl_LetsGoOfThePlayer()
    {
        FakePlayer player = new FakePlayer();
        VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        Form host = Show(control);
        control.Target = player;
        Assert.Equal(1, player.Listeners);

        host.Dispose();
        control.Dispose();

        Assert.Equal(0, player.Listeners);
        Assert.Null(Record.Exception(() => player.ChangeByItself(0.3, false)));
    }

    [Fact]
    public void ADisabledControl_StillFollowsThePlayer()
    {
        FakePlayer player = new FakePlayer();
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard();
        using Form host = Show(control);
        control.Target = player;
        control.Enabled = false;

        player.ChangeByItself(0.35, false);

        Assert.Equal(0.35, control.Slider.Value, 6);
    }
}
