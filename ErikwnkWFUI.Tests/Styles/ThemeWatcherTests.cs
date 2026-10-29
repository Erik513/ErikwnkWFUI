using System.Reflection;
using System.Runtime.ExceptionServices;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// ThemeWatcher: a control follows a change of the theme while it has a window, catches up when it gets one, lets go of the static event when the
/// window goes, and does its update on its own thread. [Collection]: the theme is static state of the process.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ThemeWatcherTests : IDisposable
{
    private readonly List<Control> _controls = new List<Control>();

    public void Dispose()
    {
        foreach (Control control in _controls)
            control.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private Panel Make(bool withWindow)
    {
        Panel panel = new Panel();
        _controls.Add(panel);

        if (withWindow)
            _ = panel.Handle;

        return panel;
    }

    // Runs the action on a thread of its own (not the one that made the windows) and passes on what it threw.
    private static void OnAnotherThread(Action action)
    {
        Exception? caught = null;
        Thread thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });

        thread.Start();
        thread.Join();

        if (caught != null)
            ExceptionDispatchInfo.Capture(caught).Throw();
    }

    private static int Subscribers()
    {
        FieldInfo field = typeof(UIColors).GetField("ThemeChanged", BindingFlags.NonPublic | BindingFlags.Static)!;

        return ((Delegate?)field.GetValue(null))?.GetInvocationList().Length ?? 0;
    }

    [Fact]
    public void WithAWindow_TheControlIsUpdated_OnEveryChangeOfTheTheme_AndOfTheAccent()
    {
        Panel panel = Make(true);
        int calls = 0;
        ThemeWatcher.Attach(panel, () => calls++);

        UIColors.ApplyTheme(UIThemes.Light);
        Assert.Equal(1, calls);

        UIColors.SetAccent(UIAccentColors.Red);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void WithoutAWindow_NothingHappens_UntilTheWindowIsMade_ThenTheControlCatchesUp_OnlyIfTheThemeChanged()
    {
        Panel changed = Make(false);
        Panel unchanged = Make(false);
        int changedCalls = 0;
        int unchangedCalls = 0;
        ThemeWatcher.Attach(changed, () => changedCalls++);

        UIColors.ApplyTheme(UIThemes.Light);
        Assert.Equal(0, changedCalls);

        _ = changed.Handle;
        Assert.Equal(1, changedCalls);

        ThemeWatcher.Attach(unchanged, () => unchangedCalls++);
        _ = unchanged.Handle;
        Assert.Equal(0, unchangedCalls);
    }

    [Fact]
    public void AControlWithoutAWindow_IsNotHeldByTheStaticEvent()
    {
        int before = Subscribers();
        Panel panel = Make(false);
        ThemeWatcher.Attach(panel, () => { });

        Assert.Equal(before, Subscribers());

        _ = panel.Handle;
        Assert.Equal(before + 1, Subscribers());

        panel.Dispose();
        Assert.Equal(before, Subscribers());
    }

    [Fact]
    public void AfterTheControlIsDisposed_ItIsNotUpdatedAnyMore_AndNothingBreaks()
    {
        Panel panel = Make(true);
        int calls = 0;
        ThemeWatcher.Attach(panel, () => calls++);
        panel.Dispose();

        UIColors.ApplyTheme(UIThemes.Light);

        Assert.Equal(0, calls);
    }

    [Fact]
    public void AChangeFromAnotherThread_UpdatesTheControl_OnTheThreadOfItsWindow()
    {
        Panel panel = Make(true);
        int ownThread = Environment.CurrentManagedThreadId;
        List<int> threads = new List<int>();
        ThemeWatcher.Attach(panel, () => threads.Add(Environment.CurrentManagedThreadId));

        OnAnotherThread(() => UIColors.ApplyTheme(UIThemes.Light));
        TestPictures.Pump(100);

        Assert.Equal(new[] { ownThread }, threads);
    }

    [Fact]
    public void AChangeFromAnotherThread_AfterTheWindowWentAway_DoesNotThrowAtTheCaller()
    {
        Panel panel = Make(true);
        ThemeWatcher.Attach(panel, () => { });
        panel.Dispose();

        Exception? thrown = Record.Exception(() => OnAnotherThread(() => UIColors.SetAccent(UIAccentColors.Green)));

        Assert.Null(thrown);
    }

    [Fact]
    public void FollowBackColor_TakesTheColorNow_FollowsTheTheme_AndLeavesAColorSetByHand()
    {
        Panel panel = Make(true);
        ThemeWatcher.FollowBackColor(panel, () => UIColors.BackgroundDark);

        Assert.Equal(UIColors.BackgroundDark, panel.BackColor);

        UIColors.ApplyTheme(UIThemes.Light);
        Assert.Equal(UIThemes.Light.BackgroundDark, panel.BackColor);

        panel.BackColor = Color.HotPink;
        UIColors.ApplyTheme(UIThemes.Dark);
        Assert.Equal(Color.HotPink, panel.BackColor);
    }

    [Fact]
    public void FollowForeColor_FollowsTheTheme_AndLeavesAColorSetByHand()
    {
        Panel panel = Make(true);
        ThemeWatcher.FollowForeColor(panel, () => UIColors.TextPrimary);

        UIColors.ApplyTheme(UIThemes.Light);
        Assert.Equal(UIThemes.Light.TextPrimary, panel.ForeColor);

        panel.ForeColor = Color.HotPink;
        UIColors.ApplyTheme(UIThemes.Dark);
        Assert.Equal(Color.HotPink, panel.ForeColor);
    }
}
