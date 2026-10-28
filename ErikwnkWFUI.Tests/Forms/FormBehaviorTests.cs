using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>How an AppForm behaves like a normal Form: its title, and the size it has when it is maximized.</summary>
public class FormBehaviorTests
{
    private const int WmGetMinMaxInfo = 0x24;
    private const int WsMinimizeBox = 0x00020000;


    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    // Lets the test send the message the system sends when a window is about to be maximized.
    private sealed class MessageForm : AppForm
    {
        public MessageForm(FormOptions options)
            : base(options)
        {
        }

        public int Style => CreateParams.Style;

        public MinMaxInfo AskMinMaxInfo()
        {
            IntPtr memory = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MinMaxInfo)));

            try
            {
                Marshal.StructureToPtr(new MinMaxInfo(), memory, false);
                Message message = Message.Create(Handle, WmGetMinMaxInfo, IntPtr.Zero, memory);
                WndProc(ref message);

                return (MinMaxInfo)Marshal.PtrToStructure(memory, typeof(MinMaxInfo))!;
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }
    }

    [Fact]
    public void TheText_AndTheTitleOfTheBar_AreOneTitle_WhicheverIsSet()
    {
        using AppForm form = UIStyles.Forms.CreateStandard("First");

        Assert.Equal("First", form.Text);
        Assert.Equal("First", form.TitleBar.Title);

        form.Text = "Second";
        Assert.Equal("Second", form.TitleBar.Title);
        Assert.Equal("Second", form.FormTitle);

        form.FormTitle = "Third";
        Assert.Equal("Third", form.Text);
        Assert.Equal("Third", form.TitleBar.Title);

        form.FormTitle = null!;
        Assert.Equal("", form.Text);
        Assert.Equal("", form.TitleBar.Title);
    }

    [Fact]
    public void TheTitleBarAlone_CanStillShowAnotherTitle()
    {
        using AppForm form = UIStyles.Forms.CreateStandard("Window");

        form.TitleBar.Title = "Only the bar";

        Assert.Equal("Window", form.Text);
    }

    [Fact]
    public void AMaximizedFramelessWindow_GetsTheWorkAreaOfItsScreen_NotTheWholeScreen()
    {
        // A second screen, on the left, with a taskbar at the top: the position is relative to the screen.
        AppForm.GetMaximizedPlacement(new Rectangle(-1920, 40, 1920, 1040), new Rectangle(-1920, 0, 1920, 1080), out Rectangle placement);

        Assert.Equal(new Rectangle(0, 40, 1920, 1040), placement);
    }

    [Fact]
    public void TheSystemAsksForTheSizeOfAMaximizedWindow_AndGetsTheWorkArea()
    {
        using MessageForm form = new MessageForm(FormOptions.CreateStandard("Window"));

        MinMaxInfo info = form.AskMinMaxInfo();
        Rectangle area = Screen.FromControl(form).WorkingArea;
        Rectangle screen = Screen.FromControl(form).Bounds;

        Assert.Equal(area.Width, info.MaxSize.X);
        Assert.Equal(area.Height, info.MaxSize.Y);
        Assert.Equal(area.X - screen.X, info.MaxPosition.X);
        Assert.Equal(area.Y - screen.Y, info.MaxPosition.Y);
    }

    [Fact]
    public void AMaximumSizeOfTheForm_IsLeftToTheSystem()
    {
        using MessageForm form = new MessageForm(FormOptions.CreateStandard("Window"));
        form.MaximumSize = new Size(500, 400);

        MinMaxInfo info = form.AskMinMaxInfo();

        Assert.NotEqual(Screen.FromControl(form).WorkingArea.Width, info.MaxSize.X);
    }

    // (What the taskbar does with the style cannot be tried here: the system minimizes by a message even without it.)
    [Fact]
    public void AFramelessWindowWithAMinimizeButton_HasTheMinimizeStyle_ForTheTaskbar_AndADialogHasNot()
    {
        using MessageForm window = new MessageForm(FormOptions.CreateStandard("Window"));
        using MessageForm dialog = new MessageForm(FormOptions.CreateDialog("Dialog"));
        using MessageForm primary = new MessageForm(FormOptions.CreatePrimarySettings("Settings"));

        Assert.NotEqual(0, window.Style & WsMinimizeBox);
        Assert.Equal(0, dialog.Style & WsMinimizeBox);
        Assert.Equal(0, primary.Style & WsMinimizeBox);
    }

    [Fact]
    public void WithTheNativeFrame_ThereIsOneTitleBar_AndTheButtonsAreThoseOfTheForm()
    {
        FormOptions options = FormOptions.CreateDialog("Native");
        options.Borderless = false;
        using AppForm dialog = new AppForm(options);

        Assert.Equal(FormBorderStyle.FixedDialog, dialog.FormBorderStyle);
        Assert.False(dialog.MinimizeBox || dialog.MaximizeBox);
        Assert.True(dialog.ControlBox);
        Assert.DoesNotContain(dialog.Controls.Cast<Control>(), c => c is ErikwnkWFUI.Controls.TitleBarControl);
        Assert.Equal("Native", dialog.Text);

        FormOptions window = FormOptions.CreateStandard("Native window");
        window.Borderless = false;
        using AppForm resizable = new AppForm(window);

        Assert.Equal(FormBorderStyle.Sizable, resizable.FormBorderStyle);
        Assert.True(resizable.MinimizeBox && resizable.MaximizeBox);
    }

    [Fact]
    public void TheBarOfAnAppFormWithTheNativeFrame_IsDisposedWithTheForm()
    {
        FormOptions options = FormOptions.CreateStandard("Native");
        options.Borderless = false;
        AppForm form = new AppForm(options);
        var bar = form.TitleBar;

        form.Dispose();

        Assert.True(bar.IsDisposed);
    }
}
