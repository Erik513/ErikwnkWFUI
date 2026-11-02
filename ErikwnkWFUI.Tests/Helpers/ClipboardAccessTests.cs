using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>
/// The one way the controls reach the clipboard: a clipboard that another program holds is never an exception for them, and what must only
/// follow a copy that worked (the clearing of a cut) can ask whether it did. [Collection]: the clipboard of the tests is shared.
/// </summary>
[Collection(ClipboardTestCollection.Name)]
public class ClipboardAccessTests
{
    private sealed class BrokenClipboard : IClipboard
    {
        public void SetDataObject(DataObject data, bool copy) => throw new InvalidOperationException("not the clipboard's doing");

        public bool ContainsText() => throw new InvalidOperationException("not the clipboard's doing");

        public string GetText() => throw new InvalidOperationException("not the clipboard's doing");
    }

    [Fact]
    public void WhenAnotherProgramHoldsTheClipboard_NothingThrows_ACopyIsReportedAsFailed_AndThereIsNoText()
    {
        using (BusyClipboard.Use())
        {
            Assert.False(ClipboardAccess.TrySetDataObject(new DataObject(DataFormats.UnicodeText, "x")));
            Assert.False(ClipboardAccess.ContainsText());
            Assert.Equal(string.Empty, ClipboardAccess.GetText());
        }
    }

    [Fact]
    public void WhenTheClipboardIsFree_ACopyIsReportedAsDone_AndTheTextIsThere()
    {
        TestClipboard.Clear();

        Assert.True(ClipboardAccess.TrySetDataObject(new DataObject(DataFormats.UnicodeText, "hello")));
        Assert.True(ClipboardAccess.ContainsText());
        Assert.Equal("hello", ClipboardAccess.GetText());
    }

    [Fact]
    public void AFailureThatIsNotTheClipboardBeingBusy_IsNotHidden()
    {
        IClipboard before = ClipboardAccess.Current;
        ClipboardAccess.Current = new BrokenClipboard();

        try
        {
            Assert.Throws<InvalidOperationException>(() => ClipboardAccess.TrySetDataObject(new DataObject()));
            Assert.Throws<InvalidOperationException>(() => ClipboardAccess.ContainsText());
            Assert.Throws<InvalidOperationException>(() => ClipboardAccess.GetText());
        }
        finally
        {
            ClipboardAccess.Current = before;
        }
    }

    [Fact]
    public void TheBusyClipboard_ThrowsWhatTheSystemClipboardThrows_AndIsPutBackAfterwards()
    {
        IClipboard before = ClipboardAccess.Current;

        using (BusyClipboard.Use())
            Assert.Throws<ExternalException>(() => ClipboardAccess.Current.GetText());

        Assert.Same(before, ClipboardAccess.Current);
    }
}
