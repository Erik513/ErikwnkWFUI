using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // The tooltip with the whole text when it is wider than the box, and the tooltip of the button in the box.
    public partial class ThemedTextBox
    {
        private bool _toolTipWhenCut = true;

        private ToolTip _toolTip;

        /// <summary>
        /// Whether the whole text is shown as a tooltip while the mouse is over the box and the text is wider than the box (true by
        /// default). Not while the box hides what is typed (a password that is not shown), and not for a multi-line box, which shows its text.
        /// </summary>
        [DefaultValue(true)]
        public bool ToolTipWhenCut
        {
            get { return _toolTipWhenCut; }
            set
            {
                _toolTipWhenCut = value;
                UpdateToolTip(false);
            }
        }

        // A box that hides what is typed shows nothing of it, not even in a tooltip.
        private bool HidesText
        {
            get { return UseSystemPasswordChar || PasswordChar != '\0'; }
        }

        // The text is wider than the place that the system gives it.
        private bool IsTextCut()
        {
            // (The handle first: reading the PasswordChar of a box that has none makes one.)
            if (!IsHandleCreated || !_toolTipWhenCut || Multiline || TextLength == 0 || HidesText)
                return false;

            Rectangle area = GetTextArea();
            int width = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;

            return width > area.Width;
        }

        // What the tooltip of the box says at the moment: the text of the button in the box while the mouse is over it, else the whole text
        // when it is cut, else nothing.
        private void UpdateToolTip(bool overInnerButton)
        {
            if (_toolTip == null)
                return;

            string text = overInnerButton && _innerToolTip.Length > 0 ? _innerToolTip : IsTextCut() ? Text : string.Empty;

            if (_toolTip.GetToolTip(this) != text)
                _toolTip.SetToolTip(this, text);
        }

        // For a box whose way of hiding the text has changed (the password that is shown or hidden).
        internal void RefreshToolTip()
        {
            UpdateToolTip(_innerHot);
        }
    }
}
