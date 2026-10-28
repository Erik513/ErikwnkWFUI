using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using Button = System.Windows.Forms.Button;
using ComboBox = System.Windows.Forms.ComboBox;
using Label = System.Windows.Forms.Label;

namespace ErikwnkWFUI.Showcase
{
    // The MessageBox page of the forms tab: the rows set what the sample shows, the button shows it (modal, like the box of Windows) and the
    // result line says which button came back.
    public partial class ShowcaseForm
    {
        private ComboBox _messageButtons;
        private ComboBox _messageIcon;
        private ComboBox _messageSize;
        private ComboBox _messageText;
        private ToggleSwitch _messageOwner;
        private Label _messageResult;

        private void AddMessageBoxSection(PropertyTable table)
        {
            table.AddSection("MessageBox.Show: a modal message");

            Button show = UIStyles.Buttons.CreateStandard("Show", size: new Size(110, 32));
            show.Click += (sender, e) => ShowSampleMessage();
            table.AddRow("Message box", show);

            _messageResult = UIStyles.Labels.CreateStandard("(nothing shown yet)");
            table.AddRow("Result (the button that was clicked)", _messageResult);

            _messageText = CreateFormChoice("short", "a few lines", "a long text");
            _messageButtons = CreateFormChoice("OK", "OKCancel", "YesNo", "YesNoCancel");
            _messageIcon = CreateFormChoice("Info", "Warning", "Error", "Question", "Success", "None");
            _messageSize = CreateFormChoice("Small", "Medium", "Large", SizeChoiceOwn);
            _messageSize.SelectedIndex = 1;
            _messageOwner = UIStyles.ToggleSwitches.CreateStandard(true);

            table.AddSection("MessageBox.Show(message, title, buttons, icon, owner, size)");
            table.AddRow("message", _messageText);
            table.AddRow("buttons", _messageButtons);
            table.AddRow("icon", _messageIcon);
            table.AddRow("size (FormSize or own Size)", _messageSize);
            table.AddRow("owner (stands over this window)", _messageOwner);
        }

        private void ShowSampleMessage()
        {
            string text;

            switch ((string)_messageText.SelectedItem)
            {
                case "a few lines":
                    text = "The file was changed.\r\nDo you want to keep the changes?\r\n\r\nThey cannot be taken back.";
                    break;
                case "a long text":
                    text = string.Join(" ", System.Linq.Enumerable.Repeat("A long message that goes on and on, to see how the box wraps it and how much room it needs.", 5));
                    break;
                default:
                    text = "Done.";
                    break;
            }

            FormSizeOption? size = ParseSizeChoice(_messageSize);

            DialogResult result = ErikwnkWFUI.Forms.MessageBox.Show(
                text,
                "Message",
                (ErikwnkWFUI.Forms.MessageBoxButtons)Enum.Parse(typeof(ErikwnkWFUI.Forms.MessageBoxButtons), (string)_messageButtons.SelectedItem),
                (ErikwnkWFUI.Forms.MessageBoxIcon)Enum.Parse(typeof(ErikwnkWFUI.Forms.MessageBoxIcon), (string)_messageIcon.SelectedItem),
                _messageOwner.Checked ? this : null,
                size);

            _messageResult.Text = result.ToString();
        }
    }
}
