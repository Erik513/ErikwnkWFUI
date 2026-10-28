using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Tests.Infrastructure;
using LibraryMenu = ErikwnkWFUI.Controls.ContextMenuStrip;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>An AppForm in the cases around the normal one: its buttons, the double click, the edges to resize by, the version strip and what is cleaned up.</summary>
[Collection(AccentColorTestCollection.Name)]
public class AppFormEdgeCaseTests : IDisposable
{
    private const int WmNcHitTest = 0x84;
    private const int HtClient = 1;
    private const int HtLeft = 10;
    private const int HtTopLeft = 13;
    private const int HtBottomRight = 17;

    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    // Lets the tests ask where a point of the window leads (what the mouse is over at the edges).
    private sealed class HitForm : AppForm
    {
        public HitForm(FormOptions options)
            : base(options)
        {
        }

        public int HitTest(Point clientPoint)
        {
            Point screen = PointToScreen(clientPoint);
            IntPtr lParam = (IntPtr)((screen.Y << 16) | (screen.X & 0xFFFF));
            Message message = Message.Create(Handle, WmNcHitTest, IntPtr.Zero, lParam);
            WndProc(ref message);

            return (int)message.Result;
        }
    }

    private T Show<T>(T form) where T : Form
    {
        _forms.Add(form);
        OffscreenForm.Place(form);
        TestPictures.Pump(60);

        return form;
    }

    // (The buttons of the title bar take no focus, so PerformClick does nothing: they are clicked by the mouse.)
    private static void Click(Button button)
    {
        button.InvokePrivate("OnClick", EventArgs.Empty);
    }

    private static Button BarButton(AppForm form, string field)
    {
        return form.TitleBar.GetPrivateField<Button>(field)!;
    }

    [Fact]
    public void WithoutOptions_TheFormIsAStandardWindow()
    {
        AppForm none = Show(new AppForm());
        AppForm nothing = Show(new AppForm(null));

        Assert.Equal("", none.Text);
        Assert.True(none.TitleBar.ShowMinimizeButton && none.TitleBar.ShowMaximizeButton && none.TitleBar.ShowCloseButton);
        Assert.Equal(FormBorderStyle.None, nothing.FormBorderStyle);
        Assert.Same(none.TitleBar.Parent, none);
    }

    [Fact]
    public void AnAmpersandInTheTitle_IsShownAsItIs()
    {
        AppForm form = Show(UIStyles.Forms.CreateStandard("Save & exit"));
        Label title = form.TitleBar.GetPrivateField<Label>("_titleLabel")!;

        Assert.Equal("Save & exit", title.Text);
        Assert.False(title.UseMnemonic);
    }

    [Fact]
    public void TheIconOfTheOptions_IsTheOneOfTheWindow_AndTheLogoOfTheBarComesFromTheOptionsOrTheProperty()
    {
        using Icon icon = SystemIcons.Information;
        using Bitmap logo = new Bitmap(16, 16);
        FormOptions options = FormOptions.CreateStandard("Window", icon: logo, windowIcon: icon);
        AppForm form = Show(new AppForm(options));

        Assert.True(form.ShowIcon);
        Assert.Equal(icon.Handle, form.Icon!.Handle);
        Assert.Same(logo, form.FormIcon);

        form.FormIcon = null!;
        Assert.Null(form.TitleBar.IconImage);
    }

    [Fact]
    public void TheButtonsOfTheBar_MinimizeMaximizeRestoreAndClose()
    {
        AppForm form = Show(UIStyles.Forms.CreateStandard("Window"));
        bool closed = false;
        form.FormClosed += (sender, e) => closed = true;

        Click(BarButton(form, "_maximizeButton"));
        TestPictures.Pump(60);
        Assert.Equal(FormWindowState.Maximized, form.WindowState);
        Assert.Equal(UIStyles.Symbols.Restore, BarButton(form, "_maximizeButton").Text);

        Click(BarButton(form, "_maximizeButton"));
        TestPictures.Pump(60);
        Assert.Equal(FormWindowState.Normal, form.WindowState);
        Assert.Equal(UIStyles.Symbols.Maximize, BarButton(form, "_maximizeButton").Text);

        Click(BarButton(form, "_minimizeButton"));
        Assert.Equal(FormWindowState.Minimized, form.WindowState);

        form.WindowState = FormWindowState.Normal;
        Click(BarButton(form, "_closeButton"));
        Assert.True(closed);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    [Fact]
    public void TheButtonsOfTheBar_NeverTakeTheFocus_SoNoThickFocusBorderStaysAfterAClick()
    {
        AppForm form = Show(UIStyles.Forms.CreateStandard("Window"));
        Button inContent = new Button { Text = "In the content" };
        form.ContentPanel.Controls.Add(inContent);
        form.ActiveControl = inContent;

        foreach (string field in new[] { "_minimizeButton", "_maximizeButton", "_closeButton" })
        {
            Assert.False(BarButton(form, field).CanSelect);
            Assert.False(BarButton(form, field).TabStop);
        }

        // A click with the mouse: the system sends the button the press and the release.
        Button maximize = BarButton(form, "_maximizeButton");
        SendMessage(maximize.Handle, 0x0201, (IntPtr)1, (IntPtr)((5 << 16) | 5));
        SendMessage(maximize.Handle, 0x0202, IntPtr.Zero, (IntPtr)((5 << 16) | 5));
        TestPictures.Pump(100);

        Assert.Equal(FormWindowState.Maximized, form.WindowState);
        Assert.False(maximize.Focused);
        Assert.NotSame(maximize, form.ActiveControl);
        Assert.Same(inContent, form.ActiveControl);
    }

    [Fact]
    public void ADoubleClickOnTheBar_MaximizesAWindow_ButNotADialog()
    {
        AppForm window = Show(UIStyles.Forms.CreateStandard("Window"));
        AppForm dialog = Show(UIStyles.Forms.CreateDialog("Dialog"));

        foreach (AppForm form in new[] { window, dialog })
            form.TitleBar.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 2, 50, 10, 0));

        TestPictures.Pump(250);

        Assert.Equal(FormWindowState.Maximized, window.WindowState);
        Assert.Equal(FormWindowState.Normal, dialog.WindowState);
    }

    [Fact]
    public void TheEdgesOfAResizableWindow_LeadToTheResizeCursors_TheMiddleDoesNot()
    {
        HitForm form = Show(new HitForm(FormOptions.CreateStandard("Window")));
        form.ClientSize = new Size(400, 300);
        Application.DoEvents();

        Assert.Equal(HtLeft, form.HitTest(new Point(2, 150)));
        Assert.Equal(HtTopLeft, form.HitTest(new Point(2, 2)));
        Assert.Equal(HtBottomRight, form.HitTest(new Point(form.ClientSize.Width - 2, form.ClientSize.Height - 2)));
        Assert.Equal(HtClient, form.HitTest(new Point(200, 150)));
    }

    [Fact]
    public void ADialog_AndAMaximizedWindow_HaveNoEdgesToResizeBy()
    {
        HitForm dialog = Show(new HitForm(FormOptions.CreateDialog("Dialog")));
        HitForm window = Show(new HitForm(FormOptions.CreateStandard("Window")));
        window.WindowState = FormWindowState.Maximized;
        TestPictures.Pump(60);

        Assert.Equal(HtClient, dialog.HitTest(new Point(2, 100)));
        Assert.Equal(HtClient, window.HitTest(new Point(2, 100)));
    }

    [Fact]
    public void TheVersionStrip_HasTheTextAtItsRight_AndWhatIsAddedStandsLeftOfIt_AndNoTextMeansNoStrip()
    {
        FormOptions options = FormOptions.CreateStandard("Window");
        options.VersionText = "v1.2.3";
        AppForm form = Show(new AppForm(options));
        Button update = new Button { Text = "Update", AutoSize = true };

        form.VersionStrip!.Controls.Add(update);
        form.PerformLayout();
        TestPictures.Pump(60);

        Assert.Equal("v1.2.3", form.VersionLabel.Text);
        Assert.True(update.Left < form.VersionLabel.Left);
        Assert.Null(Show(UIStyles.Forms.CreateStandard("Plain")).VersionStrip);

        options.VersionText = "";
        Assert.Null(Show(new AppForm(options)).VersionStrip);
    }

    [Fact]
    public void TheTitleBar_StopsListeningToTheLanguage_WhenTheFormIsDisposed()
    {
        FieldInfo? field = typeof(UIStrings).GetField("LanguageChanged", BindingFlags.NonPublic | BindingFlags.Static);
        int Listeners() => ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0;
        int before = Listeners();

        AppForm form = Show(UIStyles.Forms.CreateStandard("Window"));
        Assert.True(Listeners() > before);

        form.Dispose();

        Assert.Equal(before, Listeners());
    }

    [Fact]
    public void ACloseEntryOfTheSystemMenu_ClosesTheWindow_AndTheOpenMenuGoesWithIt()
    {
        AppForm form = Show(UIStyles.Forms.CreateStandard("Window"));
        form.ShowSystemMenu(new Point(100, 100));
        LibraryMenu menu = form.GetPrivateField<LibraryMenu>("_openMenu")!;
        ToolStripMenuItem close = menu.Items.Cast<ToolStripItem>().OfType<ToolStripMenuItem>().Last();

        Assert.Null(Record.Exception(() => close.PerformClick()));

        Assert.True(form.IsDisposed);
        Assert.True(menu.IsDisposed);
    }

    [Fact]
    public void OpeningTheSystemMenuTwice_LeavesOnlyTheNewOne()
    {
        AppForm form = Show(UIStyles.Forms.CreateStandard("Window"));

        form.ShowSystemMenu(new Point(100, 100));
        LibraryMenu first = form.GetPrivateField<LibraryMenu>("_openMenu")!;
        form.ShowSystemMenu(new Point(120, 120));

        Assert.True(first.IsDisposed);
        Assert.False(form.GetPrivateField<LibraryMenu>("_openMenu")!.IsDisposed);
    }

    [Fact]
    public void DisposingTwice_AndDisposingAFormThatWasNeverShown_AreFine()
    {
        AppForm never = UIStyles.Forms.CreateSettings("Never shown");
        AppForm shown = Show(UIStyles.Forms.CreateStandard("Shown"));

        Assert.Null(Record.Exception(() => never.Dispose()));
        Assert.Null(Record.Exception(() => shown.Dispose()));
        Assert.Null(Record.Exception(() => shown.Dispose()));
    }
}
