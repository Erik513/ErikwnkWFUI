using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>
/// NativeControlTheme.Track: the one place that keeps the native parts of a control (scroll bars, list header,
/// drop-down list) in the style of the theme - on every new window and on every change of the theme.
/// [Collection]: the theme is switched.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class NativeControlThemeTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private Panel Show(Action<IntPtr>? apply = null, Func<IntPtr>? find = null)
    {
        Panel panel = new Panel { Bounds = new Rectangle(5, 5, 100, 100) };
        Form form = OffscreenForm.Create(new Size(120, 120));
        form.Controls.Add(panel);

        if (apply != null)
            NativeControlTheme.Track(panel, find ?? (() => panel.Handle), apply);

        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return panel;
    }

    [Fact]
    public void TheStyleIsSet_WhenTheWindowIsCreated_AndOnEveryChangeOfTheTheme()
    {
        List<bool> calls = new List<bool>();
        Show(handle => calls.Add(NativeControlTheme.IsDark));

        Assert.Equal(new[] { true }, calls);

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();
        UIColors.ApplyTheme(UIThemes.Dark);
        Application.DoEvents();

        Assert.Equal(new[] { true, false, true }, calls);
    }

    [Fact]
    public void ATextBoxFromTheFactory_IsTracked_AMultilineOneToo()
    {
        int before = ThemeListeners.Count();
        TextBox box = UIStyles.TextBoxes.CreateStandard("x");
        box.Multiline = true;
        Form form = OffscreenForm.Create(new Size(120, 120));
        form.Controls.Add(box);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        // The follow of its colors, and the style of its scroll bars: four listeners while it has a window (fill, text, native parts, frame).
        Assert.Equal(before + 4, ThemeListeners.Count());
    }

    [Fact]
    public void ANativeWindowThatIsNotThere_IsLeftAlone()
    {
        int calls = 0;
        Show(handle => calls++, () => IntPtr.Zero);

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(0, calls);
    }

    [Fact]
    public void AControlThatIsGone_IsNotAskedAnymore()
    {
        int calls = 0;
        Panel panel = Show(handle => calls++);

        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(1, calls);
        Assert.True(panel.IsDisposed);
    }

    [Fact]
    public void ANewWindowOfTheSameControl_GetsTheStyleAgain()
    {
        int calls = 0;
        Panel panel = Show(handle => calls++);

        panel.Parent!.Controls.Remove(panel);
        panel.Parent = null;
        ((Form)_forms[0]).Controls.Add(panel);
        panel.RecreateHandleForTest();
        Application.DoEvents();

        Assert.True(calls >= 2);
    }

    // ---- the square where two scroll bars meet

    // A panel that scrolls both ways (its content is larger than it is): the system gives it both bars.
    private Panel ShowScrolling(Size content)
    {
        Panel panel = new Panel { Bounds = new Rectangle(5, 5, 100, 100), AutoScroll = true, AutoScrollMinSize = content };
        Form form = OffscreenForm.Create(new Size(120, 120));
        form.Controls.Add(panel);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return panel;
    }

    [Fact]
    public void TheCornerOfTwoDarkScrollBars_IsTheSizeOfABar_AtTheLowerRightOfTheWindow()
    {
        Panel panel = ShowScrolling(new Size(500, 500));

        Assert.True(NativeControlTheme.TryGetScrollBarCorner(panel, out Rectangle corner));

        int bar = SystemInformation.VerticalScrollBarWidth * panel.DeviceDpi / 96;
        Assert.Equal(bar, corner.Width);
        Assert.Equal(SystemInformation.HorizontalScrollBarHeight * panel.DeviceDpi / 96, corner.Height);
        Assert.True(corner.Right <= panel.Width + 1 && corner.Bottom <= panel.Height + 1, corner + " in a window of " + panel.Size);
        Assert.True(corner.Left > panel.Width / 2 && corner.Top > panel.Height / 2, corner + " is not at the lower right");
    }

    [Fact]
    public void ThereIsNoCorner_OnTheLightTheme_WithOnlyOneBar_OrWithoutAWindow()
    {
        Panel both = ShowScrolling(new Size(500, 500));
        Panel onlyVertical = ShowScrolling(new Size(10, 500));
        using Panel noWindow = new Panel { AutoScroll = true, AutoScrollMinSize = new Size(500, 500) };

        Assert.False(NativeControlTheme.TryGetScrollBarCorner(onlyVertical, out _));
        Assert.False(NativeControlTheme.TryGetScrollBarCorner(noWindow, out _));

        UIColors.ApplyTheme(UIThemes.Light);

        Assert.False(NativeControlTheme.TryGetScrollBarCorner(both, out Rectangle corner));
        Assert.Equal(Rectangle.Empty, corner);
    }

    [Fact]
    public void PaintingTheCorner_NeverThrows_AndWhereThereIsNoneDoesNothing()
    {
        Panel both = ShowScrolling(new Size(500, 500));
        Panel onlyVertical = ShowScrolling(new Size(10, 500));

        Assert.Null(Record.Exception(() => NativeControlTheme.PaintScrollBarCorner(both)));
        Assert.Null(Record.Exception(() => NativeControlTheme.PaintScrollBarCorner(onlyVertical)));

        UIColors.ApplyTheme(UIThemes.Light);

        Assert.Null(Record.Exception(() => NativeControlTheme.PaintScrollBarCorner(both)));
    }

    [Fact]
    public void ApplyToTheScrollBarsAndTheItemsView_AskForTheStyleOfTheTheme_AndNothingForNoWindow()
    {
        List<string> styles = new List<string>();
        Action<IntPtr, string> before = NativeControlTheme.SetStyle;
        NativeControlTheme.SetStyle = (handle, name) => styles.Add(name);

        try
        {
            NativeControlTheme.ApplyToScrollBars((IntPtr)1);
            NativeControlTheme.ApplyToItemsView((IntPtr)1);
            NativeControlTheme.ApplyToScrollBars(IntPtr.Zero);
            NativeControlTheme.ApplyToItemsView(IntPtr.Zero);

            UIColors.ApplyTheme(UIThemes.Light);
            NativeControlTheme.ApplyToScrollBars((IntPtr)1);
            NativeControlTheme.ApplyToItemsView((IntPtr)1);
        }
        finally
        {
            NativeControlTheme.SetStyle = before;
        }

        Assert.Equal(new[] { "DarkMode_Explorer", "DarkMode_ItemsView", "Explorer", "ItemsView" }, styles);
    }

    [Fact]
    public void TheSizeOfAScrollBar_GrowsWithTheScreen_AndIsNotScaledTwice()
    {
        int width96 = NativeControlTheme.GetScrollBarWidth(96);
        int width192 = NativeControlTheme.GetScrollBarWidth(192);
        int height96 = NativeControlTheme.GetScrollBarHeight(96);
        int height192 = NativeControlTheme.GetScrollBarHeight(192);

        Assert.InRange(width96, 8, 30);
        Assert.InRange(height96, 8, 30);
        Assert.InRange(width192, width96 * 2 - 2, width96 * 2 + 2);
        Assert.InRange(height192, height96 * 2 - 2, height96 * 2 + 2);
    }
}

internal static class ControlTestExtensions
{
    public static void RecreateHandleForTest(this Control control)
    {
        typeof(Control).GetMethod("RecreateHandle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(control, null);
    }
}
