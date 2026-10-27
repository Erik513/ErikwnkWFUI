using System;
using System.Windows.Forms;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A text box for a password: what is typed is hidden (the system's dots), and an eye in the box, at its right end, shows it while it
    /// is switched on (a click on the eye, or the keys Alt+F8, show the password, the next one hides it again). Make one with
    /// <c>UIStyles.TextBoxes.CreatePassword</c>, which gives the standard type; the class itself is not public.
    /// </summary>
    internal sealed class PasswordTextBox : ThemedTextBox
    {
        public PasswordTextBox()
        {
            UseSystemPasswordChar = true;
            InnerGlyph = "\U0001F441";
            ShowEyeState();
            UIStrings.LanguageChanged += OnLanguageChanged;
        }

        /// <summary>Whether the password is shown in plain text (false by default).</summary>
        public bool IsPasswordShown
        {
            get { return !UseSystemPasswordChar; }
            set
            {
                UseSystemPasswordChar = !value;
                ShowEyeState();
            }
        }

        // The keyboard shows and hides the password too (the eye is painted by the box and is not seen by a screen reader): Alt+F8.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Alt | Keys.F8) && Enabled)
            {
                IsPasswordShown = !IsPasswordShown;

                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                UIStrings.LanguageChanged -= OnLanguageChanged;

            base.Dispose(disposing);
        }
    }
}
