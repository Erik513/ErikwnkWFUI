using System.Net.Http;
using System.Windows.Forms;
using ErikwnkCore.Updater;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Buttons;

/// <summary>
/// A button has no text of its own: the caller gives the tooltip. What the library
/// translates itself are the tooltips of the title bar's buttons and of the "update
/// available" button. Two things matter, as for every control: the keys exist in every
/// language, and a language switch reaches buttons that are already on screen.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class ButtonLanguageTests
{
    [Fact]
    public void EveryKey_IsTranslatedForEveryLanguage()
    {
        LanguageTestHelper.AssertAllTranslatedForEveryLanguage(new[]
        {
            "TitleBar.Minimize",
            "TitleBar.Maximize",
            "TitleBar.Restore",
            "TitleBar.Close",
            "Update.AvailableTooltip"
        });
    }

    private static Button Field(TitleBarControl bar, string field)
    {
        return (Button)typeof(TitleBarControl)
            .GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(bar)!;
    }

    private static string Tip(TitleBarControl bar, string field)
    {
        return ButtonToolTipReader.Text(Field(bar, field));
    }

    [Fact]
    public void TheTitleBarTooltips_FollowALiveLanguageSwitch()
    {
        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            using TitleBarControl bar = new TitleBarControl();

            Assert.Equal("Minimize", Tip(bar, "_minimizeButton"));
            Assert.Equal("Maximize", Tip(bar, "_maximizeButton"));
            Assert.Equal("Close", Tip(bar, "_closeButton"));

            UIStrings.Language = UILanguage.German;

            Assert.Equal("Minimieren", Tip(bar, "_minimizeButton"));
            Assert.Equal("Maximieren", Tip(bar, "_maximizeButton"));
            Assert.Equal("Schließen", Tip(bar, "_closeButton"));

            // A screen reader gets the same words, not the symbol character.
            Assert.Equal("Minimieren", Field(bar, "_minimizeButton").AccessibleName);
            Assert.Equal("Maximieren", Field(bar, "_maximizeButton").AccessibleName);
            Assert.Equal("Schließen", Field(bar, "_closeButton").AccessibleName);
        });
    }

    [Fact]
    public void TheMaximizeTooltip_SaysRestoreWhileMaximized_InEveryLanguage_AndAfterASwitch()
    {
        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            using TitleBarControl bar = new TitleBarControl();

            bar.UpdateMaximizeButton(true);
            Assert.Equal("Restore", Tip(bar, "_maximizeButton"));

            UIStrings.Language = UILanguage.German;
            Assert.Equal("Wiederherstellen", Tip(bar, "_maximizeButton"));

            bar.UpdateMaximizeButton(false);
            Assert.Equal("Maximieren", Tip(bar, "_maximizeButton"));
        });
    }

    [Fact]
    public void TheUpdateAvailableButton_FollowsALiveLanguageSwitch()
    {
        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            using HttpClient client = new HttpClient();
            AppUpdater updater = new AppUpdater("owner", "repo", client, client);
            UpdateCheckResult result = new UpdateCheckResult(new Version(2, 0, 0), "https://example.com/release", null);

            using Form owner = new Form();
            using Button button = updater.CreateUpdateAvailableButton(result, new Version(1, 0, 0), owner);

            Assert.Equal(UIStrings.Get("Update.AvailableTooltip"), ButtonToolTipReader.Text(button));
            string english = ButtonToolTipReader.Text(button);

            UIStrings.Language = UILanguage.German;

            Assert.NotEqual(english, ButtonToolTipReader.Text(button));
            Assert.Equal(UIStrings.Get("Update.AvailableTooltip"), ButtonToolTipReader.Text(button));
            Assert.Equal(UIStyles.Symbols.Update, button.Text);
        });
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDisposeATitleBar()
    {
        TitleBarControl bar = new TitleBarControl();
        WeakReference reference = new WeakReference(bar);
        bar.Dispose();

        return reference;
    }

    [Fact]
    public void ATitleBarThatWasDisposed_IsNotKeptAliveByTheLanguageEvent()
    {
        // UIStrings.LanguageChanged is static: a bar still subscribed would live forever.
        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            WeakReference reference = CreateAndDisposeATitleBar();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(reference.IsAlive, "the title bar is still referenced after Dispose");
        });
    }
}
