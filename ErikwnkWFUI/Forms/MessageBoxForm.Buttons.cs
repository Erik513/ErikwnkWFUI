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
    public partial class MessageBoxForm
    {
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

                int column = ColumnWidthFor(button);
                _columnsWidth += column;
                buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, column));

                // The buttons with a symbol are named for the screen reader and the tooltip - in the language of the library.
                button.AccessibleName = name;
                UIButtonFactory.UpdateTooltip(button, name);

                _dialogButtons.Add(button);
                _buttonNames.Add(name);
                buttonPanel.Controls.Add(button, i + 1, 0);
            }
        }

        // The column is as wide as the preset says, and wider where the text would not fit (long words, own texts).
        private int ColumnWidthFor(Button button)
        {
            int textWidth = TextRenderer.MeasureText(button.Text, button.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;

            return Math.Max(GetPreset().ButtonColumnWidth, textWidth + 24 + 6);
        }

        // ---- language

        /// <summary>Whether the title is the standard one of the language ("Notice", "Hinweis"): it follows a change of the language while the window is open. A title that was given stays.</summary>
        internal bool UsesDefaultTitle { get; set; }

        // The texts of the box follow a change of the language while it is open: the title (when it is the standard one), the buttons that have
        // no text of their own, the help and details buttons. The columns are measured again (a word of the other language may be longer).
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            if (UsesDefaultTitle)
                FormTitle = UIStrings.Get("MessageBox.Title");

            DialogButtonInfo[] infos = GetButtons();
            TableLayoutPanel row = (TableLayoutPanel)_buttonPanel;
            _columnsWidth = 0;

            for (int i = 0; i < _dialogButtons.Count; i++)
            {
                bool own = _buttonTexts != null && i < _buttonTexts.Length && !string.IsNullOrEmpty(_buttonTexts[i]);

                if (!own)
                {
                    _dialogButtons[i].Text = infos[i].Text;
                    _dialogButtons[i].AccessibleName = infos[i].Name;
                    UIButtonFactory.UpdateTooltip(_dialogButtons[i], infos[i].Name);
                    _buttonNames[i] = infos[i].Name;
                }

                int column = ColumnWidthFor(_dialogButtons[i]);
                _columnsWidth += column;
                row.ColumnStyles[i + 1].Width = column;
            }

            _helpButton.AccessibleName = UIStrings.Get("MessageBox.Help");
            UIButtonFactory.UpdateTooltip(_helpButton, UIStrings.Get("MessageBox.Help"));

            UpdateDetailsButton();
            LayoutContent();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                UIStrings.LanguageChanged -= OnLanguageChanged;

            base.Dispose(disposing);
        }

        private void UpdateDetailsButton()
        {
            if (_detailsButton == null)
                return;

            _detailsButton.Text = UIStrings.Get("MessageBox.Details") + (_detailsExpanded ? " ▴" : " ▾");
            _detailsButton.Width = Math.Max(GetPreset().ButtonWidth * 3 / 4, TextRenderer.MeasureText(_detailsButton.Text, _detailsButton.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width + 28);
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

            // Escape closes a box that has only OK too (as the box of Windows does).
            if (result == DialogResult.Cancel || result == DialogResult.No || result == DialogResult.Abort || (result == DialogResult.OK && _buttons == MessageBoxButtons.OK))
                CancelButton = button;
        }

        private void OnDialogButtonClick(object sender, EventArgs e)
        {
            Button button = sender as Button;

            // A second click (a double click, a queued one) must not change the answer.
            if (button == null || _buttonClicked)
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

        // (Internal for the factories, which make the options of the window before the window.)
        internal static Image GetTitleBarIcon(MessageBoxIcon icon)
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
