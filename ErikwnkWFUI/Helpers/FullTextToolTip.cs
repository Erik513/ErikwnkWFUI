using System;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// The tooltip with the whole text of a control that cannot show it all (a text that is cut or moves, on a label or a progress bar):
    /// the control tells it the text to show, or an empty one when everything is visible, and it makes the standard hover tooltip of the
    /// library (the one the other controls use) show it or not. The tooltip window is only made when there is a text to show.
    /// A text that is wider than <see cref="ToolTipWidth.MaxWidthAt96Dpi"/> pixels is wrapped into lines of that width (a standard tooltip would show
    /// one line as wide as the screen); a line break in the text is kept.
    /// </summary>
    internal sealed class FullTextToolTip
    {
        private readonly Control _owner;
        private ToolTip _toolTip;
        private bool _enabled = true;
        private string _text = "";

        public FullTextToolTip(Control owner)
        {
            _owner = owner;
        }

        /// <summary>Whether the tooltip is used at all (on by default); off, it shows nothing whatever the control tells it.</summary>
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value == _enabled)
                    return;

                _enabled = value;

                if (!value)
                    Set("");
            }
        }

        /// <summary>
        /// Tells the tooltip what the control shows: the tooltip has the whole <paramref name="text"/> when the control could not
        /// show it whole (<paramref name="cut"/>: it is cut or it moves) and is <see cref="Enabled"/>, and none otherwise.
        /// </summary>
        public void Update(string text, bool cut)
        {
            Set(_enabled && cut ? text : "");
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
            {
                _toolTip = UIToolTipFactory.CreateHoverToolTip(_owner);
            }

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
