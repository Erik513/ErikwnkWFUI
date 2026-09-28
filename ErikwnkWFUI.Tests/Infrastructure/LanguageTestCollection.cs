namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// UIStrings.Language mutates static state shared by the whole process,
/// same class of problem as ClipboardTestCollection/AccentColorTestCollection -
/// any class that reads a translated string needs to run sequentially with
/// any class that can change the active language via LanguageTestHelper.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class LanguageTestCollection
{
    public const string Name = "Language tests";
}
