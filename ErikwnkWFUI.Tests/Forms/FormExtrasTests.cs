using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using LibraryMenu = ErikwnkWFUI.Controls.ContextMenuStrip;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The system menu, Escape in a dialog, the shadow and the remembered place of an AppForm.</summary>
[Collection(AccentColorTestCollection.Name)]
public class FormExtrasTests : IDisposable
{
    private const int ClassStyleDropShadow = 0x00020000;
    private const int WmClose = 0x0010;
    private const int WmSysCommand = 0x0112;
    private const int ScClose = 0xF060;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "wfui_placement_" + Guid.NewGuid().ToString("N"));
    private readonly List<Form> _forms = new List<Form>();

    public FormExtrasTests()
    {
        WindowPlacementStore.Directory = _folder;
    }

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        WindowPlacementStore.Directory = null;

        if (Directory.Exists(_folder))
            Directory.Delete(_folder, true);
    }

    // Lets the tests press keys and read the class style.
    private sealed class KeyForm : AppForm
    {
        public KeyForm(FormOptions options)
            : base(options)
        {
        }

        public bool PressEscape()
        {
            return ProcessDialogKey(Keys.Escape);
        }

        public int ClassStyle => CreateParams.ClassStyle;
    }

    private T Own<T>(T form) where T : Form
    {
        _forms.Add(form);

        return form;
    }

    private static string[] Texts(ToolStripItemCollection items)
    {
        return items.Cast<ToolStripItem>().Where(i => i is ToolStripMenuItem).Select(i => i.Text ?? "").ToArray();
    }

    // ---- system menu

    [Fact]
    public void TheSystemMenu_OffersWhatTheWindowCanDoNow()
    {
        AppForm window = Own(UIStyles.Forms.CreateStandard("Window"));
        AppForm dialog = Own(UIStyles.Forms.CreateDialogStandard("Dialog"));

        using (LibraryMenu normal = window.CreateSystemMenu())
            Assert.Equal(new[] { UIStrings.Get("TitleBar.Minimize"), UIStrings.Get("TitleBar.Maximize"), UIStrings.Get("TitleBar.Close") }, Texts(normal.Items));

        using (LibraryMenu fixedSize = dialog.CreateSystemMenu())
        {
            Assert.Equal(new[] { UIStrings.Get("TitleBar.Close") }, Texts(fixedSize.Items));
            Assert.Single(fixedSize.Items);
        }
    }

    [Fact]
    public void AMaximizedWindow_CanBeRestored_AndNotMaximizedAgain()
    {
        AppForm window = Own(UIStyles.Forms.CreateStandard("Window"));
        OffscreenForm.Place(window);
        window.WindowState = FormWindowState.Maximized;

        using LibraryMenu menu = window.CreateSystemMenu();

        Assert.Equal(new[] { UIStrings.Get("TitleBar.Restore"), UIStrings.Get("TitleBar.Minimize"), UIStrings.Get("TitleBar.Close") }, Texts(menu.Items));
    }

    [Fact]
    public void TheEntriesOfTheSystemMenu_DoWhatTheyName()
    {
        AppForm window = Own(UIStyles.Forms.CreateStandard("Window"));
        OffscreenForm.Place(window);

        using (LibraryMenu menu = window.CreateSystemMenu())
            menu.Items.Cast<ToolStripItem>().OfType<ToolStripMenuItem>().First(i => i.Text == UIStrings.Get("TitleBar.Maximize")).PerformClick();
        Assert.Equal(FormWindowState.Maximized, window.WindowState);

        using (LibraryMenu menu = window.CreateSystemMenu())
            menu.Items.Cast<ToolStripItem>().OfType<ToolStripMenuItem>().First(i => i.Text == UIStrings.Get("TitleBar.Restore")).PerformClick();
        Assert.Equal(FormWindowState.Normal, window.WindowState);

        using (LibraryMenu menu = window.CreateSystemMenu())
            menu.Items.Cast<ToolStripItem>().OfType<ToolStripMenuItem>().First(i => i.Text == UIStrings.Get("TitleBar.Close")).PerformClick();
        Assert.True(window.IsDisposed);
    }

    [Fact]
    public void TheSystemMenu_IsInTheLanguageOfTheLibrary()
    {
        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                AppForm window = Own(UIStyles.Forms.CreateStandard("Window"));

                using LibraryMenu menu = window.CreateSystemMenu();
                Assert.Contains(UIStrings.Get("TitleBar.Close"), Texts(menu.Items));
            });
        }
    }

    [Fact]
    public void ARightClickOnTheTitleBar_AsksForTheSystemMenu_AtTheCursor_AndALeftClickDoesNot()
    {
        using TitleBarControl bar = new TitleBarControl(null, "Title");
        Label title = bar.GetPrivateField<Label>("_titleLabel")!;
        int asked = 0;
        bar.SystemMenuRequested += (sender, position) => asked++;

        title.InvokePrivate("OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Assert.Equal(0, asked);

        title.InvokePrivate("OnMouseUp", new MouseEventArgs(MouseButtons.Right, 1, 5, 5, 0));
        bar.InvokePrivate("OnMouseUp", new MouseEventArgs(MouseButtons.Right, 1, 5, 5, 0));
        Assert.Equal(2, asked);
    }

    // ---- Escape

    [Fact]
    public void Escape_ClosesADialogWithoutACancelButton_ButNotAStandardWindow()
    {
        KeyForm dialog = Own(new KeyForm(FormOptions.CreateDialog("Dialog")));
        KeyForm window = Own(new KeyForm(FormOptions.CreateStandard("Window")));
        bool dialogClosed = false;
        bool windowClosed = false;
        dialog.FormClosed += (sender, e) => dialogClosed = true;
        window.FormClosed += (sender, e) => windowClosed = true;
        OffscreenForm.Place(dialog);
        OffscreenForm.Place(window);

        Assert.False(window.PressEscape());
        Assert.False(windowClosed);

        Assert.True(dialog.PressEscape());
        Assert.True(dialogClosed);
    }

    [Fact]
    public void Escape_InADialogWithACancelButton_IsLeftToTheButton()
    {
        KeyForm dialog = Own(new KeyForm(FormOptions.CreateDialog("Dialog")));
        Button cancel = new Button();
        int clicks = 0;
        cancel.Click += (sender, e) => clicks++;
        dialog.Controls.Add(cancel);
        dialog.CancelButton = cancel;
        OffscreenForm.Place(dialog);

        dialog.PressEscape();

        Assert.Equal(1, clicks);
        Assert.False(dialog.IsDisposed);
    }

    // ---- closing can be switched off

    [Fact]
    public void WhenClosingIsNotEnabled_TheUserCannotCloseTheWindow_ButTheProgramCan()
    {
        FormOptions options = FormOptions.CreateDialog("Busy");
        options.CloseEnabled = false;
        KeyForm form = Own(new KeyForm(options));
        OffscreenForm.Place(form);

        // What the user does (Alt+F4, the taskbar, the system menu) is the close command of the system.
        SendMessage(form.Handle, WmSysCommand, (IntPtr)ScClose, IntPtr.Zero);
        Application.DoEvents();
        Assert.False(form.IsDisposed);
        Assert.True(form.Visible);

        Assert.False(form.PressEscape());
        Assert.True(form.Visible);

        form.Close();
        Assert.True(form.IsDisposed);
    }

    [Fact]
    public void WhenClosingIsNotEnabled_TheButtonAndTheMenuEntryAreGreyedOut_AndItCanBeTurnedOnAgain()
    {
        AppForm form = Own(UIStyles.Forms.CreateStandard("Busy"));
        OffscreenForm.Place(form);
        System.Windows.Forms.Button close = form.TitleBar.GetPrivateField<System.Windows.Forms.Button>("_closeButton")!;

        Assert.True(close.Enabled);

        form.CloseEnabled = false;
        Assert.False(close.Enabled);

        using (LibraryMenu menu = form.CreateSystemMenu())
            Assert.False(menu.Items.Cast<ToolStripItem>().OfType<ToolStripMenuItem>().Last().Enabled);

        form.CloseEnabled = true;
        Assert.True(close.Enabled);

        using (LibraryMenu menu = form.CreateSystemMenu())
            Assert.True(menu.Items.Cast<ToolStripItem>().OfType<ToolStripMenuItem>().Last().Enabled);

        SendMessage(form.Handle, WmSysCommand, (IntPtr)ScClose, IntPtr.Zero);
        Application.DoEvents();
        Assert.True(form.IsDisposed);
    }

    [Fact]
    public void APlainCloseMessage_AsEndTaskSendsIt_StillClosesAWindowThatTheUserCannotClose()
    {
        FormOptions options = FormOptions.CreateStandard("Busy");
        options.CloseEnabled = false;
        AppForm form = Own(new AppForm(options));
        OffscreenForm.Place(form);

        SendMessage(form.Handle, WmClose, IntPtr.Zero, IntPtr.Zero);
        Application.DoEvents();

        Assert.True(form.IsDisposed);
    }

    [Fact]
    public void TheOptionsCloseEnabled_IsTakenByTheWindow_AndIsOnByDefault()
    {
        FormOptions options = FormOptions.CreateStandard("Window");

        Assert.True(options.CloseEnabled);
        Assert.True(Own(new AppForm(options)).CloseEnabled);

        options.CloseEnabled = false;
        Assert.False(Own(new AppForm(options)).CloseEnabled);
    }

    // ---- shadow

    [Fact]
    public void AFramelessWindow_HasAShadow_UnlessSwitchedOff_AndTheNativeFrameHasItsOwn()
    {
        KeyForm normal = Own(new KeyForm(FormOptions.CreateStandard("Window")));
        FormOptions noShadow = FormOptions.CreateStandard("No shadow");
        noShadow.Shadow = false;
        KeyForm plain = Own(new KeyForm(noShadow));
        FormOptions native = FormOptions.CreateStandard("Native");
        native.Borderless = false;
        KeyForm framed = Own(new KeyForm(native));

        Assert.NotEqual(0, normal.ClassStyle & ClassStyleDropShadow);
        Assert.Equal(0, plain.ClassStyle & ClassStyleDropShadow);
        Assert.Equal(0, framed.ClassStyle & ClassStyleDropShadow);
    }

    // ---- remembered place

    [Fact]
    public void TheStore_GivesBackWhatWasSaved_AndOnlyWhenTheWindowIsOnAScreen()
    {
        Rectangle[] screens = { new Rectangle(0, 0, 1920, 1040) };

        WindowPlacementStore.Save("main", new Rectangle(100, 50, 800, 600), true);

        Assert.True(WindowPlacementStore.TryLoad("main", screens, out WindowPlacementStore.Placement placement));
        Assert.Equal(new Rectangle(100, 50, 800, 600), placement.Bounds);
        Assert.True(placement.Maximized);

        // The second screen was unplugged: the window would be nowhere.
        WindowPlacementStore.Save("far", new Rectangle(3000, 100, 800, 600), false);
        Assert.False(WindowPlacementStore.TryLoad("far", screens, out _));

        // Only a corner on the screen is not enough to reach the title bar.
        WindowPlacementStore.Save("corner", new Rectangle(1890, 1020, 800, 600), false);
        Assert.False(WindowPlacementStore.TryLoad("corner", screens, out _));

        Assert.False(WindowPlacementStore.TryLoad("never saved", screens, out _));
    }

    [Fact]
    public void TheStore_IgnoresAFileThatIsBroken_AndAKeyThatIsNotAFileName()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "broken.txt"), "not;numbers;at;all;x");
        File.WriteAllText(Path.Combine(_folder, "short.txt"), "1;2;3");
        Rectangle[] screens = { new Rectangle(0, 0, 1920, 1040) };

        Assert.False(WindowPlacementStore.TryLoad("broken", screens, out _));
        Assert.False(WindowPlacementStore.TryLoad("short", screens, out _));

        WindowPlacementStore.Save("a/b:c", new Rectangle(10, 10, 400, 300), false);
        Assert.True(WindowPlacementStore.TryLoad("a/b:c", screens, out _));
    }

    private static Rectangle InsideTheScreen()
    {
        Rectangle area = Screen.PrimaryScreen!.WorkingArea;

        return new Rectangle(area.X + 40, area.Y + 30, Math.Min(500, area.Width - 100), Math.Min(300, area.Height - 100));
    }

    [Fact]
    public void AWindowWithAKey_RemembersItsPlaceAndSize_AndGetsThemBackNextTime()
    {
        Rectangle bounds = InsideTheScreen();
        AppForm first = Own(UIStyles.Forms.CreateStandard("Main"));
        first.RememberBoundsKey = "main";
        first.StartPosition = FormStartPosition.Manual;
        first.Bounds = bounds;
        first.Show();
        first.Bounds = new Rectangle(bounds.X + 20, bounds.Y + 10, bounds.Width + 60, bounds.Height + 40);
        Rectangle expected = first.Bounds;
        first.Close();

        AppForm second = Own(UIStyles.Forms.CreateStandard("Main"));
        second.RememberBoundsKey = "main";
        second.Show();

        Assert.Equal(expected, second.Bounds);
        Assert.Equal(FormWindowState.Normal, second.WindowState);
    }

    [Fact]
    public void AMaximizedWindow_ComesBackMaximized_AndRestoresToTheSizeItHad()
    {
        Rectangle bounds = InsideTheScreen();
        FormOptions options = FormOptions.CreateStandard("Main");
        options.RememberBoundsKey = "maximized";
        AppForm first = Own(new AppForm(options));
        first.StartPosition = FormStartPosition.Manual;
        first.Bounds = bounds;
        first.Show();
        first.WindowState = FormWindowState.Maximized;
        first.Close();

        AppForm second = Own(new AppForm(options));
        second.Show();

        Assert.Equal(FormWindowState.Maximized, second.WindowState);
        Assert.Equal(bounds, second.RestoreBounds);
    }

    [Fact]
    public void AFixedSizeWindow_RemembersOnlyItsPosition()
    {
        Rectangle bounds = InsideTheScreen();
        AppForm first = Own(UIStyles.Forms.CreateSettingsStandard("Settings"));
        first.RememberBoundsKey = "settings";
        first.StartPosition = FormStartPosition.Manual;
        first.Bounds = bounds;
        first.Show();
        first.Close();

        AppForm second = Own(UIStyles.Forms.CreateSettingsStandard("Settings"));
        second.RememberBoundsKey = "settings";
        second.Size = new Size(333, 222);
        second.Show();

        Assert.Equal(bounds.Location, second.Location);
        Assert.Equal(new Size(333, 222), second.Size);
    }

    [Fact]
    public void AWindowWithoutAKey_RemembersNothing()
    {
        AppForm window = Own(UIStyles.Forms.CreateStandard("Main"));
        OffscreenForm.Place(window);
        window.Close();

        Assert.False(Directory.Exists(_folder) && Directory.GetFiles(_folder).Length > 0);
    }

    [Fact]
    public void TheKeyOfTheOptions_IsTakenByTheWindow()
    {
        FormOptions options = FormOptions.CreateDialog("Dialog");
        options.RememberBoundsKey = "dialog";

        Assert.Equal("dialog", Own(new AppForm(options)).RememberBoundsKey);
        Assert.True(options.CloseOnEscape);
        Assert.False(FormOptions.CreateStandard("Window").CloseOnEscape);
    }
}
