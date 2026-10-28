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
    /// The texts of the buttons and the default title follow the language of the library.
    /// </summary>
    public class InputBoxForm : AppForm
    {
        // The sizes are given for 96 dpi and scaled to the screen.
        private const int ClientWidth96 = 400;
        private const int Margin96 = 24;
        private const int GapAfterPrompt96 = 12;
        private const int GapBeforeButtons96 = 20;
        private const int TextBoxHeight96 = 30;
        private const int ButtonWidth96 = 100;
        private const int ButtonHeight96 = 32;
        private const int ButtonGap96 = 8;

        private readonly Label _promptLabel;
        private readonly TextBox _textBox;
        private readonly Button _okButton;
        private readonly Button _cancelButton;
        private readonly bool _defaultTitle;
        private int _dpiOverride;
        private string _prompt;

        /// <summary>The text that was entered, once the dialog was closed with OK (what the user accepted); empty before.</summary>
        public string Value { get; private set; } = "";

        /// <summary>The box the user types into, for what is not covered here (a maximum length, an input filter ...).</summary>
        public TextBox InputTextBox
        {
            get { return _textBox; }
        }

        /// <summary>Whether OK also accepts an empty text. By default it does not: OK does nothing until something is typed.</summary>
        public bool AllowEmpty { get; set; }

        /// <summary>The text above the box; the window grows or shrinks to it.</summary>
        public string Prompt
        {
            get { return _prompt; }
            set
            {
                _prompt = value ?? "";
                _promptLabel.Text = _prompt;
                LayoutContent();
            }
        }

        /// <summary>The text of the OK button: "OK", or null for the text of the library.</summary>
        public string OkButtonText
        {
            get { return _okButton.Text; }
            set { _okButton.Text = value ?? UIStrings.Get("InputBox.Ok"); }
        }

        /// <summary>The text of the Cancel button ("Cancel" or "Abbrechen"), or null for the text of the library.</summary>
        public string CancelButtonText
        {
            get { return _cancelButton.Text; }
            set { _cancelButton.Text = value ?? UIStrings.Get("InputBox.Cancel"); }
        }

        /// <param name="options">The options of the window (a dialog, as <see cref="FormOptions.CreateDialog"/>).</param>
        /// <param name="prompt">The text above the box.</param>
        /// <param name="text">The text the box starts with (selected, so typing replaces it).</param>
        /// <param name="password">Whether the box hides what is typed (with an eye to show it).</param>
        public InputBoxForm(FormOptions options, string prompt, string text = "", bool password = false)
            : base(options ?? FormOptions.CreateDialog(UIStrings.Get("InputBox.Title"), ContentAlignment.MiddleLeft))
        {
            _defaultTitle = options == null || options.Title == UIStrings.Get("InputBox.Title");
            _prompt = prompt ?? "";

            ShowInTaskbar = false;

            _promptLabel = UIStyles.Labels.CreateStandard(_prompt);
            // Placed by LayoutContent: it wraps (no ellipsis) and does not follow the size of the window by itself.
            _promptLabel.AutoEllipsis = false;
            _promptLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _promptLabel.TextAlign = ContentAlignment.TopLeft;

            _textBox = password ? UIStyles.TextBoxes.CreatePassword(text ?? "") : UIStyles.TextBoxes.CreateStandard(text ?? "");

            _cancelButton = UIStyles.Buttons.CreateStandard(UIStrings.Get("InputBox.Cancel"));
            _cancelButton.DialogResult = DialogResult.Cancel;

            _okButton = UIStyles.Buttons.CreatePrimary(UIStrings.Get("InputBox.Ok"));
            _okButton.Click += (sender, e) => Accept();

            ContentPanel.Controls.AddRange(new Control[] { _promptLabel, _textBox, _cancelButton, _okButton });

            AcceptButton = _okButton;
            CancelButton = _cancelButton;
            ActiveControl = _textBox;

            LayoutContent();
            UIStrings.LanguageChanged += OnLanguageChanged;
        }

        // OK takes the text - unless it is empty and an empty text is not wanted: then nothing happens and the box gets the focus.
        private void Accept()
        {
            if (_textBox.Text.Length == 0 && !AllowEmpty)
            {
                _textBox.Focus();

                return;
            }

            Value = _textBox.Text;
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

            _okButton.Text = UIStrings.Get("InputBox.Ok");
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

        // Prompt, box and buttons one below the other; the height of the window follows the height of the (wrapped) prompt.
        private void LayoutContent()
        {
            if (_promptLabel == null)
                return;

            int margin = Scale(Margin96);
            int width = Scale(ClientWidth96);

            // The window has an edge around it: the content is as wide as the client area minus that (measured, not assumed).
            ClientSize = new Size(width, ClientSize.Height);
            int inner = ContentPanel.ClientSize.Width - 2 * margin;

            int promptHeight = _prompt.Length == 0
                ? 0
                : TextRenderer.MeasureText(_prompt, _promptLabel.Font, new Size(inner, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;

            int y = margin;
            _promptLabel.SetBounds(margin, y, inner, promptHeight);
            y += promptHeight + (promptHeight > 0 ? Scale(GapAfterPrompt96) : 0);

            _textBox.SetBounds(margin, y, inner, Scale(TextBoxHeight96));
            y += Scale(TextBoxHeight96) + Scale(GapBeforeButtons96);

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
    }

    /// <summary>
    /// The themed replacement for the input box of Visual Basic - call <see cref="Show"/>. It is shown as a dialog and returns what the user typed.
    /// </summary>
    public static class InputBox
    {
        /// <summary>
        /// Asks the user for a text. Returns what was entered, or null when the dialog was cancelled (Cancel, Escape, the close button).
        /// </summary>
        /// <param name="prompt">The text above the box.</param>
        /// <param name="title">The title of the window ("Input" or "Eingabe" when none is given).</param>
        /// <param name="text">The text the box starts with (selected).</param>
        /// <param name="password">Whether what is typed is hidden (with an eye to show it).</param>
        /// <param name="owner">The window it stands over (null: the middle of the screen).</param>
        public static string Show(string prompt, string title = null, string text = "", bool password = false, Form owner = null)
        {
            FormOptions options = title == null ? null : FormOptions.CreateDialog(title, ContentAlignment.MiddleLeft);

            using (InputBoxForm form = new InputBoxForm(options, prompt, text, password))
            {
                DialogResult result = owner != null ? form.ShowDialog(owner) : form.ShowDialog();

                return result == DialogResult.OK ? form.Value : null;
            }
        }
    }
}
