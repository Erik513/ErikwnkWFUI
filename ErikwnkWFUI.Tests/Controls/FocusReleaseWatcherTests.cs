using ErikwnkWFUI.Controls;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Tests.Controls;

/// <summary>
/// FocusReleaseWatcher: which clicks count as "elsewhere" for a box. (Giving the focus away is not tested here: a test must never take the focus.)
/// </summary>
public class FocusReleaseWatcherTests : IDisposable
{
    private readonly List<Control> _controls = new List<Control>();

    public void Dispose()
    {
        foreach (Control control in _controls)
            control.Dispose();
    }

    private T Make<T>(bool withWindow = true) where T : Control, new()
    {
        T control = new T();
        _controls.Add(control);

        if (withWindow)
            _ = control.Handle;

        return control;
    }

    [Fact]
    public void AnotherControlOfTheApplication_IsOutside_TheBoxItselfAndWhatIsInsideItIsNot()
    {
        Panel box = Make<Panel>();
        Panel inside = new Panel { Parent = box };
        _ = inside.Handle;
        Panel other = Make<Panel>();
        using FocusReleaseWatcher watcher = new FocusReleaseWatcher(box, () => true);

        Assert.True(watcher.IsOutsideClick(other.Handle));
        Assert.False(watcher.IsOutsideClick(box.Handle));
        Assert.False(watcher.IsOutsideClick(inside.Handle));
    }

    [Fact]
    public void ADropDownMenu_AWindowThatIsNoControl_AndTheZeroWindow_AreNotOutside()
    {
        Panel box = Make<Panel>();
        ToolStripDropDown menu = Make<ToolStripDropDown>();
        using FocusReleaseWatcher watcher = new FocusReleaseWatcher(box, () => true);

        Assert.False(watcher.IsOutsideClick(menu.Handle));
        Assert.False(watcher.IsOutsideClick(IntPtr.Zero));
        Assert.False(watcher.IsOutsideClick((IntPtr)0x7FFF1234));
    }

    [Fact]
    public void WhileTheSwitchIsOff_NoClickIsOutside()
    {
        Panel box = Make<Panel>();
        Panel other = Make<Panel>();
        bool on = false;
        using FocusReleaseWatcher watcher = new FocusReleaseWatcher(box, () => on);

        Assert.False(watcher.IsOutsideClick(other.Handle));

        on = true;
        Assert.True(watcher.IsOutsideClick(other.Handle));
    }

    [Fact]
    public void ABoxWithoutAWindow_OrOneThatIsDisposed_IsAskedNothing_AndMakesNoWindow()
    {
        Panel other = Make<Panel>();
        Panel noWindow = Make<Panel>(withWindow: false);
        Panel gone = Make<Panel>();
        using FocusReleaseWatcher withoutWindow = new FocusReleaseWatcher(noWindow, () => true);
        using FocusReleaseWatcher disposed = new FocusReleaseWatcher(gone, () => true);
        gone.Dispose();

        Assert.False(withoutWindow.IsOutsideClick(other.Handle));
        Assert.False(noWindow.IsHandleCreated);
        Assert.Null(Record.Exception(() => disposed.IsOutsideClick(other.Handle)));
        Assert.False(disposed.IsOutsideClick(other.Handle));
    }

    [Fact]
    public void ReleasingTheFocus_OfABoxWithoutFocus_DoesNothing()
    {
        Panel alone = Make<Panel>();
        using FocusReleaseWatcher watcher = new FocusReleaseWatcher(alone, () => true);

        Assert.Null(Record.Exception(() => watcher.ReleaseFocus()));
    }

    [Fact]
    public void StartStopAndDispose_AreSafeToRepeat()
    {
        Panel box = Make<Panel>();
        FocusReleaseWatcher watcher = new FocusReleaseWatcher(box, () => true);

        watcher.Start();
        watcher.Start();
        watcher.Stop();
        watcher.Stop();
        watcher.Dispose();
        watcher.Dispose();
    }
}
