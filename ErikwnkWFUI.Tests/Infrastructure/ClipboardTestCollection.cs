namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// The tests use a clipboard in memory (see <see cref="TestClipboard"/>), but it is still one
/// content for the whole run - xUnit runs different test classes in parallel by default, so
/// any two classes that both set and read it can race each other. Every class that touches
/// it carries [Collection(Name)] so xUnit runs them sequentially relative to each other.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class ClipboardTestCollection
{
    public const string Name = "Clipboard tests";
}
