using System;
using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A text box that is locked (read-only) until the user unlocks it: a pencil in the box, at its right end, starts the editing - the box
    /// is unlocked, has the focus and has its text selected, and the pencil becomes a check mark - and the check mark or the Enter key
    /// accepts what was typed and locks the box again; the Escape key gives the old text back. The F2 key and the Enter key on a locked
    /// box start the editing too, so that it can be done with the keyboard alone. Make one with
    /// <c>UIStyles.TextBoxes.CreateLockable</c>.
    /// </summary>
    public class LockableTextBox : ThemedTextBox
    {
        private bool _locked = true;
        private string _textBeforeEdit = "";

        public LockableTextBox()
        {
            ReadOnly = true;
            ShowLockState();
            UIStrings.LanguageChanged += OnLanguageChanged;
        }

        /// <summary>Raised when the box has been locked or unlocked (by the user or by code).</summary>
        public event EventHandler LockedChanged;

        /// <summary>Raised when the user has accepted the text with the check mark or the Enter key; the box is locked again then.</summary>
        public event EventHandler EditAccepted;

        /// <summary>Raised when the user has given up the editing with the Escape key; the old text is back and the box is locked again.</summary>
        public event EventHandler EditCancelled;

        /// <summary>
        /// Whether the box is locked (true by default): read-only, with a pencil. Setting it from code locks or unlocks the box and
        /// raises <see cref="LockedChanged"/>, and nothing else: the text stays as it is, and the box does not take the focus (see
        /// <see cref="BeginEdit"/>, <see cref="AcceptEdit"/> and <see cref="CancelEdit"/> for what the user's buttons do).
        /// </summary>
        [DefaultValue(true)]
        public bool IsLocked
        {
            get { return _locked; }
            set
            {
                if (value == _locked)
                    return;

                if (!value)
                    _textBeforeEdit = Text;

                SetLocked(value);
            }
        }

        /// <summary>What the pencil does: unlocks the box, gives it the focus and selects its text.</summary>
        public void BeginEdit()
        {
            if (!_locked || !Enabled)
                return;

            _textBeforeEdit = Text;
            SetLocked(false);
            Focus();
            SelectAll();
        }

        /// <summary>What the check mark does: locks the box with the text that was typed and raises <see cref="EditAccepted"/>.</summary>
        public void AcceptEdit()
        {
            if (_locked)
                return;

            SetLocked(true);
            SelectionLength = 0;
            EditAccepted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>What the Escape key does: gives the old text back, locks the box and raises <see cref="EditCancelled"/>.</summary>
        public void CancelEdit()
        {
            if (_locked)
                return;

            Text = _textBeforeEdit;
            SetLocked(true);
            SelectionLength = 0;
            EditCancelled?.Invoke(this, EventArgs.Empty);
        }

        private void SetLocked(bool locked)
        {
            _locked = locked;
            ReadOnly = locked;
            ShowLockState();
            LockedChanged?.Invoke(this, EventArgs.Empty);
        }

        // The pencil while it is locked, the check mark while the text is being edited.
        private void ShowLockState()
        {
            InnerGlyph = _locked ? "✎" : "✔";
            InnerActive = !_locked;
            InnerToolTip = UIStrings.Get(_locked ? "TextBox.Edit" : "TextBox.Accept");
        }

        protected override void OnInnerClick()
        {
            base.OnInnerClick();

            if (_locked)
                BeginEdit();
            else
                AcceptEdit();
        }

        // The keys: F2 or Enter start the editing of a locked box; Enter accepts and Escape cancels while it is being edited (a box of
        // several lines keeps Enter for a new line: Ctrl+Enter accepts there).
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_locked)
            {
                if (keyData == Keys.F2 || (keyData == Keys.Enter && Enabled))
                {
                    BeginEdit();

                    return true;
                }
            }
            else
            {
                if (keyData == Keys.Escape)
                {
                    CancelEdit();

                    return true;
                }

                if ((keyData == Keys.Enter && !Multiline) || keyData == (Keys.Enter | Keys.Control))
                {
                    AcceptEdit();

                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ShowLockState();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                UIStrings.LanguageChanged -= OnLanguageChanged;

            base.Dispose(disposing);
        }
    }
}
