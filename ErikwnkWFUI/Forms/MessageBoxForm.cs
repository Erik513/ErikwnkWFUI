using System;
using System.Collections.Generic;
using System.Text;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Controls;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;
using TableLayoutPanel = System.Windows.Forms.TableLayoutPanel;
using Panel = System.Windows.Forms.Panel;
using CheckBox = System.Windows.Forms.CheckBox;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// The dialog behind <see cref="MessageBox"/>: a text (it scrolls when it does not fit), an icon in the title bar, and the buttons.
    /// Make one with <c>UIStyles.Forms.CreateMessageStandard</c> / <c>CreateMessagePrimary</c> and show it with <c>ShowDialog</c>, or call
    /// <c>MessageBox.Show</c>. A window that is closed without a button returns the safe result (No for Yes/No, Cancel where there is a
    /// Cancel button, Abort for Abort/Retry/Ignore, OK for OK). Ctrl+C copies the text of the box (as the box of Windows does), <see cref="Details"/> adds a text that
    /// the user can open under the message, and <see cref="MessageBoxDefault"/> says which button has the focus first.
    /// </summary>
    public partial class MessageBoxForm : AppForm
    {
        private const string Separator = "---------------------------";
        private const int CheckBoxRowHeight = 30;

        private readonly string _message;
        private readonly MessageBoxButtons _buttons;
        private readonly FormSizeOption _size;
        private readonly MessageBoxDefault _defaultButton;
        private readonly List<Button> _dialogButtons = new List<Button>();
        private readonly List<string> _buttonNames = new List<string>();
        private readonly string[] _buttonTexts;
        private readonly MessageBoxIcon _icon;

        private Panel _messagePanel;
        private Label _messageLabel;
        private TextBox _detailsBox;
        private Button _detailsButton;
        private Control _buttonPanel;
        private int _columnsWidth;
        private string _details = "";
        private bool _detailsExpanded;
        private CheckBox _checkBox;
        private Button _helpButton;
        private bool _helpShown;


        /// <summary>The old way to give the size, with <see cref="MessageBoxSize"/>.</summary>
        [Obsolete("Use the size: FormSize (or an own Size).")]
        public MessageBoxForm(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxSize size)
            : this(message, title, buttons, icon, (FormSize)(int)size)
        {
        }

        /// <param name="message">The text of the dialog.</param>
        /// <param name="title">The title of the window ("Notice" or "Hinweis" when none is given).</param>
        /// <param name="buttons">Which buttons it has.</param>
        /// <param name="icon">The icon in the title bar.</param>
        /// <param name="size">One of the three sizes (<see cref="FormSize.Medium"/> by default) or an own <see cref="Size"/> (pixels as they are).</param>
        /// <param name="details">A text the user can open under the message (null: none).</param>
        /// <param name="defaultButton">Which button has the focus first (the safe one by default).</param>
        /// <param name="buttonTexts">Own texts for the buttons, in their order (a null entry keeps the usual one).</param>
        public MessageBoxForm(
            string message,
            string title,
            MessageBoxButtons buttons,
            MessageBoxIcon icon,
            FormSizeOption? size = null,
            string details = null,
            MessageBoxDefault defaultButton = MessageBoxDefault.Safe,
            string[] buttonTexts = null)
            : this(
                FormOptions.CreateDialog(
                    title: title ?? UIStrings.Get("MessageBox.Title"),
                    titleTextAlign: ContentAlignment.MiddleLeft,
                    icon: GetTitleBarIcon(icon)),
                message,
                buttons,
                size,
                details,
                defaultButton,
                icon,
                buttonTexts)
        {
            UsesDefaultTitle = title == null;
        }

        // The window with the options it is given (a dialog made by FormOptions, with the color set or the accent of the title bar): what
        // UIStyles.Forms.CreateMessageStandard / CreateMessagePrimary make.
        internal MessageBoxForm(FormOptions options, string message, MessageBoxButtons buttons, FormSizeOption? size, string details = null, MessageBoxDefault defaultButton = MessageBoxDefault.Safe, MessageBoxIcon icon = MessageBoxIcon.None, string[] buttonTexts = null)
            : base(options)
        {
            _icon = icon;
            _buttonTexts = buttonTexts;
            _message = message ?? "";
            _buttons = buttons;
            _size = size ?? FormSize.Medium;
            _defaultButton = defaultButton;
            _details = details ?? "";

            ShowInTaskbar = false;
            Size = GetPreset().FormSize;

            BuildLayout();
            LayoutContent();

            FocusDefaultButton();
            UIStrings.LanguageChanged += OnLanguageChanged;
        }

        /// <summary>A text under the message that the user opens with the "Details" button (the text of an exception, say); empty (the default) has no button.</summary>
        public string Details
        {
            get { return _details; }
            set
            {
                _details = value ?? "";
                _detailsBox.Text = _details;
                _detailsButton.Visible = _details.Length > 0;

                if (_details.Length == 0)
                    _detailsExpanded = false;

                UpdateDetailsButton();
                LayoutContent();
            }
        }

        /// <summary>
        /// A check box under the message ("Do not ask again", "Apply to all"); empty (the default) has none. What the user did with it is
        /// <see cref="CheckBoxChecked"/> once the dialog has ended.
        /// </summary>
        public string CheckBoxText
        {
            get { return _checkBox == null ? "" : _checkBox.Text; }
            set
            {
                EnsureCheckBox();
                _checkBox.Text = value ?? "";
                LayoutContent();
            }
        }

        /// <summary>Whether the check box is checked.</summary>
        public bool CheckBoxChecked
        {
            get { return _checkBox != null && _checkBox.Checked; }
            set
            {
                EnsureCheckBox();
                _checkBox.Checked = value;
            }
        }

        private void EnsureCheckBox()
        {
            if (_checkBox != null)
                return;

            _checkBox = UIStyles.CheckBoxes.CreateStandard();
            _checkBox.AutoSize = false;
            ContentPanel.Controls.Add(_checkBox);
        }

        /// <summary>Whether the sound of the icon is played when the window opens (the sounds of Windows for information, warning, error and question). Off by default.</summary>
        public bool PlaySound { get; set; }

        /// <summary>Whether the box has a help button (a question mark at the left of the buttons) that raises <see cref="HelpClicked"/>. Off by default.</summary>
        public bool ShowHelpButton
        {
            get { return _helpShown; }
            set
            {
                _helpShown = value;
                _helpButton.Visible = value;
                LayoutContent();
            }
        }

        /// <summary>Raised when the help button is clicked; the dialog stays open.</summary>
        public event EventHandler HelpClicked;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (PlaySound)
                PlayIconSound();
        }

        private void PlayIconSound()
        {
            switch (_icon)
            {
                case MessageBoxIcon.Info:
                case MessageBoxIcon.Success:
                    System.Media.SystemSounds.Asterisk.Play();
                    break;
                case MessageBoxIcon.Warning:
                    System.Media.SystemSounds.Exclamation.Play();
                    break;
                case MessageBoxIcon.Error:
                    System.Media.SystemSounds.Hand.Play();
                    break;
                case MessageBoxIcon.Question:
                    System.Media.SystemSounds.Question.Play();
                    break;
            }
        }

        /// <summary>Whether the text of <see cref="Details"/> is open (the window is higher then). Closed by default.</summary>
        public bool DetailsExpanded
        {
            get { return _detailsExpanded; }
            set
            {
                bool expanded = value && _details.Length > 0;

                if (expanded == _detailsExpanded)
                    return;

                _detailsExpanded = expanded;
                UpdateDetailsButton();
                LayoutContent();
            }
        }

        // Which button has the focus when the window opens: the safe one (No, Cancel) unless the caller names another. A stray key press
        // (Space activates the button that has the focus) then does the safe thing. ActiveControl, not Focus(): Focus() needs the window to be
        // the active one already, which it is not this early. The chosen button also answers Enter from anywhere in the window.
        private void FocusDefaultButton()
        {
            int index = (int)_defaultButton - 1;

            if (index >= 0 && index < _dialogButtons.Count)
            {
                ActiveControl = _dialogButtons[index];
                AcceptButton = _dialogButtons[index];

                return;
            }

            // The safe one: the button that cancels (No, Cancel, Abort, the only OK) has the focus and answers Enter from anywhere in the window,
            // from the check box and the details too - Enter must never be a Yes that nobody chose.
            if (CancelButton is Button cancelButton)
            {
                ActiveControl = cancelButton;
                AcceptButton = cancelButton;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            EnsureDialogResult();
            base.OnFormClosing(e);
        }

        // Whether one of the buttons ended the dialog (and not the close button, Alt+F4 or Close()).
        private bool _buttonClicked;

        // ---- the text to copy

        /// <summary>The text that Ctrl+C copies: the title, the message (and the details), and the names of the buttons, in the form the box of Windows uses.</summary>
        public string GetTextForCopy()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine(Separator).AppendLine(Text).AppendLine(Separator).AppendLine(_message);

            if (_details.Length > 0)
                text.AppendLine().AppendLine(_details);

            text.AppendLine(Separator).AppendLine(string.Join("   ", _buttonNames)).AppendLine(Separator);

            return text.ToString();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // (Text that is selected in the details is copied by the box itself.)
            bool selection = ActiveControl is TextBoxBase box && box.SelectionLength > 0;

            if (keyData == (Keys.Control | Keys.C) && !selection)
            {
                // (The clipboard may be in use by another program: nothing is copied this time.)
                ClipboardAccess.TrySetDataObject(new DataObject(DataFormats.UnicodeText, GetTextForCopy()), true);

                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
