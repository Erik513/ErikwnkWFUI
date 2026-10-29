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
    /// <summary>Which buttons a <see cref="MessageBox"/> shows.</summary>
    public enum MessageBoxButtons
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel,
        RetryCancel,
        AbortRetryIgnore
    }

    /// <summary>Which icon a <see cref="MessageBox"/> shows in its title bar.</summary>
    public enum MessageBoxIcon
    {
        None,
        Info,
        Warning,
        Error,
        Question,
        Success
    }

    /// <summary>Which button of a <see cref="MessageBox"/> has the focus first (and answers Enter).</summary>
    public enum MessageBoxDefault
    {
        /// <summary>The safe one: No for Yes/No, Cancel where there is a Cancel button (the first - the only - one for OK). A stray key press then does no harm.</summary>
        Safe,

        /// <summary>The first button (the left one: OK or Yes).</summary>
        Button1,

        /// <summary>The second button (No, or Cancel of OK/Cancel).</summary>
        Button2,

        /// <summary>The third button (Cancel of Yes/No/Cancel).</summary>
        Button3
    }

    /// <summary>Overall size preset for a <see cref="MessageBox"/>. Use <see cref="FormSize"/>, the one size setting of all the windows.</summary>
    [Obsolete("Use FormSize (the same three sizes, for every window and dialog of the library).")]
    public enum MessageBoxSize
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// ErikwnkWFUI's themed replacement for
    /// <see cref="System.Windows.Forms.MessageBox"/> - call <c>Show</c>.
    /// </summary>
    /// <remarks>
    /// Named the same as the native dialog it replaces, same as
    /// <see cref="Controls.ListView"/> and friends - callers never need to
    /// spell out <c>ErikwnkWFUI.Forms.MessageBox</c> themselves as long as
    /// they don't also have <c>using System.Windows.Forms;</c> in scope
    /// wherever they call <c>Show</c>.
    /// </remarks>
    public static class MessageBox
    {
        /// <summary>The old way to give the size, with <see cref="MessageBoxSize"/>.</summary>
        [Obsolete("Use the size: FormSize (or an own Size) of Show.")]
        public static DialogResult Show(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon, Form owner, MessageBoxSize size)
        {
            return Show(message, title, buttons, icon, owner, (FormSize)(int)size);
        }

        /// <summary>The old way to give the size, with <see cref="MessageBoxSize"/> and no owner.</summary>
        [Obsolete("Use the size: FormSize (or an own Size) of Show.")]
        public static DialogResult Show(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxSize size)
        {
            return Show(message, title, buttons, icon, null, (FormSize)(int)size);
        }

        /// <summary>Shows the dialog modally and returns which button was clicked (or the safe default - Cancel/No/OK - if the dialog is closed without clicking one).</summary>
        /// <param name="message">The text of the dialog.</param>
        /// <param name="title">The title of the window ("Notice" or "Hinweis" when none is given).</param>
        /// <param name="buttons">Which buttons it has.</param>
        /// <param name="icon">The icon in the title bar.</param>
        /// <param name="owner">The window it stands over (null: the middle of the screen).</param>
        /// <param name="size">One of the three sizes (<see cref="FormSize.Medium"/> by default) or an own <see cref="Size"/>; in pixels as they are, not scaled to the screen.</param>
        /// <param name="colors">A color set for the title bar (one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or a <see cref="Color"/>); null is the normal dark one.</param>
        /// <param name="details">A text the user can open under the message with a "Details" button, for instance the text of an exception (null: none).</param>
        /// <param name="defaultButton">Which button has the focus first and answers Enter: the safe one (No, Cancel) by default.</param>
        /// <param name="buttonTexts">Own texts for the buttons, in their order (a null entry keeps the symbol or the usual text of that button): "Save", "Discard", "Cancel" for Yes/No/Cancel, say.</param>
        /// <param name="playSound">Whether the sound of the icon is played when the box opens (the sounds of Windows for information, warning, error and question); off by default.</param>
        public static DialogResult Show(
            string message,
            string title = null,
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Info,
            Form owner = null,
            FormSizeOption? size = null,
            UIColorSet colors = null,
            string details = null,
            MessageBoxDefault defaultButton = MessageBoxDefault.Safe,
            string[] buttonTexts = null,
            bool playSound = false)
        {
            MessageBoxForm form = UIStyles.Forms.CreateMessageStandard(message, title, buttons, icon, colors, size, details, defaultButton, buttonTexts, playSound);

            try
            {
                if (owner != null)
                    return form.ShowDialog(owner);

                return form.ShowDialog();
            }
            finally
            {
                form.Dispose();
            }
        }

        /// <summary>
        /// Like <c>Show</c> with a check box under the message ("Do not ask again", "Apply to all"): <paramref name="isChecked"/> is what it
        /// starts with and what the user left it at (also when the dialog was closed without a button).
        /// </summary>
        /// <param name="message">The text of the dialog.</param>
        /// <param name="checkBoxText">The text of the check box.</param>
        /// <param name="isChecked">Whether the check box is checked: at the start, and when the dialog has ended.</param>
        /// <param name="title">The title of the window ("Notice" or "Hinweis" when none is given).</param>
        /// <param name="buttons">Which buttons it has.</param>
        /// <param name="icon">The icon in the title bar.</param>
        /// <param name="owner">The window it stands over (null: the middle of the screen).</param>
        /// <param name="size">One of the three sizes or an own <see cref="Size"/>.</param>
        /// <param name="colors">A color set for the title bar; null is the normal dark one.</param>
        public static DialogResult ShowWithCheckBox(
            string message,
            string checkBoxText,
            ref bool isChecked,
            string title = null,
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Info,
            Form owner = null,
            FormSizeOption? size = null,
            UIColorSet colors = null)
        {
            MessageBoxForm form = UIStyles.Forms.CreateMessageStandard(message, title, buttons, icon, colors, size);
            form.CheckBoxText = checkBoxText;
            form.CheckBoxChecked = isChecked;

            try
            {
                DialogResult result = owner != null ? form.ShowDialog(owner) : form.ShowDialog();
                isChecked = form.CheckBoxChecked;

                return result;
            }
            finally
            {
                form.Dispose();
            }
        }
    }

    /// <summary>
    /// The dialog behind <see cref="MessageBox"/>: a text (it scrolls when it does not fit), an icon in the title bar, and the buttons.
    /// Make one with <c>UIStyles.Forms.CreateMessageStandard</c> / <c>CreateMessagePrimary</c> and show it with <c>ShowDialog</c>, or call
    /// <c>MessageBox.Show</c>. A window that is closed without a button returns the safe result (No for Yes/No, Cancel where there is a
    /// Cancel button, OK for OK). Ctrl+C copies the text of the box (as the box of Windows does), <see cref="Details"/> adds a text that
    /// the user can open under the message, and <see cref="MessageBoxDefault"/> says which button has the focus first.
    /// </summary>
    public class MessageBoxForm : AppForm
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
        private string _details = "";
        private bool _detailsExpanded;
        private CheckBox _checkBox;
        private Button _helpButton;
        private bool _helpShown;

        private sealed class MessageBoxPreset
        {
            public Size FormSize { get; set; }
            public int ButtonPanelHeight { get; set; }
            public int ButtonColumnWidth { get; set; }
            public int ButtonWidth { get; set; }
            public int ButtonHeight { get; set; }
            public int DetailsHeight { get; set; }
            public Padding ContentPadding { get; set; }
            public Padding ButtonPanelPadding { get; set; }
        }

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

            if (CancelButton is Control cancelControl)
                ActiveControl = cancelControl;
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
                try
                {
                    ClipboardAccess.SetDataObject(new DataObject(DataFormats.UnicodeText, GetTextForCopy()), true);
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    // The clipboard is in use by another program: nothing is copied this time.
                }

                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ---- size and place

        private MessageBoxPreset GetPreset()
        {
            // The size of the window comes from the common table (or is the own one); the rest of the layout follows the ready-made size.
            Size formSize = _size.Resolve(FormKind.MessageBox);

            switch (_size.Preset)
            {
                case FormSize.Small:
                    return new MessageBoxPreset
                    {
                        FormSize = formSize,
                        ButtonPanelHeight = 52,
                        ButtonColumnWidth = 78,
                        ButtonWidth = 72,
                        ButtonHeight = 28,
                        DetailsHeight = 80,
                        ContentPadding = new Padding(18, 12, 18, 8),
                        ButtonPanelPadding = new Padding(8, 8, 18, 12)
                    };

                case FormSize.Large:
                    return new MessageBoxPreset
                    {
                        FormSize = formSize,
                        ButtonPanelHeight = 80,
                        ButtonColumnWidth = 140,
                        ButtonWidth = 130,
                        ButtonHeight = 40,
                        DetailsHeight = 170,
                        ContentPadding = new Padding(32, 26, 32, 14),
                        ButtonPanelPadding = new Padding(12, 16, 32, 22)
                    };

                default:
                    return new MessageBoxPreset
                    {
                        FormSize = formSize,
                        ButtonPanelHeight = 70,
                        ButtonColumnWidth = 125,
                        ButtonWidth = 120,
                        ButtonHeight = 35,
                        DetailsHeight = 120,
                        ContentPadding = new Padding(28, 20, 28, 10),
                        ButtonPanelPadding = new Padding(12, 12, 28, 18)
                    };
            }
        }

        private void BuildLayout()
        {
            MessageBoxPreset preset = GetPreset();

            _messageLabel = UILabelFactory.CreateStandard(_message);
            _messageLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _messageLabel.TextAlign = ContentAlignment.TopCenter;
            ThemeWatcher.FollowForeColor(_messageLabel, () => UIStyles.Colors.TextPrimary);
            _messageLabel.Font = UIStyles.Fonts.Normal;
            _messageLabel.BackColor = Color.Transparent;
            _messageLabel.AutoEllipsis = false;

            // The message scrolls when it is longer than the window can be.
            _messagePanel = UIPanelFactory.CreateMedium();
            _messagePanel.Dock = DockStyle.None;
            _messagePanel.AutoScroll = true;
            _messagePanel.Controls.Add(_messageLabel);

            _detailsBox = UIStyles.TextBoxes.CreateStandard(_details);
            _detailsBox.Multiline = true;
            _detailsBox.ReadOnly = true;
            _detailsBox.ScrollBars = ScrollBars.Vertical;
            _detailsBox.Visible = false;

            _buttonPanel = CreateButtonPanel();

            ContentPanel.Controls.Clear();
            ContentPanel.Controls.Add(_messagePanel);
            ContentPanel.Controls.Add(_detailsBox);
            ContentPanel.Controls.Add(_buttonPanel);

            UpdateDetailsButton();
        }

        // The message, the details (when they are open) and the buttons one below the other. The window is as high as its size says; a
        // message that needs more makes a window of one of the three sizes higher, up to the height of the screen, and then it scrolls (an own
        // size is kept: the message scrolls in it). The details add their own height.
        private void LayoutContent()
        {
            if (_buttonPanel == null)
                return;

            MessageBoxPreset preset = GetPreset();
            int width = preset.FormSize.Width;

            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            Size = new Size(width, Height);

            int contentWidth = ContentPanel.ClientSize.Width;
            int outside = Height - ContentPanel.ClientSize.Height;
            Padding pad = preset.ContentPadding;
            int detailsHeight = _detailsExpanded ? preset.DetailsHeight : 0;
            int checkHeight = _checkBox != null && _checkBox.Text.Length > 0 ? CheckBoxRowHeight : 0;
            int available = preset.FormSize.Height - outside - preset.ButtonPanelHeight - checkHeight;

            // The height of the message at the width it has, and with the scroll bar when it does not fit.
            int textHeight = MeasureMessage(contentWidth - pad.Horizontal);
            int needed = textHeight + pad.Vertical;
            int messageHeight = available;

            if (!_size.Custom.HasValue)
            {
                int room = Screen.FromControl(this).WorkingArea.Height - 40 - outside - preset.ButtonPanelHeight - detailsHeight - checkHeight;
                messageHeight = Math.Max(available, Math.Min(needed, room));
            }

            bool scrolls = needed > messageHeight;

            if (scrolls)
                textHeight = MeasureMessage(contentWidth - pad.Horizontal - SystemInformation.VerticalScrollBarWidth);

            int labelWidth = contentWidth - pad.Horizontal - (scrolls ? SystemInformation.VerticalScrollBarWidth : 0);
            int labelTop = pad.Top + (scrolls ? 0 : Math.Max(0, (messageHeight - pad.Vertical - textHeight) / 2));

            _messagePanel.SetBounds(0, 0, contentWidth, messageHeight);
            _messageLabel.SetBounds(pad.Left, labelTop, labelWidth, textHeight);
            _messagePanel.AutoScrollMinSize = scrolls ? new Size(0, pad.Top + textHeight + pad.Bottom) : Size.Empty;

            _detailsBox.Visible = detailsHeight > 0;

            if (detailsHeight > 0)
                _detailsBox.SetBounds(pad.Left, messageHeight, contentWidth - pad.Horizontal, detailsHeight - 8);

            if (_checkBox != null)
            {
                _checkBox.Visible = checkHeight > 0;
                _checkBox.SetBounds(pad.Left, messageHeight + detailsHeight, contentWidth - pad.Horizontal, checkHeight);
            }

            _buttonPanel.SetBounds(0, messageHeight + detailsHeight + checkHeight, contentWidth, preset.ButtonPanelHeight);

            int height = outside + messageHeight + detailsHeight + checkHeight + preset.ButtonPanelHeight;
            Size = new Size(width, height);
            MinimumSize = Size;
            MaximumSize = Size;

            KeepOnScreen();
        }

        private int MeasureMessage(int width)
        {
            if (_message.Length == 0)
                return 0;

            return TextRenderer.MeasureText(_message, _messageLabel.Font, new Size(Math.Max(1, width), int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
        }

        // A window that has become higher (the details were opened) does not reach below the screen.
        private void KeepOnScreen()
        {
            if (!IsHandleCreated || WindowState != FormWindowState.Normal)
                return;

            Rectangle area = Screen.FromControl(this).WorkingArea;

            if (Bottom > area.Bottom)
                Top = Math.Max(area.Top, area.Bottom - Height);
        }

        // ---- the buttons

        private Control CreateButtonPanel()
        {
            MessageBoxPreset preset = GetPreset();

            TableLayoutPanel buttonPanel = UITableLayoutPanelFactory.CreateStandard(1, 1);

            buttonPanel.Dock = DockStyle.None;
            ThemeWatcher.FollowBackColor(buttonPanel, () => UIColors.BackgroundMedium);
            buttonPanel.Padding = preset.ButtonPanelPadding;
            buttonPanel.Margin = new Padding(0);

            AddButtonsToPanel(buttonPanel);

            return buttonPanel;
        }

        private void AddButtonsToPanel(TableLayoutPanel buttonPanel)
        {
            MessageBoxPreset preset = GetPreset();
            DialogButtonInfo[] buttonInfos = GetButtons();

            buttonPanel.ColumnCount = buttonInfos.Length + 1;
            buttonPanel.ColumnStyles.Clear();
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            buttonPanel.RowStyles.Clear();
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // At the left: the help button and the button of the details. Each is there only when it is wanted (details, ShowHelpButton).
            Panel left = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };

            _detailsButton = UIButtonFactory.CreateStandard("", "", new Size(preset.ButtonWidth, preset.ButtonHeight));
            _detailsButton.Dock = DockStyle.Left;
            _detailsButton.Margin = new Padding(0);
            _detailsButton.Width = preset.ButtonWidth + 10;
            _detailsButton.Visible = _details.Length > 0;
            _detailsButton.Click += (sender, e) => DetailsExpanded = !DetailsExpanded;

            _helpButton = UIButtonFactory.CreateSymbol(UIStyles.Symbols.Help, UIStrings.Get("MessageBox.Help"), new Size(preset.ButtonHeight, preset.ButtonHeight), (UIColorSet)null);
            _helpButton.Dock = DockStyle.Left;
            _helpButton.Margin = new Padding(0);
            _helpButton.Width = preset.ButtonHeight + 8;
            _helpButton.AccessibleName = UIStrings.Get("MessageBox.Help");
            _helpButton.Visible = _helpShown;
            _helpButton.Click += (sender, e) => { EventHandler handler = HelpClicked; if (handler != null) handler(this, EventArgs.Empty); };

            // (Docked from the last one added: the help button stands at the very left.)
            left.Controls.Add(_detailsButton);
            left.Controls.Add(_helpButton);
            buttonPanel.Controls.Add(left, 0, 0);

            for (int i = 0; i < buttonInfos.Length; i++)
            {
                // An own text for the button replaces the symbol (or the usual text); a null entry keeps it.
                string own = _buttonTexts != null && i < _buttonTexts.Length && !string.IsNullOrEmpty(_buttonTexts[i]) ? _buttonTexts[i] : null;
                string name = own ?? buttonInfos[i].Name;
                string text = own ?? buttonInfos[i].Text;

                Button button = CreateDialogButton(
                    text,
                    buttonInfos[i].Result);

                // The column is as wide as the preset says, and wider where the text would not fit (long words, own texts).
                int textWidth = TextRenderer.MeasureText(text, button.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;

                buttonPanel.ColumnStyles.Add(
                    new ColumnStyle(
                        SizeType.Absolute,
                        Math.Max(preset.ButtonColumnWidth, textWidth + 24 + 6)));

                // The buttons with a symbol are named for the screen reader and the tooltip - in the language of the library.
                button.AccessibleName = name;
                UIButtonFactory.UpdateTooltip(button, name);

                _dialogButtons.Add(button);
                _buttonNames.Add(name);
                buttonPanel.Controls.Add(button, i + 1, 0);
            }
        }

        private void UpdateDetailsButton()
        {
            if (_detailsButton != null)
                _detailsButton.Text = UIStrings.Get("MessageBox.Details") + (_detailsExpanded ? " ▴" : " ▾");
        }

        private Button CreateDialogButton(string text, DialogResult result)
        {
            Button button = CreateButton(text, result);

            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(6, 0, 0, 0);
            button.DialogResult = result;
            button.Click += OnDialogButtonClick;

            ConfigureAcceptCancelButton(button, result);

            return button;
        }

        private Button CreateButton(string text, DialogResult result)
        {
            MessageBoxPreset preset = GetPreset();

            Size size = new Size(
                preset.ButtonWidth,
                preset.ButtonHeight);

            if (result == DialogResult.OK || result == DialogResult.Yes)
                return UIButtonFactory.CreateGreen(text, "", size);

            if (result == DialogResult.No || result == DialogResult.Abort)
                return UIButtonFactory.CreateRed(text, "", size);

            return UIButtonFactory.CreateStandard(text, "", size);
        }

        private void ConfigureAcceptCancelButton(Button button, DialogResult result)
        {
            if (result == DialogResult.OK || result == DialogResult.Yes || result == DialogResult.Retry)
                AcceptButton = button;

            if (result == DialogResult.Cancel || result == DialogResult.No || result == DialogResult.Abort)
                CancelButton = button;
        }

        private void OnDialogButtonClick(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button == null)
                return;

            _buttonClicked = true;
            DialogResult = button.DialogResult;
            Close();
        }

        private DialogButtonInfo[] GetButtons()
        {
            DialogButtonInfo ok = new DialogButtonInfo(UIStyles.Symbols.Check, DialogResult.OK, UIStrings.Get("MessageBox.Ok"));
            DialogButtonInfo yes = new DialogButtonInfo(UIStyles.Symbols.Check, DialogResult.Yes, UIStrings.Get("MessageBox.Yes"));
            DialogButtonInfo no = new DialogButtonInfo(UIStyles.Symbols.Cancel, DialogResult.No, UIStrings.Get("MessageBox.No"));
            DialogButtonInfo cancel = new DialogButtonInfo(UIStrings.Get("MessageBox.Cancel"), DialogResult.Cancel, UIStrings.Get("MessageBox.Cancel"));
            DialogButtonInfo retry = new DialogButtonInfo(UIStrings.Get("MessageBox.Retry"), DialogResult.Retry, UIStrings.Get("MessageBox.Retry"));
            DialogButtonInfo abort = new DialogButtonInfo(UIStrings.Get("MessageBox.Abort"), DialogResult.Abort, UIStrings.Get("MessageBox.Abort"));
            DialogButtonInfo ignore = new DialogButtonInfo(UIStrings.Get("MessageBox.Ignore"), DialogResult.Ignore, UIStrings.Get("MessageBox.Ignore"));

            switch (_buttons)
            {
                case MessageBoxButtons.OKCancel:
                    return new[] { ok, cancel };

                case MessageBoxButtons.YesNo:
                    return new[] { yes, no };

                case MessageBoxButtons.YesNoCancel:
                    return new[] { yes, no, cancel };

                case MessageBoxButtons.RetryCancel:
                    return new[] { retry, cancel };

                case MessageBoxButtons.AbortRetryIgnore:
                    return new[] { abort, retry, ignore };

                default:
                    return new[] { ok };
            }
        }

        // A dialog that is closed without a button has the result that is safe for its buttons - not the Cancel that a closed dialog has by
        // itself, which a box with Yes and No does not even have: No for Yes/No, Cancel where there is a Cancel button, OK for OK.
        private void EnsureDialogResult()
        {
            if (_buttonClicked)
                return;

            DialogResult = GetDefaultDialogResult();
        }

        private DialogResult GetDefaultDialogResult()
        {
            switch (_buttons)
            {
                case MessageBoxButtons.OKCancel:
                case MessageBoxButtons.YesNoCancel:
                case MessageBoxButtons.RetryCancel:
                    return DialogResult.Cancel;

                case MessageBoxButtons.YesNo:
                    return DialogResult.No;

                case MessageBoxButtons.AbortRetryIgnore:
                    return DialogResult.Abort;

                default:
                    return DialogResult.OK;
            }
        }

        // (For the factories, which make the options of the window before the window.)
        internal static Image GetTitleBarIconOf(MessageBoxIcon icon)
        {
            return GetTitleBarIcon(icon);
        }

        private static Image GetTitleBarIcon(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Info:
                    return SystemIcons.Information.ToBitmap();

                case MessageBoxIcon.Warning:
                    return SystemIcons.Warning.ToBitmap();

                case MessageBoxIcon.Error:
                    return SystemIcons.Error.ToBitmap();

                case MessageBoxIcon.Question:
                    return SystemIcons.Question.ToBitmap();

                case MessageBoxIcon.Success:
                    return SystemIcons.Shield.ToBitmap();

                default:
                    return null;
            }
        }

        private struct DialogButtonInfo
        {
            public readonly string Text;
            public readonly DialogResult Result;
            public readonly string Name;

            public DialogButtonInfo(string text, DialogResult result, string name)
            {
                Text = text;
                Result = result;
                Name = name;
            }
        }
    }
}
