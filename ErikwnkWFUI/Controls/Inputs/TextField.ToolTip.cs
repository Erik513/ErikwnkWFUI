using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // The tooltip with the whole text when it is wider than the box, and the tooltip of the button in the box.
    public partial class TextField
    {
        private bool _toolTipWhenCut = true;

        private ToolTip _toolTip;

        // The tooltip of a cut text: its text is the text of the box, always there, and the question whether it is shown is asked when it is
        // about to pop up (Popup). A tooltip that is only given its text when the mouse arrives has missed the mouse by then: the native one
        // looks for a tool under the mouse when the mouse moves, and does not find the one that was made a moment later.
        private ToolTip _cutToolTip;

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

        // Whether the text is cut is looked at again when the mouse comes: the width of the box and the margins may have changed since the
        // tooltip was set (the box is placed by a layout, after its text was given).
        /// <inheritdoc/>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            UpdateToolTip(_innerHot);
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

        // What the tooltips of the box say at the moment: the text of the button in the box while the mouse is over it (the first tooltip), else
        // the whole text when it is cut (the second one), else nothing.
        private void UpdateToolTip(bool overInnerButton)
        {
            if (_toolTip == null)
                return;

            string inner = overInnerButton && _innerToolTip.Length > 0 ? _innerToolTip : string.Empty;

            if (_toolTip.GetToolTip(this) != inner)
                _toolTip.SetToolTip(this, inner);

            // (The text of the box, whether it is cut or not at the moment: Popup decides. Not for a text that is never shown.)
            string whole = _toolTipWhenCut && !Multiline && !HidesText ? Text : string.Empty;

            if (_cutToolTip != null && _cutToolTip.GetToolTip(this) != whole)
                _cutToolTip.SetToolTip(this, whole);
        }

        // What the tooltips say now, as the mouse would see them (for the tests): the button in the box, or the cut text, or nothing.
        internal string ToolTipTextNow
        {
            get { return _innerHot && _innerToolTip.Length > 0 ? _innerToolTip : IsTextCut() ? Text : string.Empty; }
        }

        // The tooltip of the cut text pops up only when the text is cut and the mouse is not over the button in the box.
        private void OnCutToolTipPopup(object sender, PopupEventArgs e)
        {
            if (_innerHot || !IsTextCut())
                e.Cancel = true;
        }

        internal void RaiseCutToolTipPopup(PopupEventArgs e)
        {
            OnCutToolTipPopup(_cutToolTip, e);
        }

        // For a box whose way of hiding the text has changed (the password that is shown or hidden).
        internal void RefreshToolTip()
        {
            UpdateToolTip(_innerHot);
        }
    }
}
