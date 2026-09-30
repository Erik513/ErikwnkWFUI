using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Tests.Infrastructure;

[Collection(LanguageTestCollection.Name)]
public class LanguageTestHelperTests
{
    [Fact]
    public void RunWithLanguage_SwitchesLanguageForTheDurationOfTheAction()
    {
        UILanguage? observed = null;

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            observed = UIStrings.Language;
        });

        Assert.Equal(UILanguage.German, observed);
        Assert.Equal(UILanguage.English, UIStrings.Language); // restored afterward
    }

    [Fact]
    public void RunWithLanguage_RestoresEnglishEvenWhenTheActionThrows()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            LanguageTestHelper.RunWithLanguage(UILanguage.German, () => throw new InvalidOperationException());
        });

        Assert.Equal(UILanguage.English, UIStrings.Language);
    }

    [Fact]
    public void AssertTranslatedForEveryLanguage_ARealKey_Passes()
    {
        // Present in both English and German dictionaries (see UIStrings).
        Exception? exception = Record.Exception(() => LanguageTestHelper.AssertTranslatedForEveryLanguage("MessageBox.Cancel"));

        Assert.Null(exception);
    }

    [Fact]
    public void AssertTranslatedForEveryLanguage_AKeyMissingFromEveryLanguage_Fails()
    {
        // UIStrings.Get falls back to the raw key itself when a key exists
        // in neither dictionary - exactly the failure this helper exists
        // to catch.
        Assert.Throws<Xunit.Sdk.FalseException>(() =>
            LanguageTestHelper.AssertTranslatedForEveryLanguage("ThisKeyDoesNotExistAnywhere"));
    }

    [Fact]
    public void AssertAllTranslatedForEveryLanguage_RealKeys_Pass()
    {
        Exception? exception = Record.Exception(() =>
            LanguageTestHelper.AssertAllTranslatedForEveryLanguage(new[] { "MessageBox.Cancel", "InfoPopup.None" }));

        Assert.Null(exception);
    }

    [Fact]
    public void AssertAllTranslatedForEveryLanguage_NamesEveryMissingKey()
    {
        Xunit.Sdk.TrueException exception = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            LanguageTestHelper.AssertAllTranslatedForEveryLanguage(new[] { "MessageBox.Cancel", "Missing.One", "Missing.Two" }));

        Assert.Contains("Missing.One", exception.Message);
        Assert.Contains("Missing.Two", exception.Message);
        Assert.DoesNotContain("MessageBox.Cancel", exception.Message);
    }
}
