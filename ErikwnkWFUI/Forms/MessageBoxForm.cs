using System;
using ErikwnkWFUI.Controls;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;
using TableLayoutPanel = System.Windows.Forms.TableLayoutPanel;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Forms
{
    /// <summary>Which buttons a <see cref="MessageBox"/> shows.</summary>
    public enum MessageBoxButtons
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel
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
        public static DialogResult Show(
            string message,
            string title = null,
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Info,
            Form owner = null,
            FormSizeOption? size = null,
            UIColorSet colors = null)
        {
            MessageBoxForm form = UIStyles.Forms.CreateMessageStandard(message, title, buttons, icon, colors, size);

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
    }

    public class MessageBoxForm : AppForm
    {
        private readonly string _message;
        private readonly MessageBoxButtons _buttons;
        private readonly FormSizeOption _size;

        private sealed class MessageBoxPreset
        {
            public Size FormSize { get; set; }
            public int ButtonPanelHeight { get; set; }
            public int ButtonColumnWidth { get; set; }
            public int ButtonWidth { get; set; }
            public int ButtonHeight { get; set; }
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
        /// <param name="title">The title of the window.</param>
        /// <param name="buttons">Which buttons it has.</param>
        /// <param name="icon">The icon in the title bar.</param>
        /// <param name="size">One of the three sizes (<see cref="FormSize.Medium"/> by default) or an own <see cref="Size"/> (pixels as they are).</param>
        public MessageBoxForm(
            string message,
            string title,
            MessageBoxButtons buttons,
            MessageBoxIcon icon,
            FormSizeOption? size = null)
            : this(
                FormOptions.CreateDialog(
                    title: title ?? UIStrings.Get("MessageBox.Title"),
                    titleTextAlign: ContentAlignment.MiddleLeft,
                    icon: GetTitleBarIcon(icon)),
                message,
                buttons,
                size)
        {
        }

        // The window with the options it is given (a dialog made by FormOptions, with the color set or the accent of the title bar): what
        // UIStyles.Forms.CreateMessageStandard / CreateMessagePrimary make.
        internal MessageBoxForm(FormOptions options, string message, MessageBoxButtons buttons, FormSizeOption? size)
            : base(options)
        {
            _message = message ?? "";
            _buttons = buttons;
            _size = size ?? FormSize.Medium;

            ConfigureForm();
            BuildLayout();

            // Whichever button was added first (Yes, for YesNo) otherwise
            // silently ends up with the initial keyboard focus via normal
            // tab order - meaning a stray Space press confirms the
            // destructive option instead of doing nothing, since Space
            // activates whatever's focused (unlike Enter, which respects
            // AcceptButton). Defaulting focus to CancelButton (No/Cancel)
            // makes an accidental key press safe instead. ActiveControl,
            // not Control.Focus() - Focus() requires the top-level form to
            // already be the active window, which isn't guaranteed yet
            // this early; ActiveControl is the documented way to set a
            // form's initial focus before it's actually shown.
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
                        ContentPadding = new Padding(28, 20, 28, 10),
                        ButtonPanelPadding = new Padding(12, 12, 28, 18)
                    };
            }
        }

        private void ConfigureForm()
        {
            MessageBoxPreset preset = GetPreset();

            ShowInTaskbar = false;

            Size = preset.FormSize;
            MinimumSize = preset.FormSize;
            MaximumSize = preset.FormSize;
        }

        private void BuildLayout()
        {
            Panel rootPanel = UIStyles.Panels.CreateMedium();
            TableLayoutPanel mainLayout = CreateMainLayout();

            mainLayout.Controls.Add(CreateContentPanel(), 0, 0);
            mainLayout.Controls.Add(CreateButtonPanel(), 0, 1);

            rootPanel.Controls.Add(mainLayout);

            ContentPanel.Controls.Clear();
            ContentPanel.Controls.Add(rootPanel);
        }

        private TableLayoutPanel CreateMainLayout()
        {
            MessageBoxPreset preset = GetPreset();

            TableLayoutPanel layout = UITableLayoutPanelFactory.CreateStandard(1, 2);

            layout.Dock = DockStyle.Fill;
            ThemeWatcher.FollowBackColor(layout, () => UIStyles.Colors.BackgroundMedium);
            layout.Padding = new Padding(0);
            layout.Margin = new Padding(0);

            layout.RowStyles.Clear();
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, preset.ButtonPanelHeight));

            return layout;
        }

        private Control CreateContentPanel()
        {
            MessageBoxPreset preset = GetPreset();

            Panel panel = UIPanelFactory.CreateMedium();
            panel.Padding = preset.ContentPadding;

            Label messageLabel = CreateMessageLabel();

            panel.Controls.Add(messageLabel);

            return panel;
        }

        private Label CreateMessageLabel()
        {
            Label label = UILabelFactory.CreateStandard(_message);

            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleCenter;
            ThemeWatcher.FollowForeColor(label, () => UIStyles.Colors.TextPrimary);
            label.Font = UIStyles.Fonts.Normal;
            label.BackColor = Color.Transparent;
            label.AutoEllipsis = false;

            return label;
        }

        private Control CreateButtonPanel()
        {
            MessageBoxPreset preset = GetPreset();

            TableLayoutPanel buttonPanel = UITableLayoutPanelFactory.CreateStandard(1, 1);

            buttonPanel.Dock = DockStyle.Fill;
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

            for (int i = 0; i < buttonInfos.Length; i++)
            {
                buttonPanel.ColumnStyles.Add(
                    new ColumnStyle(
                        SizeType.Absolute,
                        preset.ButtonColumnWidth));

                Button button = CreateDialogButton(
                    buttonInfos[i].Text,
                    buttonInfos[i].Result);

                buttonPanel.Controls.Add(button, i + 1, 0);
            }
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

            if (result == DialogResult.No)
                return UIButtonFactory.CreateRed(text, "", size);

            return UIButtonFactory.CreateStandard(text, "", size);
        }

        private void ConfigureAcceptCancelButton(Button button, DialogResult result)
        {
            if (result == DialogResult.OK || result == DialogResult.Yes)
                AcceptButton = button;

            if (result == DialogResult.Cancel || result == DialogResult.No)
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
            switch (_buttons)
            {
                case MessageBoxButtons.OK:
                    return new[]
                    {
                        new DialogButtonInfo(UIStyles.Symbols.Check, DialogResult.OK)
                    };

                case MessageBoxButtons.OKCancel:
                    return new[]
                    {
                        new DialogButtonInfo(UIStyles.Symbols.Check, DialogResult.OK),
                        new DialogButtonInfo(UIStrings.Get("MessageBox.Cancel"), DialogResult.Cancel)
                    };

                case MessageBoxButtons.YesNo:
                    return new[]
                    {
                        new DialogButtonInfo(UIStyles.Symbols.Check, DialogResult.Yes),
                        new DialogButtonInfo(UIStyles.Symbols.Cancel, DialogResult.No)
                    };

                case MessageBoxButtons.YesNoCancel:
                    return new[]
                    {
                        new DialogButtonInfo(UIStyles.Symbols.Check, DialogResult.Yes),
                        new DialogButtonInfo(UIStyles.Symbols.Cancel, DialogResult.No),
                        new DialogButtonInfo(UIStrings.Get("MessageBox.Cancel"), DialogResult.Cancel)
                    };

                default:
                    return new[]
                    {
                        new DialogButtonInfo(UIStyles.Symbols.Check, DialogResult.OK)
                    };
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
                    return DialogResult.Cancel;

                case MessageBoxButtons.YesNo:
                    return DialogResult.No;

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

            public DialogButtonInfo(string text, DialogResult result)
            {
                Text = text;
                Result = result;
            }
        }
    }
}
