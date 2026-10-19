using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    // The mouse, the keys and the wheel: dragging the thumb, and what ends a drag.
    public partial class TrackBar
    {
        private int _grabOffset;
        private bool _dragging;
        private bool _hover;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Only the left button, and not while a drag is on already (a second press with the mouse held starts no second drag).
            if (e.Button == MouseButtons.Left && Enabled && !_dragging)
            {
                Focus();
                _dragging = true;

                // A press on the thumb takes hold of it where it was pressed (the thumb does not jump to the mouse); a press on the
                // track puts the thumb under the mouse and drags it from its middle.
                _grabOffset = Math.Abs(e.X - ThumbCenterX) <= ThumbRadius + Dpi.Scale(this, 2, DpiOverride) ? e.X - ThumbCenterX : 0;

                if (DragStarted != null)
                {
                    DragStarted(this, EventArgs.Empty);
                }
                SetValueFromUser(ValueFromX(e.X - _grabOffset));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                SetValueFromUser(ValueFromX(e.X - _grabOffset));
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            EndDrag();
        }

        // The drag also ends where no MouseUp comes: the mouse is taken from the control (a dialog, another window, Alt+Tab), the
        // control is disabled or its window is gone. Otherwise it would go on following the mouse with no button pressed.
        private void EndDrag()
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;
            Invalidate();

            if (DragEnded != null)
            {
                DragEnded(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture)
            {
                EndDrag();
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            // A disabled control gets no MouseLeave: whether the mouse is over it is looked at again, or the thumb would stay big.
            _hover = Enabled && IsHandleCreated && ClientRectangle.Contains(PointToClient(Cursor.Position));

            if (!Enabled)
            {
                EndDrag();
                _valuePopup.Hide();
            }

            Invalidate();
        }

        // A control that is hidden under the mouse gets no MouseLeave either.
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible)
            {
                _hover = false;
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            // (No DragEnded: whoever listens may be on its way out as well.)
            _dragging = false;
            _hover = false;
            base.OnHandleDestroyed(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
            _valuePopup.MouseEnter(IsDragging);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
            _valuePopup.MouseLeave(IsDragging);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Up || keyData == Keys.Down
                || keyData == Keys.PageUp || keyData == Keys.PageDown
                || keyData == Keys.Home || keyData == Keys.End)
            {
                return true;
            }
            return base.IsInputKey(keyData);
        }

        // As on the original: left and down make it less, right and up more, by SmallChange; Page Down and Up by LargeChange;
        // Home and End go to the ends. A disabled slider does not move.
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!Enabled)
            {
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Down:
                    SetValueFromUser(_value - SmallChange);
                    e.Handled = true;
                    break;
                case Keys.Right:
                case Keys.Up:
                    SetValueFromUser(_value + SmallChange);
                    e.Handled = true;
                    break;
                case Keys.PageDown:
                    SetValueFromUser(_value - LargeChange);
                    e.Handled = true;
                    break;
                case Keys.PageUp:
                    SetValueFromUser(_value + LargeChange);
                    e.Handled = true;
                    break;
                case Keys.Home:
                    SetValueFromUser(_minimum);
                    e.Handled = true;
                    break;
                case Keys.End:
                    SetValueFromUser(_maximum);
                    e.Handled = true;
                    break;
            }
        }

        // A step of the wheel moves the value by SmallChange (up = more) while the slider has the focus, as on the original; the
        // parent does not scroll with it then.
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            HandledMouseEventArgs handled = e as HandledMouseEventArgs;

            if (handled != null)
            {
                handled.Handled = true;
            }

            if (!Enabled || _dragging)
            {
                return;
            }

            StepByWheel(e.Delta);
        }

        // A step of the mouse wheel (also for a control that passes the wheel on to this one, like the volume control from its button).
        internal void StepByWheel(int delta)
        {
            if (!Enabled || _dragging)
            {
                return;
            }

            SetValueFromUser(_value + delta / (double)SystemInformation.MouseWheelScrollDelta * SmallChange);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }
    }
}
