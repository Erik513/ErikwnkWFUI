using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The question of the update follows a change of the language while it is open: its title, headline and buttons, and the window stays wide enough.</summary>
[Collection(AccentColorTestCollection.Name)]
public class UpdateLanguageTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();

    public void Dispose()
    {
        _host.Dispose();
        UIStrings.Language = UILanguage.English;
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

    private static string[] Texts(Form form)
    {
        return All(form).OfType<Control>().Where(control => control is Button || control is Label).Select(control => control.Text).ToArray();
    }

    [Fact]
    public void TheTexts_FollowTheLanguage_WhileTheWindowIsOpen_AndComeBack()
    {
        UpdateAvailableForm form = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2", releaseNotes: "text", allowSkip: true));

        Assert.Equal("Update available", form.Text);
        Assert.Contains("A new version is available.", Texts(form));
        Assert.Contains("Later", Texts(form));

        UIStrings.Language = UILanguage.German;

        Assert.Equal("Update verfügbar", form.Text);
        Assert.Contains("Eine neue Version ist verfügbar.", Texts(form));
        Assert.Contains("Später", Texts(form));
        Assert.Contains("Jetzt aktualisieren", Texts(form));
        Assert.Contains("Diese Version überspringen", Texts(form));
        Assert.Contains(Texts(form), text => text.StartsWith("Neuigkeiten"));
        Assert.DoesNotContain("Later", Texts(form));

        UIStrings.Language = UILanguage.English;

        Assert.Equal("Update available", form.Text);
        Assert.Contains("Update now", Texts(form));
        Assert.Contains(Texts(form), text => text.StartsWith("What's new"));
    }

    [Fact]
    public void ATitleThatWasGiven_Stays()
    {
        UpdateAvailableForm form = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2", "My update"));

        UIStrings.Language = UILanguage.German;

        Assert.Equal("My update", form.Text);
        Assert.Contains("Später", Texts(form));
    }

    [Theory]
    [InlineData(UILanguage.English, UILanguage.German)]
    [InlineData(UILanguage.German, UILanguage.English)]
    public void AfterTheSwitch_BothButtonsStayInsideTheWindow_AndTheWindowGrowsWhereNeeded(UILanguage from, UILanguage to)
    {
        UIStrings.Language = from;
        UpdateAvailableForm form = _host.Show(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2", releaseNotes: "text", allowSkip: true));
        form.NotesExpanded = true;

        UIStrings.Language = to;
        TestPictures.Pump(100);

        Control surface = form.ContentPanel;
        foreach (Button button in All(form).OfType<Button>().Where(b => b.Visible && b.Text.Length > 0))
        {
            Rectangle area = surface.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));

            Assert.True(area.Left >= 0 && area.Right <= surface.ClientSize.Width, $"{button.Text} {area} in {surface.ClientSize}");
        }

        // The buttons of the left and of the right do not overlap.
        Button first = All(form).OfType<Button>().First(b => b.Text.StartsWith("What") || b.Text.StartsWith("Neuig"));
        Button later = All(form).OfType<Button>().First(b => b.Text == "Later" || b.Text == "Später");
        Rectangle skipBox = first.RectangleToScreen(first.ClientRectangle);
        Assert.True(skipBox.Right <= later.RectangleToScreen(later.ClientRectangle).Left);
    }

    [Fact]
    public void ADisposedWindow_HearsNothingMore_AndASwitchDoesNotThrow()
    {
        UpdateAvailableForm form = new UpdateAvailableForm("1", "2");
        form.Dispose();

        UIStrings.Language = UILanguage.German;
        UIStrings.Language = UILanguage.English;
    }
}