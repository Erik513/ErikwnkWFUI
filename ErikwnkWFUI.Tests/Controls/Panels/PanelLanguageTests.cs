using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using ErikwnkCore.Updater;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Panels;

/// <summary>
/// What the panels say in words: the names of the arrows of a multi panel (for a screen reader; their tooltips are the titles of the
/// pages) and the tooltip of the update button of a version panel. The keys exist in every language, and a language switch reaches
/// panels that are already on screen.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class PanelLanguageTests
{
    [Fact]
    public void EveryKey_IsTranslatedForEveryLanguage()
    {
        LanguageTestHelper.AssertAllTranslatedForEveryLanguage(new[]
        {
            "MultiPanel.PreviousPage",
            "MultiPanel.NextPage",
            "Update.AvailableTooltip"
        });
    }

    private static Button[] Arrows(MultiPanel panel)
    {
        return panel.Controls.OfType<Button>().OrderBy(button => button.Left).ToArray();
    }

    [Fact]
    public void TheNamesOfTheArrows_AreInTheLanguage_AndFollowALiveSwitch()
    {
        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            using MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
            panel.AddPage("A");
            panel.AddPage("B");

            Assert.Equal("Previous page", Arrows(panel)[0].AccessibleName);
            Assert.Equal("Next page", Arrows(panel)[1].AccessibleName);

            UIStrings.Language = UILanguage.German;

            Assert.Equal("Vorherige Seite", Arrows(panel)[0].AccessibleName);
            Assert.Equal("Nächste Seite", Arrows(panel)[1].AccessibleName);

            UIStrings.Language = UILanguage.English;

            Assert.Equal("Next page", Arrows(panel)[1].AccessibleName);
        });
    }

    [Fact]
    public void AMultiPanelMadeInGerman_HasGermanNamesAtOnce_AndTheTooltipIsStillTheTitleOfThePage()
    {
        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            using MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
            panel.AddPage("Erste");
            panel.AddPage("Zweite");

            Assert.Equal("Vorherige Seite", Arrows(panel)[0].AccessibleName);
            Assert.Equal("Zweite", ButtonToolTipReader.Text(Arrows(panel)[1]));

            UIStrings.Language = UILanguage.English;

            Assert.Equal("Zweite", ButtonToolTipReader.Text(Arrows(panel)[1]));
            Assert.Equal("Next page", Arrows(panel)[1].AccessibleName);
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference MakeAndDispose()
    {
        MultiPanel panel = UIStyles.MultiPanels.CreateStandard();
        panel.AddPage("A");
        panel.Dispose();

        return new WeakReference(panel);
    }

    [Fact]
    public void ADisposedMultiPanel_IsNotHeldByTheLanguageSwitch()
    {
        WeakReference reference = MakeAndDispose();

        for (int i = 0; i < 3 && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(reference.IsAlive);
        LanguageTestHelper.RunWithLanguage(UILanguage.German, () => { });
    }

    [Fact]
    public void TheUpdateButtonOfAVersionPanel_FollowsALiveLanguageSwitch()
    {
        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            using HttpClient client = new HttpClient();
            AppUpdater updater = new AppUpdater("owner", "repo", client, client);
            using VersionPanel panel = UIStyles.VersionPanels.CreateStandard();
            panel.ShowUpdate(updater, new UpdateCheckResult(new Version(2, 0, 0), "", null), new Version(1, 0, 0));
            Button button = panel.Controls.OfType<Button>().Single();
            string english = ButtonToolTipReader.Text(button);

            Assert.Equal(UIStrings.Get("Update.AvailableTooltip"), english);

            UIStrings.Language = UILanguage.German;

            Assert.NotEqual(english, ButtonToolTipReader.Text(button));
            Assert.Equal(UIStrings.Get("Update.AvailableTooltip"), ButtonToolTipReader.Text(button));
        });
    }

    [Fact]
    public void TheVersionText_DoesNotDependOnTheLanguage()
    {
        string english = LanguageTestHelper_Version(UILanguage.English);
        string german = LanguageTestHelper_Version(UILanguage.German);

        Assert.Equal(english, german);
        Assert.StartsWith("v", english);
    }

    private static string LanguageTestHelper_Version(UILanguage language)
    {
        string text = "";
        LanguageTestHelper.RunWithLanguage(language, () =>
        {
            using VersionPanel panel = UIStyles.VersionPanels.CreateStandard();
            text = panel.VersionText;
        });

        return text;
    }
}
