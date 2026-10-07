using System.Windows.Forms;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>The clipboard as the controls use it: setting content, asking for text, getting text.</summary>
    internal interface IClipboard
    {
        void SetDataObject(DataObject data, bool copy);

        bool ContainsText();

        string GetText();
    }

    /// <summary>The real clipboard of the system.</summary>
    internal sealed class SystemClipboard : IClipboard
    {
        public void SetDataObject(DataObject data, bool copy)
        {
            Clipboard.SetDataObject(data, copy);
        }

        public bool ContainsText()
        {
            return Clipboard.ContainsText();
        }

        public string GetText()
        {
            return Clipboard.GetText();
        }
    }

    /// <summary>
    /// The one way the grids and the list view reach the clipboard. It is the system's clipboard;
    /// a test puts one in memory in its place, so it neither depends on what other programs do
    /// with the clipboard at that moment nor overwrites the clipboard of the person running it.
    /// </summary>
    internal static class ClipboardAccess
    {
        internal static IClipboard Current = new SystemClipboard();

        internal static void SetDataObject(DataObject data, bool copy = false)
        {
            Current.SetDataObject(data, copy);
        }

        internal static bool ContainsText()
        {
            return Current.ContainsText();
        }

        internal static string GetText()
        {
            return Current.GetText();
        }
    }
}
