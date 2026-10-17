using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ErikwnkCore.Updater;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Panels;

/// <summary>The version panel: the version of the program and, while an update is known, the button that leads to it.</summary>
[Collection(AccentColorTestCollection.Name)]
public class VersionPanelTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();
    private readonly HttpClient _client = new HttpClient(new FakeGitHub("{\"tag_name\":\"v99.0.0\",\"html_url\":\"https://example.com/r\",\"assets\":[]}"));

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        _client.Dispose();
        UIColors.ApplyTheme(UIThemes.Dark);
    }

    // Answers the request of the update check with a fixed text (no network).
    private sealed class FakeGitHub : HttpMessageHandler
    {
        private readonly string _json;

        public FakeGitHub(string json)
        {
            _json = json;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = _json == null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_json, Encoding.UTF8, "application/json") };

            return Task.FromResult(response);
        }
    }

    private AppUpdater Updater() => new AppUpdater("owner", "repo", _client, _client);

    private static UpdateCheckResult Result() => new UpdateCheckResult(new Version(99, 0, 0), "", null);

    private VersionPanel Shown(VersionPanel panel, int width = 300)
    {
        panel.Dock = DockStyle.None;
        panel.Size = new Size(width, panel.Height);
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(width + 20, 100) };
        panel.Location = new Point(10, 10);
        host.Controls.Add(panel);
        host.Show();
        Application.DoEvents();
        _forms.Add(host);

        return panel;
    }

    private static Bitmap Render(Control control)
    {
        Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));

        return bitmap;
    }

    // The columns where something other than the background is painted, left of the update button.
    private static (int First, int Last) InkColumns(Bitmap bitmap, int toColumn)
    {
        Color back = bitmap.GetPixel(2, 2);
        int first = -1, last = -1;

        for (int x = 0; x < toColumn; x++)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() != back.ToArgb())
                {
                    if (first < 0)
                        first = x;

                    last = x;
                    break;
                }
            }
        }

        return (first, last);
    }

    [Fact]
    public void TheVersionIsThatOfTheProgram_ByDefault_AndTheTextCanBeSet()
    {
        using VersionPanel panel = UIStyles.VersionPanels.CreateStandard();

        Assert.Matches(new Regex(@"^v\d+\.\d+\.\d+$"), panel.VersionText);

        panel.VersionText = "v9.8.7 beta";
        Assert.Equal("v9.8.7 beta", panel.VersionText);

        panel.VersionText = null;
        Assert.Equal("", panel.VersionText);
    }

    [Fact]
    public void TheVersionStandsAtTheRightEnd_WithTheInsetOfTheVersionStripOfASettingsWindow_AndAnEmptyTextShowsNone()
    {
        VersionPanel panel = Shown(UIStyles.VersionPanels.CreateStandard());
        panel.VersionText = "v1.2.3";
        Application.DoEvents();

        using (Bitmap picture = Render(panel))
        {
            (int first, int last) = InkColumns(picture, picture.Width);

            Assert.True(first > panel.Width / 2, $"{first}");
            Assert.True(last < panel.Width - 8, $"{last}");
            Assert.True(last > panel.Width - 20, $"{last}");
        }

        panel.VersionText = "";
        Application.DoEvents();

        using (Bitmap picture = Render(panel))
            Assert.Equal(-1, InkColumns(picture, picture.Width).First);
    }

    [Fact]
    public void ThereIsNoUpdateButton_UntilAnUpdateIsKnown()
    {
        VersionPanel panel = Shown(UIStyles.VersionPanels.CreateStandard());

        Assert.False(panel.UpdateAvailable);
        Assert.Empty(panel.Controls.OfType<Button>());
    }

    [Fact]
    public void ShowUpdate_PutsTheGreenButtonLeftOfTheVersion_WithTheTooltip_AndHideUpdateTakesItAway()
    {
        VersionPanel panel = Shown(UIStyles.VersionPanels.CreateStandard());
        panel.VersionText = "v1.2.3";

        panel.ShowUpdate(Updater(), Result(), new Version(1, 0, 0));
        Application.DoEvents();

        Button button = panel.Controls.OfType<Button>().Single();
        Assert.True(panel.UpdateAvailable);
        Assert.Equal(UIStyles.Symbols.Update, button.Text);
        Assert.Equal(UIStrings.Get("Update.AvailableTooltip"), ButtonToolTipReader.Text(button));
        Assert.Equal(UIStyles.Colors.Sets.Green.Basic.ToArgb(), button.BackColor.ToArgb());
        Assert.True(button.Left >= 0 && button.Right <= panel.Width && button.Top >= 0 && button.Bottom <= panel.Height);
        Assert.Equal((panel.Height - button.Height) / 2, button.Top);

        int buttonRight = button.Right;

        panel.HideUpdate();
        Assert.False(panel.UpdateAvailable);
        Assert.Empty(panel.Controls.OfType<Button>());
        Assert.True(button.IsDisposed);

        // The text stands where it stood, right of the place of the button (the button was left of it, not over it).
        using Bitmap picture = Render(panel);
        Assert.True(InkColumns(picture, picture.Width).First >= buttonRight, $"{InkColumns(picture, picture.Width).First} >= {buttonRight}");
    }

    [Fact]
    public void WithoutAVersionText_TheButtonSitsAtTheRightInset_AndATextThatChangesMovesIt()
    {
        VersionPanel panel = Shown(UIStyles.VersionPanels.CreateStandard());
        panel.ShowUpdate(Updater(), Result());
        Button button = panel.Controls.OfType<Button>().Single();

        panel.VersionText = "";
        int withoutText = button.Right;

        panel.VersionText = "v1.2.3";
        int withText = button.Right;

        panel.VersionText = "v1.2.3 with a much longer text";
        int withLongText = button.Right;

        Assert.True(withoutText > withText);
        Assert.True(withText > withLongText);
    }

    [Fact]
    public void ShowUpdateAgain_ReplacesTheButton_AndAMissingUpdaterOrResultIsRefused()
    {
        VersionPanel panel = Shown(UIStyles.VersionPanels.CreateStandard());
        AppUpdater updater = Updater();
        panel.ShowUpdate(updater, Result());
        Button first = panel.Controls.OfType<Button>().Single();

        panel.ShowUpdate(updater, Result());

        Assert.Single(panel.Controls.OfType<Button>());
        Assert.True(first.IsDisposed);
        Assert.Throws<ArgumentNullException>(() => panel.ShowUpdate(null!, Result()));
        Assert.Throws<ArgumentNullException>(() => panel.ShowUpdate(updater, null!));
    }

    // Like in a program: the answer comes back to the thread of the window (the message loop is pumped by hand).
    private static bool Check(VersionPanel panel, AppUpdater updater, Version current)
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

        try
        {
            Task<bool> check = panel.CheckForUpdateAsync(updater, TimeSpan.FromSeconds(5), current);
            DateTime end = DateTime.UtcNow.AddSeconds(10);

            while (!check.IsCompleted && DateTime.UtcNow < end)
            {
                Application.DoEvents();
                Thread.Sleep(5);
            }

            return check.GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [Fact]
    public void CheckForUpdateAsync_ShowsTheButtonWhenThereIsANewerVersion_AndOtherwiseLeavesTheStripAlone()
    {
        VersionPanel panel = Shown(UIStyles.VersionPanels.CreateStandard());

        Assert.True(Check(panel, Updater(), new Version(1, 0, 0)));
        Assert.True(panel.UpdateAvailable);

        panel.HideUpdate();
        using HttpClient nothing = new HttpClient(new FakeGitHub(null!));
        AppUpdater unreachable = new AppUpdater("owner", "repo", nothing, nothing);

        Assert.False(Check(panel, unreachable, new Version(1, 0, 0)));
        Assert.False(panel.UpdateAvailable);

        // The same version is not an update.
        Assert.False(Check(panel, Updater(), new Version(99, 0, 0)));
        Assert.False(panel.UpdateAvailable);
    }

    [Fact]
    public void TheClickAsksTheWindowThePanelIsInAtTheTime_AndNotTheOneItWasMadeIn()
    {
        // The panel is made before it is put in a window: the button must not have fixed an owner by then.
        using VersionPanel panel = UIStyles.VersionPanels.CreateStandard();
        panel.ShowUpdate(Updater(), Result());

        Assert.Null(panel.FindForm());
        Assert.True(panel.UpdateAvailable);
    }

    [Fact]
    public void TheColors_AreTheDarkestBackground_ThePrimaryAccent_AndTheBasicColorOfASet_AndFollowTheAccent()
    {
        UIColorSet green = UIStyles.Colors.Sets.Green;
        Color accent = UIColors.Primary;

        try
        {
            using VersionPanel standard = UIStyles.VersionPanels.CreateStandard();
            using VersionPanel inGreen = UIStyles.VersionPanels.CreateStandard(green);
            using VersionPanel primary = UIStyles.VersionPanels.CreatePrimary();

            Assert.Equal(UIColors.BackgroundBlack, standard.BackColor);
            Assert.Equal(green.Basic, inGreen.BackColor);
            Assert.Equal(UIColors.PrimaryDarkDark, primary.BackColor);
            Assert.Equal(DockStyle.Bottom, standard.Dock);

            UIColors.SetAccent(Color.OrangeRed);
            Application.DoEvents();
            VersionPanel shown = Shown(UIStyles.VersionPanels.CreatePrimary());
            Assert.Equal(UIColors.PrimaryDarkDark, shown.BackColor);
        }
        finally
        {
            UIColors.SetAccent(accent);
        }
    }
}
