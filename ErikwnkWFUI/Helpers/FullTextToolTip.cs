using System.Windows.Forms;
using ErikwnkWFUI.Factories;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// The tooltip with the whole text of a control that cannot show it all (a text that is cut or moves, on a label or a progress bar):
    /// the control tells it the text to show, or an empty one when everything is visible, and it makes the standard hover tooltip of the
    /// library (the one the other controls use) show it or not. The tooltip window is only made when there is a text to show.
    /// </summary>
    internal sealed class FullTextToolTip
    {
        private readonly Control _owner;
        private ToolTip _toolTip;
        private string _text = "";

        public FullTextToolTip(Control owner)
        {
            _owner = owner;
        }

        /// <summary>The text the tooltip shows now; empty while there is none.</summary>
        public string Text
        {
            get { return _text; }
        }

        /// <summary>Sets the text to show on hover (empty: none). Nothing happens while it is the one that is set.</summary>
        public void Set(string text)
        {
            text = text ?? "";

            if (text == _text)
                return;

            _text = text;

            if (_toolTip == null && text.Length == 0)
                return;

            if (_toolTip == null)
                _toolTip = UIToolTipFactory.CreateHoverToolTip(_owner);

            _toolTip.SetToolTip(_owner, text);
        }

        public void Dispose()
        {
            if (_toolTip != null)
            {
                _toolTip.Dispose();
                _toolTip = null;
            }

            _text = "";
        }
    }
}
