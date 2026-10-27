using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // What the switch does with the mouse, the keyboard and the focus.
    public partial class ToggleSwitch
    {
        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;

            Invalidate();

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;

            Invalidate();

            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                bool wasPressed = _isPressed;
                _isPressed = false;

                // As a standard check box: the press has to end over the switch (a press that is dragged away and let go elsewhere
                // does nothing), and a press that has been cancelled in the meantime (the capture was lost) does not count.
                if (wasPressed && ClientRectangle.Contains(e.Location))
                    ToggleByUser();

                Invalidate();
            }

            base.OnMouseUp(e);
        }

        // The window that takes the mouse away (a dialog that opens on the press) means no release will come: nothing stays pressed.
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);

            if (_isPressed && !Capture)
            {
                _isPressed = false;
                Invalidate();
            }
        }

        // The space bar works as on a standard check box: the switch looks pressed while it is down and toggles when it is let go.
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Space && !e.Alt && !e.Control && !_isPressed && Enabled)
            {
                _isPressed = true;
                Invalidate();
                e.Handled = true;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyCode != Keys.Space)
                return;

            bool wasPressed = _isPressed;
            _isPressed = false;
            Invalidate();

            if (wasPressed && Enabled)
            {
                ToggleByUser();
                e.Handled = true;
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        // A press with the space bar that is interrupted by the focus going elsewhere is cancelled (its release would never come).
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            _isPressed = false;
            Invalidate();
        }

        // A disabled control gets no MouseLeave: whether the mouse is over it is looked at again, and nothing stays pressed.
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            _isPressed = false;
            _isHovered = Enabled && IsHandleCreated && ClientRectangle.Contains(PointToClient(Cursor.Position));
            Invalidate();
        }

        // A control that is hidden under the mouse gets no MouseLeave either.
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible)
            {
                _isHovered = false;
                _isPressed = false;
            }
        }
    }
}
