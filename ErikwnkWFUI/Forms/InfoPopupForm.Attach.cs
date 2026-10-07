using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Forms
{
    // AttachTo / Detach: the popup as the tooltip of controls (see the class remarks).
    public partial class InfoPopupForm
    {
        // ---- attaching to controls ----

        // What the popup knows about a control it is attached to.
        private sealed class Binding
        {
            public Control Control;
            public Func<InfoPopupSection[]> Content;
            public Control Parent;
            public bool Inside;
            public bool Shown;
            public MouseEventHandler OnMove;
            public EventHandler OnLeave;
            public MouseEventHandler OnDown;
            public KeyEventHandler OnKey;
            public EventHandler OnHidden;
            public EventHandler OnDisposed;
            public EventHandler OnParentChanged;
            public MouseEventHandler OnParentMove;
            public EventHandler OnParentLeave;
        }

        private readonly Dictionary<Control, Binding> _bindings = new Dictionary<Control, Binding>();

        /// <summary>
        /// Shows this popup as the tooltip of <paramref name="control"/>: after the
        /// <see cref="Delay"/> once the mouse rests on it, at the mouse, once for each time the
        /// mouse enters. It goes away when the mouse leaves, on a click or a key press, and when
        /// the control is hidden or disposed (which also detaches it). One popup can serve any
        /// number of controls, each with its own content. A disabled control gets it too.
        /// </summary>
        public void AttachTo(Control control, string text)
        {
            AttachTo(control, () => new[] { new InfoPopupSection(null, text) });
        }

        /// <summary>Attaches a header with its text; see <see cref="AttachTo(Control, string)"/>.</summary>
        public void AttachTo(Control control, string header, string text)
        {
            AttachTo(control, () => new[] { new InfoPopupSection(header, text) });
        }

        /// <summary>Attaches the blocks; see <see cref="AttachTo(Control, string)"/>.</summary>
        public void AttachTo(Control control, params InfoPopupSection[] sections)
        {
            AttachTo(control, () => sections);
        }

        /// <summary>
        /// Attaches content that is worked out each time the popup is about to show (a value
        /// that changes, a text for the current state); see <see cref="AttachTo(Control, string)"/>.
        /// </summary>
        public void AttachTo(Control control, Func<InfoPopupSection[]> content)
        {
            if (control == null || content == null || IsDisposed)
                return;

            Detach(control);

            Binding binding = new Binding { Control = control, Content = content };

            binding.OnMove = (sender, e) => StartHover(binding, Control.MousePosition);
            binding.OnLeave = (sender, e) => StopHover(binding);
            binding.OnDown = (sender, e) => StopHover(binding);
            binding.OnKey = (sender, e) => StopHover(binding);
            binding.OnHidden = (sender, e) =>
            {
                if (!control.Visible)
                    StopHover(binding);
            };
            binding.OnDisposed = (sender, e) => Detach(control);
            binding.OnParentChanged = (sender, e) => HookParent(binding);

            // A disabled control gets no mouse events of its own: its parent sees the mouse and
            // the control is found by where it is.
            binding.OnParentMove = (sender, e) =>
            {
                if (!control.Enabled && control.Bounds.Contains(e.Location))
                    StartHover(binding, Control.MousePosition);
                else if (binding.Inside && !control.Enabled)
                    StopHover(binding);
            };
            binding.OnParentLeave = (sender, e) =>
            {
                if (binding.Inside && !control.Enabled)
                    StopHover(binding);
            };

            control.MouseMove += binding.OnMove;
            control.MouseLeave += binding.OnLeave;
            control.MouseDown += binding.OnDown;
            control.KeyDown += binding.OnKey;
            control.VisibleChanged += binding.OnHidden;
            control.Disposed += binding.OnDisposed;
            control.ParentChanged += binding.OnParentChanged;
            HookParent(binding);

            _bindings[control] = binding;
        }

        /// <summary>Takes the popup off <paramref name="control"/> again; a popup that is up for it goes away.</summary>
        public void Detach(Control control)
        {
            Binding binding;

            if (control == null || !_bindings.TryGetValue(control, out binding))
                return;

            _bindings.Remove(control);
            StopHover(binding);

            control.MouseMove -= binding.OnMove;
            control.MouseLeave -= binding.OnLeave;
            control.MouseDown -= binding.OnDown;
            control.KeyDown -= binding.OnKey;
            control.VisibleChanged -= binding.OnHidden;
            control.Disposed -= binding.OnDisposed;
            control.ParentChanged -= binding.OnParentChanged;
            UnhookParent(binding);
        }

        private void HookParent(Binding binding)
        {
            UnhookParent(binding);

            binding.Parent = binding.Control.Parent;

            if (binding.Parent != null)
            {
                binding.Parent.MouseMove += binding.OnParentMove;
                binding.Parent.MouseLeave += binding.OnParentLeave;
            }
        }

        private void UnhookParent(Binding binding)
        {
            if (binding.Parent == null)
                return;

            binding.Parent.MouseMove -= binding.OnParentMove;
            binding.Parent.MouseLeave -= binding.OnParentLeave;
            binding.Parent = null;
        }

        // The mouse is on the control: once for each time it enters, the popup is scheduled, and
        // not again while it is up or after it has gone away by itself.
        private void StartHover(Binding binding, Point mouseScreenPosition)
        {
            binding.Inside = true;

            if (binding.Shown)
                return;

            ShowAtMouseDelayed(binding.Control, mouseScreenPosition, binding.Content);
        }

        private void StopHover(Binding binding)
        {
            binding.Inside = false;
            binding.Shown = false;

            // Only what this control asked for: another control's popup is not touched.
            if (_pendingAnchor == binding.Control)
                CancelPendingShow();

            if (Visible && _lastAnchor == binding.Control)
                Hide();
        }

        private void NoteShownFor(Control anchor)
        {
            Binding binding;

            if (_bindings.TryGetValue(anchor, out binding))
                binding.Shown = true;
        }
    }
}
