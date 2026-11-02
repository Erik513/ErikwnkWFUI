using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.ColorContrastTestSupport;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The colors of the update prompt: the texts on its page, the text of the title bar in every color set, and the texts in both languages.</summary>
[Collection(AccentColorTestCollection.Name)]
public class UpdateColorTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();

    public void Dispose()
    {
        _host.Dispose();
        UIStrings.Language = UILanguage.English;
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static IEnumerable<Control> All(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;

            foreach (Control deeper in All(child))
                yield return deeper;
        }
    }

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheHeadlineAndTheVersionLine_AreReadable_OnThePage_InBothThemes(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            UpdateAvailableForm form = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1.0.0", "1.1.0"));
            Label headline = All(form).OfType<Label>().First(label => label.Text == UIStrings.Get("Update.AvailableMessage"));
            Label versions = All(form).OfType<Label>().First(label => label.Text.Contains("→"));

            Check(shortfalls, "headline", headline.ForeColor, UIColors.BackgroundMedium, ColorContrastHelper.MinimumRatioNormalText);
            Check(shortfalls, "version line", versions.ForeColor, UIColors.BackgroundMedium, ColorContrastHelper.MinimumRatioNormalText);
        });
    }

    [Fact]
    public void TheTexts_FollowTheThemeWhileTheWindowIsOpen()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UpdateAvailableForm form = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1.0.0", "1.1.0"));
        Label headline = All(form).OfType<Label>().First(label => label.Text == UIStrings.Get("Update.AvailableMessage"));
        Label versions = All(form).OfType<Label>().First(label => label.Text.Contains("→"));
        Color darkHeadline = headline.ForeColor;
        Color darkVersions = versions.ForeColor;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.NotEqual(darkHeadline.ToArgb(), headline.ForeColor.ToArgb());
        Assert.NotEqual(darkVersions.ToArgb(), versions.ForeColor.ToArgb());
        Assert.Equal(UIColors.TextPrimary.ToArgb(), headline.ForeColor.ToArgb());
    }

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheTitle_IsReadable_OnTheTitleBar_InEveryColorSet(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in AllSets())
            {
                UpdateAvailableForm form = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2", colors: set));

                Check(shortfalls, name, form.TitleBar.TitleForeColor, form.TitleBar.BackColor, 3.0);
            }

            UpdateAvailableForm primary = _host.Show(UIStyles.Forms.CreateUpdateAvailablePrimary("1", "2"));
            Check(shortfalls, "primary", primary.TitleBar.TitleForeColor, primary.TitleBar.BackColor, 3.0);
        });
    }

    [Theory]
    [InlineData(UILanguage.English)]
    [InlineData(UILanguage.German)]
    public void EveryTextOfTheWindows_ExistsInTheLanguage_AndIsNotTheKey(UILanguage language)
    {
        UIStrings.Language = language;

        foreach (string key in new[] { "Update.AvailableTitle", "Update.AvailableMessage", "Update.Later", "Update.Skip", "Update.DownloadFailed", "Update.WhatsNew", "Update.UpdateNow", "Update.Downloading" })
        {
            string text = UIStrings.Get(key);

            Assert.False(string.IsNullOrWhiteSpace(text), key);
            Assert.NotEqual(key, text);
        }

        Assert.NotEqual(
            UIStrings.Get("Update.UpdateNow"),
            language == UILanguage.English ? "Jetzt aktualisieren" : "Update now");
    }
}