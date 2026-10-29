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
        // size is kept: the message scrolls in it). The details and the check box row add their own height. The window keeps its width.
        private void LayoutContent()
        {
            if (_buttonPanel == null)
                return;

            MessageBoxPreset preset = GetPreset();
            int width = preset.FormSize.Width;

            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;

            // The row of buttons keeps its room: a window of one of the three sizes gets wider when the help button, the details button and the
            // buttons of the set do not fit side by side (an own size is kept).
            if (!_size.Custom.HasValue)
            {
                int left = (_detailsButton.Visible ? _detailsButton.Width : 0) + (_helpButton.Visible ? _helpButton.Width : 0);
                int frame = Width - ContentPanel.ClientSize.Width;
                width = Math.Max(width, frame + preset.ButtonPanelPadding.Horizontal + left + _columnsWidth + 8);
            }

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
    }
}
