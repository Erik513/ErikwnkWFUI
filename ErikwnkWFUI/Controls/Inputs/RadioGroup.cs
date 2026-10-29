using System;
using System.Collections.Generic;
using System.Linq;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// One choice among several radio buttons that do not have to share a parent: the standard grouping works only for buttons in
    /// the same container, which is awkward in a table layout or when a choice is spread over several panels. Put the buttons into a
    /// group; selecting one clears the others, and <see cref="SelectionChanged"/> tells the new choice once.
    /// </summary>
    /// <remarks>
    /// The group does not draw anything and is not a control. It listens to the buttons until it is disposed or a button is removed
    /// or disposed. <see cref="SelectionChanged"/> is raised when a button of the group becomes
    /// checked and when <see cref="SelectedIndex"/> is set to -1; clearing a button by setting its own <c>Checked</c> to false
    /// does not raise it (<see cref="SelectedButton"/> is always read from the buttons, so it is right either way).
    /// </remarks>
    public class RadioGroup : IDisposable
    {
        private readonly List<System.Windows.Forms.RadioButton> _buttons = new List<System.Windows.Forms.RadioButton>();
        private bool _isChangingSelection;
        private bool _isDisposed;

        /// <summary>Makes a group of these buttons (none, one or several of them may be checked at the start; of several the last one added that is checked stays).</summary>
        public RadioGroup(params System.Windows.Forms.RadioButton[] buttons)
        {
            if (buttons == null)
                return;

            foreach (System.Windows.Forms.RadioButton button in buttons)
                Add(button);
        }

        /// <summary>Raised when the choice changes: a button of the group was checked, or the selection was cleared with <see cref="SelectedIndex"/>.</summary>
        public event EventHandler SelectionChanged;

        /// <summary>The buttons of the group, in the order they were added.</summary>
        public IReadOnlyList<System.Windows.Forms.RadioButton> Buttons
        {
            get { return _buttons.AsReadOnly(); }
        }

        /// <summary>The checked button, or null when there is none.</summary>
        public System.Windows.Forms.RadioButton SelectedButton
        {
            get { return _buttons.FirstOrDefault(button => button.Checked); }
        }

        /// <summary>The index of the checked button in <see cref="Buttons"/>, or -1 for none. Setting -1 clears the choice; an index selects that button.</summary>
        public int SelectedIndex
        {
            get { return _buttons.FindIndex(button => button.Checked); }
            set
            {
                if (value < -1 || value >= _buttons.Count)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (value == SelectedIndex)
                    return;

                if (value >= 0)
                {
                    _buttons[value].Checked = true;
                    return;
                }

                ClearOthers(null);
                OnSelectionChanged();
            }
        }

        /// <summary>Adds a button to the group. If it is checked, the others are cleared. A button that is in the group already is not added again.</summary>
        public void Add(System.Windows.Forms.RadioButton button)
        {
            if (button == null)
                throw new ArgumentNullException(nameof(button));

            ThrowIfDisposed();

            if (_buttons.Contains(button))
                return;

            _buttons.Add(button);
            button.CheckedChanged += ButtonCheckedChanged;
            button.Disposed += ButtonDisposed;

            if (button.Checked)
                ClearOthers(button);
        }

        /// <summary>Takes a button out of the group (it is left as it is).</summary>
        public bool Remove(System.Windows.Forms.RadioButton button)
        {
            if (button == null || !_buttons.Remove(button))
                return false;

            Detach(button);
            return true;
        }

        private void Detach(System.Windows.Forms.RadioButton button)
        {
            button.CheckedChanged -= ButtonCheckedChanged;
            button.Disposed -= ButtonDisposed;
        }

        private void ButtonDisposed(object sender, EventArgs e)
        {
            Remove((System.Windows.Forms.RadioButton)sender);
        }

        private void ButtonCheckedChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.RadioButton button = (System.Windows.Forms.RadioButton)sender;

            if (_isChangingSelection || !button.Checked)
                return;

            ClearOthers(button);
            OnSelectionChanged();
        }

        // Clears every button but the one to keep; the changes made here must not start the same again.
        private void ClearOthers(System.Windows.Forms.RadioButton keep)
        {
            _isChangingSelection = true;

            try
            {
                foreach (System.Windows.Forms.RadioButton other in _buttons.ToArray())
                {
                    if (other != keep && other.Checked)
                        other.Checked = false;
                }
            }
            finally
            {
                _isChangingSelection = false;
            }
        }

        private void OnSelectionChanged()
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(RadioGroup));
        }

        /// <summary>Lets go of the buttons (they are not disposed and keep their state).</summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            foreach (System.Windows.Forms.RadioButton button in _buttons)
                Detach(button);

            _buttons.Clear();
        }
    }
}
