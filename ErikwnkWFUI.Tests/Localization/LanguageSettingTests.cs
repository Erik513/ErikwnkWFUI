using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using PropertyTable = ErikwnkWFUI.Controls.PropertyTable;

namespace ErikwnkWFUI.Tests.Localization;

/// <summary>ONE language setting: the library's and the program's own texts follow each other, and the texts of a program (rows, controls) follow the language when they are given as a function.</summary>
[Collection(AccentColorTestCollection.Name)]
public class LanguageSettingTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();

    public void Dispose()
    {
        _host.Dispose();
        UIStrings.Language = UILanguage.English;
        AppLocalization.Language = AppLanguage.English;
    }

    private static int Listeners()
    {
        FieldInfo? field = typeof(UIStrings).GetField("LanguageChanged", BindingFlags.NonPublic | BindingFlags.Static);

        return ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0;
    }

    // ---- one setting

    [Fact]
    public void SettingTheLanguageOfTheLibrary_SetsTheLanguageOfTheProgramsOwnTexts_Too()
    {
        UIStyles.Language = UILanguage.German;

        Assert.Equal(AppLanguage.German, AppLocalization.Language);

        UIStyles.Language = UILanguage.English;

        Assert.Equal(AppLanguage.English, AppLocalization.Language);
    }

    [Fact]
    public void SettingTheLanguageOfTheProgramsOwnTexts_SetsTheLanguageOfTheLibrary_Too()
    {
        AppLocalization.Language = AppLanguage.German;

        Assert.Equal(UILanguage.German, UIStyles.Language);
        Assert.Equal("Schließen", UIStrings.Get("TitleBar.Close"));

        AppLocalization.Language = AppLanguage.English;

        Assert.Equal(UILanguage.English, UIStyles.Language);
    }

    [Fact]
    public void ASwitchRaisesTheOneEventOnce_WhicheverWayItWasSet_AndNothingHappensWithoutAChange()
    {
        int raised = 0;
        EventHandler handler = (sender, e) => raised++;
        UIStyles.LanguageChanged += handler;

        try
        {
            UIStyles.Language = UILanguage.German;
            Assert.Equal(1, raised);

            AppLocalization.Language = AppLanguage.English;
            Assert.Equal(2, raised);

            UIStyles.SetLanguage(AppLanguage.German);
            Assert.Equal(3, raised);

            UIStyles.SetLanguage(AppLanguage.German);
            UIStyles.Language = UILanguage.German;
            AppLocalization.Language = AppLanguage.German;
            Assert.Equal(3, raised);
        }
        finally
        {
            UIStyles.LanguageChanged -= handler;
        }

        UIStyles.Language = UILanguage.English;
        Assert.Equal(3, raised);
    }

    [Fact]
    public void TheOwnTextsOfAProgram_AreInTheLanguageThatWasSetOnTheLibrary()
    {
        AppLocalization.Register(AppLanguage.English, new Dictionary<string, string> { ["test.name"] = "Name" });
        AppLocalization.Register(AppLanguage.German, new Dictionary<string, string> { ["test.name"] = "Bezeichnung" });

        UIStyles.Language = UILanguage.German;

        Assert.Equal("Bezeichnung", AppLocalization.Get("test.name"));
    }

    // ---- texts as a function

    [Fact]
    public void ARowWithATextAsAFunction_FollowsTheLanguage_AndARowWithATextDoesNot()
    {
        using PropertyTable table = new PropertyTable();
        table.AddSection(() => UIStyles.Language == UILanguage.German ? "Eigenes" : "Own");
        table.AddRow(() => UIStyles.Language == UILanguage.German ? "Name" : "Title", new Button());
        table.AddRow(() => UIStyles.Language == UILanguage.German ? "Spalten" : "Columns", PropertyColumn.Percent(new Button(), 50f), PropertyColumn.Percent(new Button(), 50f));
        table.AddRow(() => UIStyles.Language == UILanguage.German ? "Hoch" : "Tall", 60, new Button());
        table.AddRow("Fixed", new Button());

        Assert.Equal(new[] { "Own", "Title", "Columns", "Tall", "Fixed" }, PropertyTableHost.AllLabels(table).Select(l => l.Text).ToArray());

        UIStyles.Language = UILanguage.German;

        Assert.Equal(new[] { "Eigenes", "Name", "Spalten", "Hoch", "Fixed" }, PropertyTableHost.AllLabels(table).Select(l => l.Text).ToArray());

        AppLocalization.Language = AppLanguage.English;

        Assert.Equal(new[] { "Own", "Title", "Columns", "Tall", "Fixed" }, PropertyTableHost.AllLabels(table).Select(l => l.Text).ToArray());
    }

    [Fact]
    public void TheTextOfARowCanBeBoundLater_AndNullEndsTheBinding()
    {
        using PropertyTable table = new PropertyTable();
        PropertyRow row = table.AddRow("Fixed", new Button());

        row.TextSource = () => UIStyles.Language == UILanguage.German ? "Fest" : "Fixed";
        UIStyles.Language = UILanguage.German;
        Assert.Equal("Fest", row.Text);

        row.TextSource = null;
        UIStyles.Language = UILanguage.English;
        Assert.Equal("Fest", row.Text);
    }

    [Fact]
    public void ATableWithoutRowsThatFollow_DoesNotListen_AndOneThatDoesStopsWhenItIsDisposed()
    {
        int before = Listeners();

        PropertyTable plain = new PropertyTable();
        plain.AddRow("Fixed", new Button());
        Assert.Equal(before, Listeners());

        PropertyTable following = new PropertyTable();
        following.AddRow(() => "Follows", new Button());
        following.AddRow(() => "Follows too", new Button());
        Assert.Equal(before + 1, Listeners());

        following.Dispose();
        plain.Dispose();
        Assert.Equal(before, Listeners());
    }

    [Fact]
    public void RowsThatWereCleared_AreNotAskedAnyMore_AndAFailingSourceCannotBreakTheSwitch()
    {
        using PropertyTable table = new PropertyTable();
        int asked = 0;
        table.AddRow(() => { asked++; return "Row"; }, new Button());
        table.ClearRows();
        asked = 0;

        UIStyles.Language = UILanguage.German;

        Assert.Equal(0, asked);
    }

    [Fact]
    public void ALocalizedControl_HasTheTextNow_FollowsTheLanguage_AndLetsGoWhenItIsDisposed()
    {
        int before = Listeners();
        Label label = UIStyles.Localize(UIStyles.Labels.CreateStandard(), () => UIStyles.Language == UILanguage.German ? "Hallo" : "Hello");

        Assert.Equal("Hello", label.Text);
        Assert.Equal(before + 1, Listeners());

        UIStyles.Language = UILanguage.German;
        Assert.Equal("Hallo", label.Text);

        label.Dispose();
        Assert.Equal(before, Listeners());
        Assert.Null(Record.Exception(() => UIStyles.Language = UILanguage.English));
    }

    [Fact]
    public void ALocalizedControl_CanLocalizeAnythingItShows_NotOnlyItsText()
    {
        Button button = new Button();
        int applied = 0;

        UIStyles.Localize(button, b => { applied++; b.AccessibleName = UIStyles.Language == UILanguage.German ? "Knopf" : "Button"; });

        Assert.Equal("Button", button.AccessibleName);
        Assert.Equal(1, applied);

        UIStyles.Language = UILanguage.German;
        Assert.Equal("Knopf", button.AccessibleName);
        Assert.Equal(2, applied);

        button.Dispose();
        UIStyles.Language = UILanguage.English;
        Assert.Equal(2, applied);
    }

    [Fact]
    public void LocalizeNeedsATextAndAControl()
    {
        Assert.Throws<ArgumentNullException>(() => UIStyles.Localize(new Button(), (Func<string>)null!));
        Assert.Throws<ArgumentNullException>(() => UIStyles.Localize<Button>(null!, () => "x"));
    }

    // ---- in the options window

    [Fact]
    public void TheOwnRowsOfAnOptionsWindow_FollowTheLanguagePresetWhenTheyAreGivenAsFunctions()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard(presets: new[] { OptionsPreset.Language }));
        form.PropertyTable.AddSection(() => UIStyles.Language == UILanguage.German ? "Eigene Zeilen" : "Own rows");
        form.PropertyTable.AddRow(() => UIStyles.Language == UILanguage.German ? "Benachrichtigungen" : "Notifications", new Button());

        form.GetPrivateField<System.Windows.Forms.ComboBox>("_languageBox")!.SelectedIndex = 1;

        Assert.Equal(
            new[] { "Allgemein", "Sprache", "Eigene Zeilen", "Benachrichtigungen" },
            PropertyTableHost.AllLabels(form.PropertyTable).Select(l => l.Text).ToArray());
    }
}
