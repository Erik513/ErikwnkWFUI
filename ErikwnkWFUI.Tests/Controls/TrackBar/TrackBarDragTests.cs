using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;
using TrackBar = ErikwnkWFUI.Controls.TrackBar;

namespace ErikwnkWFUI.Tests.Controls.TrackBars;

/// <summary>The mouse on the track bar: a drag changes the value and ends where the mouse is let go, and also where it is taken away.</summary>
public class TrackBarDragTests
{
    private static MouseEventArgs Mouse(int x, MouseButtons buttons = MouseButtons.Left) => new MouseEventArgs(buttons, 1, x, 10, 0);

    private static Form Show(TrackBar bar)
    {
        return OffscreenForm.Show(bar, new Size(120, 20), Color.Black, bounds: new Rectangle(0, 0, 120, 20));
    }

    private static void Down(TrackBar bar, int x) => bar.InvokePrivate("OnMouseDown", Mouse(x));

    private static void Move(TrackBar bar, int x) => bar.InvokePrivate("OnMouseMove", Mouse(x));

    private static void Up(TrackBar bar, int x) => bar.InvokePrivate("OnMouseUp", Mouse(x));

    [Fact]
    public void ADrag_RaisesDragStartedThenValueChanged_AndDragEndedOnceWhenTheMouseIsLetGo()
    {
        using TrackBar bar = new TrackBar();
        using Form host = Show(bar);
        List<string> events = new List<string>();
        bar.DragStarted += (s, e) => events.Add("started");
        bar.ValueChanged += (s, e) => events.Add("changed");
        bar.DragEnded += (s, e) => events.Add("ended");

        Down(bar, 7);   // the left end of the track: the value is 0 already, nothing changes
        Assert.True(bar.IsDragging);
        Move(bar, 60);
        Move(bar, 113);
        Up(bar, 113);
        Up(bar, 113);

        Assert.False(bar.IsDragging);
        Assert.Equal(new[] { "started", "changed", "changed", "ended" }, events);
        Assert.Equal(1.0, bar.Value, 3);
    }

    [Fact]
    public void ADragThatLosesTheMouse_Ends_AndTheMouseMovingOnDoesNotChangeTheValue()
    {
        using TrackBar bar = new TrackBar();
        using Form host = Show(bar);
        int ended = 0;
        bar.DragEnded += (s, e) => ended++;

        Down(bar, 60);
        double value = bar.Value;

        // A dialog, another window or Alt+Tab takes the mouse: no MouseUp comes.
        bar.InvokePrivate("OnMouseCaptureChanged", EventArgs.Empty);

        Assert.False(bar.IsDragging);
        Assert.Equal(1, ended);

        Move(bar, 10);
        Assert.Equal(value, bar.Value);
    }

    [Fact]
    public void ADragOfADisabledTrackBar_Ends()
    {
        using TrackBar bar = new TrackBar();
        using Form host = Show(bar);
        int ended = 0;
        bar.DragEnded += (s, e) => ended++;

        Down(bar, 60);
        bar.Enabled = false;

        Assert.False(bar.IsDragging);
        Assert.Equal(1, ended);

        bar.Enabled = true;
        Move(bar, 100);
        Assert.Equal(0.5, bar.Value, 1);
    }

    [Fact]
    public void ADisabledTrackBar_CannotBeDragged_AndOtherMouseButtonsDoNothing()
    {
        using TrackBar bar = new TrackBar();
        using Form host = Show(bar);
        bar.Enabled = false;
        Down(bar, 100);

        Assert.False(bar.IsDragging);
        Assert.Equal(0, bar.Value);

        bar.Enabled = true;
        bar.InvokePrivate("OnMouseDown", Mouse(100, MouseButtons.Right));

        Assert.False(bar.IsDragging);
        Assert.Equal(0, bar.Value);
    }

    [Fact]
    public void WhenTheWindowIsGone_TheDragIsOver_WithoutAnEventForTheListeners()
    {
        TrackBar bar = new TrackBar();
        Form host = Show(bar);
        int ended = 0;
        bar.DragEnded += (s, e) => ended++;
        Down(bar, 60);

        host.Dispose();

        Assert.False(bar.IsDragging);
        Assert.Equal(0, ended);
    }
}
