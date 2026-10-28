using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using Button = System.Windows.Forms.Button;
using Label = System.Windows.Forms.Label;
using TextBox = System.Windows.Forms.TextBox;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// The themed replacement for the input box of Visual Basic: a small dialog with a prompt, one text box (or a password box) and the buttons OK and
    /// Cancel. Make one with <c>UIStyles.Forms.CreateInputStandard</c> / <c>CreateInputPrimary</c> and show it with <c>ShowDialog</c>, or just call
    /// <see cref="InputBox.Show"/>. After <c>DialogResult.OK</c> the text is in <see cref="Value"/>. Enter confirms, Escape cancels.
    /// Under the prompt there can be a gray <see cref="Hint"/>, and a red <see cref="ErrorText"/> over the box (set it for a wrong
    /// entry, or let <see cref="Validator"/> do it). The window has a fixed width and is as high as its texts need, but never higher than the
    /// screen (a prompt that is too long is cut at its end). The texts of the buttons and the default title follow the language of the library.
    /// Dispose it after <c>ShowDialog</c> (<see cref="InputBox.Show"/> does).
    /// </summary>
    public class InputBoxForm : AppForm
    {
        // The sizes are given for 96 dpi and scaled to the screen.
        private const int Margin96 = 24;
        private const int GapBeforeBox96 = 12;
        private const int GapBetweenTexts96 = 6;
        private const int GapBeforeButtons96 = 20;
        private const int TextBoxHeight96 = 30;
        private const int ButtonWidth96 = 100;
        private const int ButtonHeight96 = 32;
        private const int ButtonGap96 = 8;
        private const int ScreenMargin96 = 40;

        private readonly Label _promptLabel;
        private readonly Label _hintLabel;
        private readonly Label _errorLabel;
        private readonly TextBox _textBox;
        private readonly Button _okButton;
        private readonly Button _cancelButton;
        private readonly bool _defaultTitle;
        private readonly FormSizeOption _size;
        private int _dpiOverride;
        private string _okText;
        private string _cancelText;

        /// <summary>The text that was entered, once the dialog was closed with OK (what the user accepted); empty before.</summary>
        public string Value { get; private set; } = "";

        /// <summary>The box the user types into, for what is not covered here (an input filter ...).</summary>
        public TextBox InputTextBox
        {
            get { return _textBox; }
        }

        /// <summary>Whether OK also accepts an empty text. By default it does not: OK does nothing until something is typed.</summary>
        public bool AllowEmpty { get; set; }

        /// <summary>
        /// Whether the spaces at the beginning and the end of the text are cut off: <see cref="Value"/> and the text that <see cref="Validator"/> sees
        /// are without them, and a text of spaces only counts as empty. Off by default (a password may begin with a space).
        /// </summary>
        public bool TrimValue { get; set; }

        /// <summary>The most characters the box takes; 0 (the default) is no limit beyond the one of Windows.</summary>
        public int MaxLength
        {
            // 32767 is the limit that a single-line text box has by itself.
            get { return _textBox.MaxLength == 32767 ? 0 : _textBox.MaxLength; }
            set { _textBox.MaxLength = value <= 0 ? 32767 : value; }
        }

        /// <summary>
        /// Checks the text when OK is clicked: returns null when it is fine, otherwise the message that is shown in red over the box (the
        /// window stays open, the text is selected). Null (the default) accepts every text that is not empty. It is not asked for an empty text
        /// unless <see cref="AllowEmpty"/> is set, and it gets the trimmed text when <see cref="TrimValue"/> is set. An exception in it is not caught.
        /// </summary>
        public Func<string, string> Validator { get; set; }

        /// <summary>The text above the box; the window grows or shrinks to it.</summary>
        public string Prompt
        {
            get { return _promptLabel.Text; }
            set
            {
                _promptLabel.Text = value ?? "";
                LayoutContent();
            }
        }

        /// <summary>A short note in small gray letters under the prompt (what is wanted, an example ...); empty (the default) shows none.</summary>
        public string Hint
        {
            get { return _hintLabel.Text; }
            set
            {
                _hintLabel.Text = value ?? "";
                LayoutContent();
            }
        }

        /// <summary>
        /// A message in red over the box, for an entry that was not right (for instance "wrong password" when the program asks again). It
        /// is taken away when the user types, and <see cref="Validator"/> sets it. Empty (the default) shows none.
        /// </summary>
        public string ErrorText
        {
            get { return _errorLabel.Text; }
            set
            {
                _errorLabel.Text = value ?? "";
                LayoutContent();
            }
        }

        /// <summary>The text of the OK button: "OK", or null for the text of the library.</summary>
        public string OkButtonText
        {
            get { return _okButton.Text; }
            set
            {
                _okText = value;
                _okButton.Text = value ?? UIStrings.Get("InputBox.Ok");
            }
        }

        /// <summary>The text of the Cancel button ("Cancel" or "Abbrechen"), or null for the text of the library.</summary>
        public string CancelButtonText
        {
            get { return _cancelButton.Text; }
            set
            {
                _cancelText = value;
                _cancelButton.Text = value ?? UIStrings.Get("InputBox.Cancel");
            }
        }

        /// <param name="options">The options of the window (a dialog, as <see cref="FormOptions.CreateDialog"/>).</param>
        /// <param name="prompt">The text above the box.</param>
        /// <param name="text">The text the box starts with (selected, so typing replaces it).</param>
        /// <param name="password">Whether the box hides what is typed (with an eye to show it).</param>
        /// <param name="placeholder">A hint in the empty box that has not the focus.</param>
        /// <param name="size">One of the three widths (<see cref="FormSize.Medium"/> by default) or an own <see cref="Size"/>: its width, and its height as the least height (the texts decide when they need more).</param>
        public InputBoxForm(FormOptions options, string prompt, string text = "", bool password = false, string placeholder = "", FormSizeOption? size = null)
            : base(options ?? FormOptions.CreateDialog(UIStrings.Get("InputBox.Title"), ContentAlignment.MiddleLeft))
        {
            _size = size ?? FormSize.Medium;
            _defaultTitle = options == null || options.Title == UIStrings.Get("InputBox.Title");

            ShowInTaskbar = false;

            _promptLabel = CreateWrappingLabel(UIStyles.Labels.CreateStandard(prompt ?? ""));
            _hintLabel = CreateWrappingLabel(UIStyles.Labels.CreateMuted());
            _errorLabel = CreateWrappingLabel(UIStyles.Labels.CreateStandard("", UIStyles.Colors.Sets.Red));

            _textBox = password
                ? UIStyles.TextBoxes.CreatePassword(text ?? "", placeholder ?? "")
                : UIStyles.TextBoxes.CreateStandard(text ?? "", placeholder ?? "");

            // Typing takes the message of the last try away: it was about the old text.
            _textBox.TextChanged += (sender, e) =>
            {
                if (_errorLabel.Text.Length > 0)
                    ErrorText = "";
            };

            _cancelButton = UIStyles.Buttons.CreateStandard(UIStrings.Get("InputBox.Cancel"));
            _cancelButton.DialogResult = DialogResult.Cancel;

            _okButton = UIStyles.Buttons.CreatePrimary(UIStrings.Get("InputBox.Ok"));
            _okButton.Click += (sender, e) => Accept();

            ContentPanel.Controls.AddRange(new Control[] { _promptLabel, _hintLabel, _errorLabel, _textBox, _cancelButton, _okButton });

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
            ActiveControl = _textBox;

            LayoutContent();
            UIStrings.LanguageChanged += OnLanguageChanged;
        }

        // A label that is placed by LayoutContent: it wraps (no ellipsis) and does not follow the size of the window by itself.
        private static Label CreateWrappingLabel(Label label)
        {
            label.AutoEllipsis = false;
            label.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            label.TextAlign = ContentAlignment.TopLeft;

            return label;
        }

        // OK takes the text - unless it is empty and an empty text is not wanted (nothing happens, the box gets the focus), or Validator
        // does not accept it (its message is shown and the text is selected, so typing replaces it).
        private void Accept()
        {
            string text = TrimValue ? _textBox.Text.Trim() : _textBox.Text;

            if (text.Length == 0 && !AllowEmpty)
            {
                _textBox.Focus();

                return;
            }

            string problem = Validator == null ? null : Validator(text);

            if (!string.IsNullOrEmpty(problem))
            {
                ErrorText = problem;
                _textBox.Focus();
                _textBox.SelectAll();

                return;
            }

            Value = text;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _textBox.SelectAll();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            // A text that was set stays as it is; the others follow the language.
            if (_okText == null)
                _okButton.Text = UIStrings.Get("InputBox.Ok");

            if (_cancelText == null)
                _cancelButton.Text = UIStrings.Get("InputBox.Cancel");

            if (_defaultTitle)
                FormTitle = UIStrings.Get("InputBox.Title");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                UIStrings.LanguageChanged -= OnLanguageChanged;

            base.Dispose(disposing);
        }

        // ---- layout

        internal int DpiOverride
        {
            get { return _dpiOverride; }
            set
            {
                _dpiOverride = value;
                LayoutContent();
            }
        }

        private int Scale(int value96)
        {
            return Dpi.Scale(this, value96, _dpiOverride);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            LayoutContent();
        }

        // Prompt, note, message, box and buttons one below the other; the height of the window follows the height of the (wrapped) texts.
        // A text that is empty is not shown and takes no room.
        private void LayoutContent()
        {
            if (_okButton == null)
                return;

            int margin = Scale(Margin96);
            Size wanted = _size.Resolve(FormKind.Input);
            int width = Scale(wanted.Width);

            // The window has an edge around it: the content is as wide as the client area minus that (measured, not assumed).
            ClientSize = new Size(width, ClientSize.Height);
            int inner = ContentPanel.ClientSize.Width - 2 * margin;

            Label[] texts = { _promptLabel, _hintLabel, _errorLabel };
            int y = margin;

            int[] heights = new int[texts.Length];
            int textsHeight = 0;

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].Text.Length == 0)
                    continue;

                heights[i] = TextRenderer.MeasureText(texts[i].Text, texts[i].Font, new Size(inner, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
                textsHeight += heights[i] + Scale(HasTextAfter(texts, i) ? GapBetweenTexts96 : GapBeforeBox96);
            }

            // Never higher than the screen: a prompt that is longer than what fits is cut at its end (the box and the buttons stay).
            int below = Scale(TextBoxHeight96) + Scale(GapBeforeButtons96) + Scale(ButtonHeight96) + margin;
            int outside = ClientSize.Height - ContentPanel.ClientSize.Height;
            int room = Screen.FromControl(this).WorkingArea.Height - Scale(ScreenMargin96) - outside - margin - below;

            if (textsHeight > room)
                heights[0] = Math.Max(0, heights[0] - (textsHeight - room));

            for (int i = 0; i < texts.Length; i++)
            {
                Label label = texts[i];
                bool shown = label.Text.Length > 0;
                label.Visible = shown;

                // (Not label.Visible: that is false for every label as long as the window itself is not shown.)
                if (!shown)
                    continue;

                label.SetBounds(margin, y, inner, heights[i]);
                y += heights[i] + Scale(HasTextAfter(texts, i) ? GapBetweenTexts96 : GapBeforeBox96);
            }

            _textBox.SetBounds(margin, y, inner, Scale(TextBoxHeight96));
            y += Scale(TextBoxHeight96) + Scale(GapBeforeButtons96);

            // An own height that is more than the texts need: the buttons go down to the bottom edge.
            int leastHeight = Scale(wanted.Height) - (ClientSize.Height - ContentPanel.ClientSize.Height);
            int bottom = y + Scale(ButtonHeight96) + margin;

            if (bottom < leastHeight)
                y += leastHeight - bottom;

            Size button = new Size(Scale(ButtonWidth96), Scale(ButtonHeight96));
            _okButton.SetBounds(ContentPanel.ClientSize.Width - margin - button.Width, y, button.Width, button.Height);
            _cancelButton.SetBounds(_okButton.Left - Scale(ButtonGap96) - button.Width, y, button.Width, button.Height);
            y += button.Height + margin;

            // What is around the content panel (title bar, edge) is the difference of the two heights.
            int around = ClientSize.Height - ContentPanel.ClientSize.Height;
            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            ClientSize = new Size(width, around + y);
            MinimumSize = Size;
            MaximumSize = Size;
        }

        private static bool HasTextAfter(Label[] texts, int index)
        {
            for (int i = index + 1; i < texts.Length; i++)
            {
                if (texts[i].Text.Length > 0)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// The themed replacement for the input box of Visual Basic - call <see cref="Show"/>. It is shown as a dialog and returns what the user typed.
    /// </summary>
    public static class InputBox
    {
        /// <summary>
        /// Asks the user for a text in a modal dialog and disposes it. Returns what was entered, or null when the dialog was cancelled (Cancel,
        /// Escape, the close button). An empty text is not accepted (OK does nothing until something is typed).
        /// </summary>
        /// <param name="prompt">The text above the box.</param>
        /// <param name="title">The title of the window ("Input" or "Eingabe" when none is given).</param>
        /// <param name="text">The text the box starts with (selected).</param>
        /// <param name="password">Whether what is typed is hidden (with an eye to show it).</param>
        /// <param name="owner">The window it stands over (null: the middle of the screen).</param>
        /// <param name="hint">A short note in small gray letters under the prompt (null: none).</param>
        /// <param name="placeholder">A hint in the empty box that has not the focus.</param>
        /// <param name="maxLength">The most characters the box takes; 0 is no limit.</param>
        /// <param name="validate">Checks the text when OK is clicked: null when it is fine, else the message that is shown in red (the dialog stays open).</param>
        /// <param name="trim">Whether the spaces at the beginning and the end are cut off.</param>
        /// <param name="size">One of the three widths (<see cref="FormSize.Medium"/> by default) or an own <see cref="Size"/> (its width, and its height as the least height).</param>
        public static string Show(
            string prompt,
            string title = null,
            string text = "",
            bool password = false,
            Form owner = null,
            string hint = null,
            string placeholder = "",
            int maxLength = 0,
            Func<string, string> validate = null,
            bool trim = false,
            FormSizeOption? size = null)
        {
            FormOptions options = title == null ? null : FormOptions.CreateDialog(title, ContentAlignment.MiddleLeft);

            using (InputBoxForm form = new InputBoxForm(options, prompt, text, password, placeholder, size))
            {
                form.Hint = hint;
                form.MaxLength = maxLength;
                form.Validator = validate;
                form.TrimValue = trim;

                DialogResult result = owner != null ? form.ShowDialog(owner) : form.ShowDialog();

                return result == DialogResult.OK ? form.Value : null;
            }
        }
    }
}
