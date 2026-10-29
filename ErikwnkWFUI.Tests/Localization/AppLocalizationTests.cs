using ErikwnkCore;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Localization;

/// <summary>
/// The texts of a program (ErikwnkCore.AppLocalization): a table per language, the current language first, then English, then the key itself.
/// [Collection]: the language is static state of the process, and it is one setting with the language of the library.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class AppLocalizationTests : IDisposable
{
    public void Dispose()
    {
        AppLocalization.Language = AppLanguage.English;
    }

    private static void Register(string prefix)
    {
        AppLocalization.Register(AppLanguage.English, new Dictionary<string, string>
        {
            [prefix + ".Both"] = "both (English)",
            [prefix + ".EnglishOnly"] = "only English",
            [prefix + ".Hello"] = "Hello {0}, you have {1} messages"
        });
        AppLocalization.Register(AppLanguage.German, new Dictionary<string, string>
        {
            [prefix + ".Both"] = "beide (Deutsch)",
            [prefix + ".Hello"] = "Hallo {0}, du hast {1} Nachrichten"
        });
    }

    [Fact]
    public void ATextIsTakenFromTheCurrentLanguage_ThenFromEnglish_ThenItIsTheKeyItself()
    {
        Register("Loc.A");

        AppLocalization.Language = AppLanguage.English;
        Assert.Equal("both (English)", AppLocalization.Get("Loc.A.Both"));

        AppLocalization.Language = AppLanguage.German;
        Assert.Equal("beide (Deutsch)", AppLocalization.Get("Loc.A.Both"));
        Assert.Equal("only English", AppLocalization.Get("Loc.A.EnglishOnly"));
        Assert.Equal("Loc.A.Nothing", AppLocalization.Get("Loc.A.Nothing"));
    }

    [Fact]
    public void NoKey_IsAnEmptyText()
    {
        Assert.Equal(string.Empty, AppLocalization.Get(null));
    }

    [Fact]
    public void AnEntryThatIsRegisteredAgain_ReplacesTheOldOne_AndTheOthersStay()
    {
        Register("Loc.B");
        AppLocalization.Register(AppLanguage.English, new Dictionary<string, string> { ["Loc.B.Both"] = "both, new" });

        Assert.Equal("both, new", AppLocalization.Get("Loc.B.Both"));
        Assert.Equal("only English", AppLocalization.Get("Loc.B.EnglishOnly"));
    }

    [Fact]
    public void RegisteringNothing_ChangesNothing()
    {
        Assert.Null(Record.Exception(() => AppLocalization.Register(AppLanguage.German, null)));
    }

    [Fact]
    public void ATextWithPlaceholders_IsFormattedInTheCurrentLanguage()
    {
        Register("Loc.C");

        Assert.Equal("Hello Anna, you have 3 messages", AppLocalization.Get("Loc.C.Hello", "Anna", 3));

        AppLocalization.Language = AppLanguage.German;
        Assert.Equal("Hallo Anna, du hast 3 Nachrichten", AppLocalization.Get("Loc.C.Hello", "Anna", 3));
    }

    [Fact]
    public void ASwitchOfTheLanguage_RaisesTheEventOnce_AndTheSameLanguageDoesNot()
    {
        int raised = 0;
        EventHandler handler = (sender, e) => raised++;
        AppLocalization.Language = AppLanguage.English;
        AppLocalization.LanguageChanged += handler;

        try
        {
            AppLocalization.Language = AppLanguage.English;
            Assert.Equal(0, raised);

            AppLocalization.Language = AppLanguage.German;
            Assert.Equal(1, raised);

            AppLocalization.Language = AppLanguage.German;
            Assert.Equal(1, raised);
        }
        finally
        {
            AppLocalization.LanguageChanged -= handler;
        }
    }
}
