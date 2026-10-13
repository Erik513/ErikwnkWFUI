using System;

namespace ErikwnkWFUI.Controls
{
    // The prefix and the suffix of the number: shown in the field, taken off again before the number is read.
    public partial class NumericUpDown
    {
        // Reading the number back with a prefix and a suffix (see ValidateEditText).
        private bool _validating;
        private string _prefix = "";
        private string _suffix = "";

        /// <summary>Text shown before the number in the field (for example "$ "); the <see cref="System.Windows.Forms.NumericUpDown.Value"/> stays the plain number. Empty by default.</summary>
        public string Prefix
        {
            get { return _prefix; }
            set
            {
                _prefix = value ?? "";
                UpdateEditText();
            }
        }

        /// <summary>Text shown after the number in the field (for example " %" or " px"); the <see cref="System.Windows.Forms.NumericUpDown.Value"/> stays the plain number. Empty by default.</summary>
        public string Suffix
        {
            get { return _suffix; }
            set
            {
                _suffix = value ?? "";
                UpdateEditText();
            }
        }

        protected override void UpdateEditText()
        {
            base.UpdateEditText();

            if (_prefix.Length == 0 && _suffix.Length == 0)
                return;

            // Not a change by the user (that would make the standard control read the text as an edit).
            ChangingText = true;

            try
            {
                Text = _prefix + Text + _suffix;
            }
            finally
            {
                ChangingText = false;
            }
        }

        // What the user typed may still carry the prefix and the suffix (or have lost a space of them): they are taken
        // off before the number is read (the standard control reads it when the field is left and before an arrow
        // steps), and put back afterwards.
        protected override void ValidateEditText()
        {
            // Putting the bare number into the field validates again (the standard control does that for a text set by
            // code): the inner round reads it, the outer one must not take off a second suffix.
            if (_validating)
            {
                base.ValidateEditText();
                return;
            }

            _validating = true;

            try
            {
                TakeOffAffixes();
                base.ValidateEditText();
                PutOnAffixes();
            }
            finally
            {
                _validating = false;
            }
        }

        public override void UpButton()
        {
            TakeOffAffixes();
            base.UpButton();
            PutOnAffixes();
        }

        public override void DownButton()
        {
            TakeOffAffixes();
            base.DownButton();
            PutOnAffixes();
        }

        private void TakeOffAffixes()
        {
            if (_prefix.Length == 0 && _suffix.Length == 0)
                return;

            string bare = RemoveAffixes(Text);

            if (bare != Text)
                Text = bare;
        }

        private void PutOnAffixes()
        {
            if (_prefix.Length > 0 || _suffix.Length > 0)
                UpdateEditText();
        }

        private string RemoveAffixes(string text)
        {
            text = text.Trim();
            text = RemoveAffix(text, _prefix, atStart: true);

            return RemoveAffix(text, _suffix, atStart: false);
        }

        private static string RemoveAffix(string text, string affix, bool atStart)
        {
            if (affix.Length == 0)
                return text;

            foreach (string candidate in new[] { affix, affix.Trim() })
            {
                if (candidate.Length == 0)
                    continue;

                if (atStart && text.StartsWith(candidate, StringComparison.Ordinal))
                    return text.Substring(candidate.Length).TrimStart();

                if (!atStart && text.EndsWith(candidate, StringComparison.Ordinal))
                    return text.Substring(0, text.Length - candidate.Length).TrimEnd();
            }

            return text;
        }
    }
}
