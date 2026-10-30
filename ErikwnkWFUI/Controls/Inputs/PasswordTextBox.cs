using System;
using System.Windows.Forms;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A text box for a password: what is typed is hidden (dots), and an eye in the box, at its right end, shows it while it
    /// is switched on (a click on the eye, or the keys Alt+F8, show the password, the next one hides it again). The hiding is the
    /// <c>PasswordChar</c> of the box, which the system changes with a message: the window stays the one it is (with <c>UseSystemPasswordChar</c>
    /// every switch makes a new window, and the text, the caret and the focus have to be carried over to it). Make one with
    /// <c>UIStyles.TextBoxes.CreatePassword</c>, which gives the standard type; the class itself is not public.
    /// </summary>
    internal sealed class PasswordTextBox : TextField
    {
        // The dot that hides a letter (the one that the system uses for its own password boxes).
        private const char Dot = '\u25CF';

        private bool _shown;

        /// <summary>Makes a box that hides what is typed, with the eye in it.</summary>
        public PasswordTextBox()
        {
            PasswordChar = Dot;
            InnerGlyph = "\U0001F441";
            ShowEyeState();
            UIStrings.LanguageChanged += OnLanguageChanged;
        }

        /// <summary>Whether the password is shown in plain text (false by default).</summary>
        public bool IsPasswordShown
        {
            get { return _shown; }
            set
            {
                _shown = value;
                PasswordChar = value ? '\0' : Dot;
                ShowEyeState();
            }
        }

        // The keyboard shows and hides the password too (the eye is painted by the box and is not seen by a screen reader): Alt+F8.
        /// <inheritdoc/>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Alt | Keys.F8) && Enabled)
            {
                IsPasswordShown = !IsPasswordShown;

                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <inheritdoc/>
        protected override void OnInnerClick()
        {
            base.OnInnerClick();
            IsPasswordShown = !IsPasswordShown;
        }

        private void ShowEyeState()
        {
            InnerActive = IsPasswordShown;
            InnerToolTip = UIStrings.Get(IsPasswordShown ? "TextBox.HidePassword" : "TextBox.ShowPassword");
            RefreshToolTip();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ShowEyeState();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                UIStrings.LanguageChanged -= OnLanguageChanged;

            base.Dispose(disposing);
        }
    }
}
