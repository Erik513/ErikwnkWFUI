namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// UIColors.SetAccent mutates static state (Primary, Selection, ...) shared
/// by the whole process, same class of problem as ClipboardTestCollection -
/// any class that reads one of those values needs to run sequentially with
/// any class that can change them via SetAccent.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class AccentColorTestCollection
{
    public const string Name = "Accent color tests";
}
