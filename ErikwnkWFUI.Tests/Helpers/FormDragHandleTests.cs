using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>
/// Dragging a window by a control (the title bar): a press and a move wide enough start the move of the window, a double click maximizes it or
/// gives it back, a drag from the maximized state restores it under the mouse, one to the top of the screen maximizes it, and a window that ends
/// up off the screen is brought back. The mouse is put where the test wants it, and the system's loop of moving a window is not started.
/// </summary>
public class FormDragHandleTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private (Form Form, Control Handle, FormDragHandle Drag, List<Form> Moves) Make(bool allowSnap = true)
    {
        Form form = OffscreenForm.Create(new Size(300, 200), Color.Black);
        Control handle = new Control { Dock = DockStyle.Top, Height = 30 };
        form.Controls.Add(handle);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        FormDragHandle drag = new FormDragHandle(handle, allowSnap);
        List<Form> moves = new List<Form>();
        drag.StartNativeMove = moved => moves.Add(moved);
        drag.CursorPosition = () => new Point(100, 100);

        return (form, handle, drag, moves);
    }

    private static MouseEventArgs Mouse(MouseButtons button, int clicks = 1) => new MouseEventArgs(button, clicks, 5, 5, 0);

    private static void Raise(Control handle, string handler, MouseEventArgs e) => handle.InvokePrivate(handler, e);

    private static bool PumpUntil(Func<bool> done)
    {
        for (int i = 0; i < 200 && !done(); i++)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        return done();
    }

    [Fact]
    public void AControlIsNeeded_AndDisposingLetsGoOfIt()
    {
        Assert.Throws<ArgumentNullException>(() => new FormDragHandle(null!));

        (Form form, Control handle, FormDragHandle drag, List<Form> moves) = Make();
        drag.Dispose();
        Assert.Null(drag.DragHandle);

        Raise(handle, "OnMouseDown", Mouse(MouseButtons.Left));
        drag.CursorPosition = () => new Point(200, 200);
        Raise(handle, "OnMouseMove", Mouse(MouseButtons.Left));
        Assert.Empty(moves);
    }

    [Fact]
    public void APressAndAMoveWideEnough_StartTheMoveOfTheWindow_OnceAndOnlyWithTheLeftButton()
    {
        (Form form, Control handle, FormDragHandle drag, List<Form> moves) = Make();

        Raise(handle, "OnMouseDown", Mouse(MouseButtons.Right));
        drag.CursorPosition = () => new Point(150, 150);
        Raise(handle, "OnMouseMove", Mouse(MouseButtons.Left));
        Assert.Empty(moves);

        drag.CursorPosition = () => new Point(100, 100);
        Raise(handle, "OnMouseDown", Mouse(MouseButtons.Left));
        drag.CursorPosition = () => new Point(101, 101);
        Raise(handle, "OnMouseMove", Mouse(MouseButtons.Left));
        Assert.Empty(moves);

        drag.CursorPosition = () => new Point(110, 108);
        Raise(handle, "OnMouseMove", Mouse(MouseButtons.None));
        Assert.Empty(moves);
        Raise(handle, "OnMouseMove", Mouse(MouseButtons.Left));
        Assert.Single(moves);
        Assert.Same(form, moves[0]);

        Raise(handle, "OnMouseMove", Mouse(MouseButtons.Left));
        Assert.Single(moves);

        Raise(handle, "OnMouseDown", Mouse(MouseButtons.Left));
        Raise(handle, "OnMouseUp", Mouse(MouseButtons.Left));
        drag.CursorPosition = () => new Point(300, 300);
        Raise(handle, "OnMouseMove", Mouse(MouseButtons.Left));
        Assert.Single(moves);
    }

    [Fact]
    public void ADoubleClick_MaximizesTheWindow_AndGivesItBack_UnlessItMayNot()
    {
        (Form form, Control handle, FormDragHandle drag, List<Form> moves) = Make();

        Raise(handle, "OnMouseDown", Mouse(MouseButtons.Left, clicks: 2));
        Assert.True(PumpUntil(() => form.WindowState == FormWindowState.Maximized));

        Raise(handle, "OnMouseDown", Mouse(MouseButtons.Left, clicks: 2));
        Assert.Equal(FormWindowState.Normal, form.WindowState);

        drag.AllowWindowSnapAndMaximize = false;
        Raise(handle, "OnMouseDown", Mouse(MouseButtons.Left, clicks: 2));
        PumpUntil(() => false);
        Assert.Equal(FormWindowState.Normal, form.WindowState);
    }

    [Fact]
    public void ADragFromTheMaximizedState_RestoresTheWindowUnderTheMouse_AndThenMovesIt()
    {
        (Form form, Control handle, FormDragHandle drag, List<Form> moves) = Make();
        form.WindowState = FormWindowState.Maximized;
        Application.DoEvents();
        Screen screen = Screen.FromPoint(new Point(400, 300));
        drag.CursorPosition = () => new Point(400, 300);

        Raise(handle, "OnMouseDown", Mouse(MouseButtons.Left));
        drag.CursorPosition = () => new Point(420, 300);
        Raise(handle, "OnMouseMove", Mouse(MouseButtons.Left));

        Assert.Equal(FormWindowState.Normal, form.WindowState);
        Assert.True(PumpUntil(() => moves.Count == 1));
        Assert.Equal(420 - form.Width / 2, form.Left);
        Assert.True(form.Top >= screen.WorkingArea.Top);
    }

    [Fact]
    public void AMaximizedWindowThatMayNotSnap_StaysWhereItIsWhenItsBarIsDragged()
    {
        (Form form, Control handle, FormDragHandle drag, List<Form> moves) = Make(allowSnap: false);
        form.WindowState = FormWindowState.Maximized;
        Application.DoEvents();

        Raise(handle, "OnMouseDown", Mouse(MouseButtons.Left));
        drag.CursorPosition = () => new Point(200, 100);
        Raise(handle, "OnMouseMove", Mouse(MouseButtons.Left));
        PumpUntil(() => false);

        Assert.Equal(FormWindowState.Maximized, form.WindowState);
        Assert.Empty(moves);
    }

    [Fact]
    public void AWindowLetGoAtTheTopOfTheScreen_IsMaximized_AndOneThatEndsFarOffTheScreenIsBroughtBack()
    {
        (Form form, Control handle, FormDragHandle drag, List<Form> moves) = Make();
        Screen screen = Screen.PrimaryScreen!;
        form.Location = screen.WorkingArea.Location + new Size(20, 20);

        Raise(handle, "OnMouseDown", Mouse(MouseButtons.Left));
        drag.CursorPosition = () => new Point(screen.WorkingArea.Left + 100, screen.WorkingArea.Top);
        Raise(handle, "OnMouseMove", Mouse(MouseButtons.Left));
        Assert.True(PumpUntil(() => form.WindowState == FormWindowState.Maximized));

        form.WindowState = FormWindowState.Normal;
        form.Location = new Point(SystemInformation.VirtualScreen.Right + 500, screen.WorkingArea.Top + 300);
        drag.CursorPosition = () => new Point(screen.WorkingArea.Left + 100, screen.WorkingArea.Top + 300);
        Raise(handle, "OnMouseDown", Mouse(MouseButtons.Left));
        drag.CursorPosition = () => new Point(screen.WorkingArea.Left + 200, screen.WorkingArea.Top + 300);
        Raise(handle, "OnMouseMove", Mouse(MouseButtons.Left));

        Assert.True(form.Right <= SystemInformation.VirtualScreen.Right && form.Left >= SystemInformation.VirtualScreen.Left);
    }
}
