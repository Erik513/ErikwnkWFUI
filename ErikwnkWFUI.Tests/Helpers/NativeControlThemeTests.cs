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
}

internal static class ControlTestExtensions
{
    public static void RecreateHandleForTest(this Control control)
    {
        typeof(Control).GetMethod("RecreateHandle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(control, null);
    }
}
