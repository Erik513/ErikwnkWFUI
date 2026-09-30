namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// Tests that show a real form and depend on which window has the focus
/// (typing into a control, a text box losing the focus, ...). Focus and the
/// active window are shared by the whole process' windows, so a parallel
/// test that shows and activates its own form can take the focus away in
/// the middle of one of these - the same class of problem as
/// ClipboardTestCollection, and solved the same way: these run on their own.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class FocusTestCollection
{
    public const string Name = "Focus tests";
}
