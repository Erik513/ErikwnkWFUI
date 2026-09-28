using System;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// Shared across every control's own "does this text actually change with
/// the language, in every language this library defines" tests - reusable
/// via InternalsVisibleTo (see ErikwnkWFUI/InternalsVisibleTo.cs) the same
/// way ColorContrastHelper is: this only ever answers "is this key/action
/// safe against a language switch", never which keys or which control
/// properties actually matter - that stays per control, in its own test
/// file, same split as ColumnLayoutMath (shared math) vs each control's own
/// wiring test.
/// </summary>
internal static class LanguageTestHelper
{
    // Iterated rather than hand-listing English/German by name, so a
    // language added to UILanguage later is automatically covered by every
    // existing AssertTranslatedForEveryLanguage call too, with nothing
    // here or in any caller needing to change.
    public static readonly UILanguage[] AllLanguages = (UILanguage[])Enum.GetValues(typeof(UILanguage));

    /// <summary>
    /// Runs <paramref name="test"/> with <see cref="UIStrings.Language"/>
    /// set to <paramref name="language"/>, then always restores it to
    /// <see cref="UILanguage.English"/> (UIStrings' own documented default)
    /// afterward - same try/finally-to-a-fixed-default pattern
    /// UIAccentColorsTests already uses for UIColors.ApplyTheme.
    /// </summary>
    public static void RunWithLanguage(UILanguage language, Action test)
    {
        try
        {
            UIStrings.Language = language;
            test();
        }
        finally
        {
            UIStrings.Language = UILanguage.English;
        }
    }

    /// <summary>
    /// Confirms <paramref name="key"/> resolves to a REAL translation - not
    /// silently falling back to the raw key itself, which is exactly what
    /// UIStrings.Get does for an entry missing from a given language's own
    /// dictionary. Checking every language this library currently defines
    /// is the actual point: a key present in English but never added to
    /// German (or vice versa) is precisely the gap "protected against a
    /// language change" means catching.
    /// </summary>
    public static void AssertTranslatedForEveryLanguage(string key)
    {
        foreach (UILanguage language in AllLanguages)
        {
            RunWithLanguage(language, () =>
            {
                string value = UIStrings.Get(key);

                Assert.False(value == key,
                    $"'{key}' has no {language} translation - UIStrings.Get fell back to the raw key.");
            });
        }
    }
}
