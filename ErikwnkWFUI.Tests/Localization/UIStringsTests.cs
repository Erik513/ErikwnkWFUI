using System.Reflection;
using System.Text.RegularExpressions;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Localization;

/// <summary>
/// The texts of the library: both languages have the same keys with the same placeholders, nothing is left empty or copied untranslated by accident, and
/// a key that is not there shows as itself. [Collection]: the language is static state of the process.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class UIStringsTests
{
    // The texts that are the same in both languages on purpose (a word that is written alike, a name). A new entry that is only copied is not.
    private static readonly string[] SameInBothLanguages =
    {
        "InputBox.Ok", "MessageBox.Ok", "MessageBox.Details", "DataGridView.EnumerationHeader",
        "OptionsForm.Accent.Orange", "OptionsForm.Accent.Cyan", "OptionsForm.Accent.Indigo", "OptionsForm.Accent.Magenta"
    };

    private static Dictionary<string, string> Table(string name)
    {
        FieldInfo field = typeof(UIStrings).GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!;

        return (Dictionary<string, string>)field.GetValue(null)!;
    }

    private static IEnumerable<string> Placeholders(string text)
    {
        return Regex.Matches(text, @"\{(\d+)[^}]*\}").Cast<Match>().Select(match => match.Groups[1].Value).OrderBy(value => value).ToList();
    }

    [Fact]
    public void BothLanguages_HaveTheSameKeys()
    {
        Dictionary<string, string> english = Table("English");
        Dictionary<string, string> german = Table("German");

        Assert.Empty(english.Keys.Except(german.Keys));
        Assert.Empty(german.Keys.Except(english.Keys));
        Assert.NotEmpty(english);
    }

    [Fact]
    public void ATextAndItsTranslation_HaveTheSamePlaceholders()
    {
        Dictionary<string, string> english = Table("English");
        Dictionary<string, string> german = Table("German");

        foreach (string key in english.Keys.Where(german.ContainsKey))
            Assert.True(Placeholders(english[key]).SequenceEqual(Placeholders(german[key])), "the placeholders of '" + key + "' differ between the languages");
    }

    [Fact]
    public void NoTextIsEmpty_AndNoKeyIsItsOwnText()
    {
        foreach (string name in new[] { "English", "German" })
        {
            foreach (KeyValuePair<string, string> entry in Table(name))
            {
                Assert.False(string.IsNullOrWhiteSpace(entry.Value), name + ": '" + entry.Key + "' is empty");
                Assert.NotEqual(entry.Key, entry.Value);
            }
        }
    }

    [Fact]
    public void ATextThatIsTheSameInBothLanguages_IsOneOfTheKnownOnes()
    {
        Dictionary<string, string> english = Table("English");
        Dictionary<string, string> german = Table("German");

        string[] same = english.Keys.Where(key => german.ContainsKey(key) && english[key] == german[key]).OrderBy(key => key).ToArray();

        Assert.Empty(same.Except(SameInBothLanguages));
    }

    [Fact]
    public void EveryAccentOfThePalette_HasAName_InBothLanguages()
    {
        List<string> names = typeof(UIAccentColors).GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.FieldType == typeof(Color)).Select(field => field.Name).ToList();
        names.Add(nameof(UIAccentColors.BlackOrWhite));

        foreach (string name in names)
        {
            Assert.True(Table("English").ContainsKey("OptionsForm.Accent." + name), "English: no name for the accent " + name);
            Assert.True(Table("German").ContainsKey("OptionsForm.Accent." + name), "German: no name for the accent " + name);
        }

        Assert.Equal(UIAccentColors.All.Length, names.Count);
    }

    [Fact]
    public void AKeyThatIsNotThere_ShowsAsItself_AndNoKeyIsAnEmptyText_InBothLanguages()
    {
        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                Assert.Equal("No.Such.Key", UIStrings.Get("No.Such.Key"));
                Assert.Equal(string.Empty, UIStrings.Get(null!));
            });
        }
    }

    [Fact]
    public void TheNumbersAreWrittenInTheLanguageOfTheLibrary_NotOfTheComputer()
    {
        LanguageTestHelper.RunWithLanguage(UILanguage.English, () => Assert.Equal(".", UIStrings.NumberCulture.NumberFormat.NumberDecimalSeparator));
        LanguageTestHelper.RunWithLanguage(UILanguage.German, () => Assert.Equal(",", UIStrings.NumberCulture.NumberFormat.NumberDecimalSeparator));
    }
}
