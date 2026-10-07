using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// The tests never touch the real clipboard: the one the controls use is the one in memory, and it
/// behaves like the system's for text.
/// </summary>
[Collection(ClipboardTestCollection.Name)]
public class TestClipboardTests
{
    [Fact]
    public void TheControlsUseTheClipboardInMemory_DuringTheTests()
    {
        Assert.IsType<TestClipboard>(ClipboardAccess.Current);
    }

    [Fact]
    public void TextGoesIn_AndComesOut_AndClearEmptiesIt()
    {
        TestClipboard.SetText("A	1");
        Assert.True(ClipboardAccess.ContainsText());
        Assert.Equal("A	1", ClipboardAccess.GetText());

        TestClipboard.Clear();
        Assert.False(ClipboardAccess.ContainsText());
        Assert.Equal(string.Empty, ClipboardAccess.GetText());
    }
}
