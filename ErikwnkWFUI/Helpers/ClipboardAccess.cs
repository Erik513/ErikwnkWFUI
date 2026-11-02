using System.Runtime.InteropServices;
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
    /// The clipboard belongs to the whole system and another program (a clipboard manager, a remote
    /// session) can hold it for a moment: that is never an exception for the caller here.
    /// </summary>
    internal static class ClipboardAccess
    {
        internal static IClipboard Current = new SystemClipboard();

        /// <summary>
        /// Puts the content on the clipboard. False when another program holds the clipboard (nothing was copied): what must only follow
        /// a copy that worked - the clearing of a cut - has to ask.
        /// </summary>
        internal static bool TrySetDataObject(DataObject data, bool copy = false)
        {
            try
            {
                Current.SetDataObject(data, copy);

                return true;
            }
            catch (ExternalException)
            {
                return false;
            }
        }

        /// <summary>Whether there is text; false also when another program holds the clipboard.</summary>
        internal static bool ContainsText()
        {
            try
            {
                return Current.ContainsText();
            }
            catch (ExternalException)
            {
                return false;
            }
        }

        /// <summary>The text; empty also when another program holds the clipboard.</summary>
        internal static string GetText()
        {
            try
            {
                return Current.GetText();
            }
            catch (ExternalException)
            {
                return string.Empty;
            }
        }
    }
}
