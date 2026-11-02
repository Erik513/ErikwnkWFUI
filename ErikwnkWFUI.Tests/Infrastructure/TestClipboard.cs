using System.Runtime.CompilerServices;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// A clipboard in memory, used by every test instead of the system's: other programs (a clipboard
/// manager, a browser) can hold or change the real one at any moment, which made the paste
/// tests fail now and then, and a test run overwrote the clipboard of whoever ran it. It is put
/// in place once, when the test assembly loads. Tests use the members here instead of
/// <c>Clipboard</c>; the classes that do are in the "Clipboard tests" collection, as the
/// content is shared.
/// </summary>
public sealed class TestClipboard : IClipboard
{
    private static readonly TestClipboard Instance = new TestClipboard();

    private DataObject? _data;

    [ModuleInitializer]
    internal static void Install()
    {
        ClipboardAccess.Current = Instance;
    }

    public static void SetText(string text)
    {
        Instance._data = new DataObject(DataFormats.UnicodeText, text);
    }

    public static string GetText()
    {
        return Instance.GetTextCore();
    }

    public static bool ContainsText()
    {
        return Instance.ContainsTextCore();
    }

    public static void Clear()
    {
        Instance._data = null;
    }

    public static IDataObject? GetDataObject()
    {
        return Instance._data;
    }

    void IClipboard.SetDataObject(DataObject data, bool copy)
    {
        _data = data;
    }

    bool IClipboard.ContainsText()
    {
        return ContainsTextCore();
    }

    string IClipboard.GetText()
    {
        return GetTextCore();
    }

    private bool ContainsTextCore()
    {
        return _data != null && _data.GetDataPresent(DataFormats.UnicodeText, true);
    }

    private string GetTextCore()
    {
        if (_data == null)
            return string.Empty;

        return _data.GetData(DataFormats.UnicodeText, true) as string ?? string.Empty;
    }
}
