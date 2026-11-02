using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// A clipboard that another program holds (a clipboard manager, a remote session): every call fails with the exception the system's clipboard
/// throws then. <see cref="Use"/> puts it in place of the one of the tests until the returned object is disposed. The classes that use it are in the
/// "Clipboard tests" collection, as the clipboard of the tests is shared.
/// </summary>
internal sealed class BusyClipboard : IClipboard
{
    public static IDisposable Use()
    {
        IClipboard before = ClipboardAccess.Current;
        ClipboardAccess.Current = new BusyClipboard();

        return new Restore(before);
    }

    public void SetDataObject(DataObject data, bool copy)
    {
        throw new ExternalException("Requested Clipboard operation did not succeed.");
    }

    public bool ContainsText()
    {
        throw new ExternalException("Requested Clipboard operation did not succeed.");
    }

    public string GetText()
    {
        throw new ExternalException("Requested Clipboard operation did not succeed.");
    }

    private sealed class Restore : IDisposable
    {
        private readonly IClipboard _before;

        public Restore(IClipboard before)
        {
            _before = before;
        }

        public void Dispose()
        {
            ClipboardAccess.Current = _before;
        }
    }
}
