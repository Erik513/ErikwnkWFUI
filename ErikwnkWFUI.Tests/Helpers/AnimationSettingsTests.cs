using System.Reflection;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;
using Microsoft.Win32;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>
/// Hearing that the animation setting of Windows changed: listeners wait only while there is one, each is told, one that fails does not keep the others
/// from hearing it, and a control is told on the thread of its window. [Collection]: the list of listeners is shared by every control that moves.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class AnimationSettingsTests : IDisposable
{
    private readonly List<IAnimationSettingsListener> _listeners = new List<IAnimationSettingsListener>();
    private readonly List<Control> _controls = new List<Control>();

    public void Dispose()
    {
        foreach (IAnimationSettingsListener listener in _listeners)
            AnimationSettings.Remove(listener);

        foreach (Control control in _controls)
            control.Dispose();
    }

    private sealed class CountingListener : IAnimationSettingsListener
    {
        public int Told;

        public void NotifySettingChanged() => Told++;
    }

    private sealed class FailingListener : IAnimationSettingsListener
    {
        public void NotifySettingChanged() => throw new ObjectDisposedException("its window is gone");
    }

    private T Add<T>(T listener) where T : IAnimationSettingsListener
    {
        _listeners.Add(listener);
        AnimationSettings.Add(listener);

        return listener;
    }

    [Fact]
    public void TheSettingIsAYesOrNo_AndATestCanReplaceIt()
    {
        Func<bool> before = AnimationSettings.Enabled;

        try
        {
            AnimationSettings.Enabled = () => false;
            Assert.False(AnimationSettings.Enabled());

            AnimationSettings.Enabled = before;
            Assert.IsType<bool>(AnimationSettings.Enabled());
        }
        finally
        {
            AnimationSettings.Enabled = before;
        }
    }

    [Fact]
    public void AListenerAddedTwice_IsWaitingOnce_AndLeavesWithOneRemove()
    {
        int before = AnimationSettings.Count;
        CountingListener listener = new CountingListener();

        Add(listener);
        Add(listener);
        Assert.Equal(before + 1, AnimationSettings.Count);

        AnimationSettings.Remove(listener);
        Assert.Equal(before, AnimationSettings.Count);

        AnimationSettings.Remove(listener);
        Assert.Equal(before, AnimationSettings.Count);
    }

    [Fact]
    public void RaisingTellsEveryListener_AndOneThatFailsDoesNotKeepTheOthersFromHearingIt()
    {
        Add(new FailingListener());
        CountingListener first = Add(new CountingListener());
        CountingListener second = Add(new CountingListener());

        Assert.Null(Record.Exception(() => AnimationSettings.Raise()));

        Assert.Equal(1, first.Told);
        Assert.Equal(1, second.Told);
    }

    [Fact]
    public void AChangeOfAPreferenceOfTheSystem_TellsTheListeners()
    {
        CountingListener listener = Add(new CountingListener());

        typeof(AnimationSettings).GetMethod("OnPreferenceChanged", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object?[] { null, new UserPreferenceChangedEventArgs(UserPreferenceCategory.General) });

        Assert.Equal(1, listener.Told);
    }

    [Fact]
    public void AControlIsTold_OnTheThreadOfItsWindow_AndNotWithoutAWindowOrWhenDisposed()
    {
        Control withWindow = new Control();
        Control withoutWindow = new Control();
        Control gone = new Control();
        _controls.AddRange(new[] { withWindow, withoutWindow, gone });
        _ = withWindow.Handle;
        _ = gone.Handle;
        gone.Dispose();
        int ownThread = Environment.CurrentManagedThreadId;
        List<int> threads = new List<int>();
        int withoutCalls = 0;
        int goneCalls = 0;

        ControlAnimationListener listener = new ControlAnimationListener(withWindow, () => threads.Add(Environment.CurrentManagedThreadId));
        ControlAnimationListener none = new ControlAnimationListener(withoutWindow, () => withoutCalls++);
        ControlAnimationListener disposed = new ControlAnimationListener(gone, () => goneCalls++);

        listener.NotifySettingChanged();
        none.NotifySettingChanged();
        disposed.NotifySettingChanged();
        TestPictures.Pump(100);

        Assert.Equal(new[] { ownThread }, threads);
        Assert.Equal(0, withoutCalls);
        Assert.Equal(0, goneCalls);
    }
}
