namespace ErikwnkWFUI.Tests;

/// <summary>
/// The Windows clipboard is a single process/system-global resource - xUnit
/// runs different test classes in parallel by default, so any two classes
/// that both call Clipboard.SetText/GetText can race each other. Every class
/// that touches the clipboard carries [Collection(Name)] so xUnit runs them
/// sequentially relative to each other instead.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class ClipboardTestCollection
{
    public const string Name = "Clipboard tests";
}
