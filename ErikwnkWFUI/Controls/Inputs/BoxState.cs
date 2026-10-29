using System;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// What the check box and the radio button know of the mouse and the keyboard besides what the standard control tells: whether the mouse is over
    /// the box (<see cref="IsHot"/>) and whether it is pressed (<see cref="IsPressed"/>, by the left button or the space bar). They cannot share a
    /// base class - each is another standard control - so this watches the events of the box and repaints it when the state changes.
    /// </summary>
    internal sealed class BoxState
    {
        private readonly ButtonBase _box;

        public BoxState(ButtonBase box)
        {
            _box = box;

            box.MouseEnter += (sender, e) => Set(true, IsPressed);
            box.MouseLeave += (sender, e) => Set(false, false);

            box.MouseDown += (sender, e) => Set(IsHot, IsPressed || e.Button == MouseButtons.Left);
            box.MouseUp += (sender, e) => Set(IsHot, false);

            box.KeyDown += (sender, e) => Set(IsHot, IsPressed || e.KeyCode == Keys.Space);
            box.KeyUp += (sender, e) => Set(IsHot, e.KeyCode == Keys.Space ? false : IsPressed);

            box.GotFocus += (sender, e) => box.Invalidate();
            box.LostFocus += (sender, e) => Set(IsHot, false);

            // The mouse can be taken away without a MouseUp (another window, a dialog opened while pressing): then nothing is pressed any more.
            box.MouseCaptureChanged += (sender, e) => Set(IsHot, false);

            // Hidden, a box gets no MouseLeave: shown again it must not still look hovered.
            box.VisibleChanged += (sender, e) =>
            {
                if (!box.Visible)
                {
                    IsHot = false;
                    IsPressed = false;
                }
            };

            // A disabled control gets no MouseUp: a press that was still going (a handler that disables the box) must not outlive it. Nor does it
            // get a MouseLeave: whether the mouse is over the box is looked at again when it is enabled, so it does not look hovered with the mouse
            // somewhere else.
            box.EnabledChanged += (sender, e) =>
                Set(box.Enabled && box.IsHandleCreated && box.ClientRectangle.Contains(box.PointToClient(Cursor.Position)), false);
        }

        /// <summary>Whether the mouse is over the box.</summary>
        public bool IsHot { get; private set; }

        /// <summary>Whether the box is pressed (the left button or the space bar is down).</summary>
        public bool IsPressed { get; private set; }

        /// <summary>Whether the box shows hovered or pressed.</summary>
        public bool IsActive
        {
            get { return IsHot || IsPressed; }
        }

        private void Set(bool isHot, bool isPressed)
        {
            IsHot = isHot;
            IsPressed = isPressed;
            _box.Invalidate();
        }
    }
}
