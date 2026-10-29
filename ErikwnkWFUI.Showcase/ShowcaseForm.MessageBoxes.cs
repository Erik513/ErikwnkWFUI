using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using Button = System.Windows.Forms.Button;
using ComboBox = System.Windows.Forms.ComboBox;
using Label = System.Windows.Forms.Label;
using MessageBoxButtons = ErikwnkWFUI.Forms.MessageBoxButtons;
using MessageBoxIcon = ErikwnkWFUI.Forms.MessageBoxIcon;

namespace ErikwnkWFUI.Showcase
{
    // The MessageBox page of the forms tab: the rows set what the sample shows, the button shows it (modal, like the box of Windows) and the
    // result line says which button came back (and what the check box and the help button did).
    public partial class ShowcaseForm
    {
        private ComboBox _messageLook;
        private ComboBox _messageButtons;
        private ComboBox _messageIcon;
        private ComboBox _messageSize;
        private ComboBox _messageText;
        private ComboBox _messageDefault;
        private ComboBox _messageButtonTexts;
        private ToggleSwitch _messageOwner;
        private ToggleSwitch _messageDetails;
        private ToggleSwitch _messageCheckBox;
        private ToggleSwitch _messageHelp;
        private ToggleSwitch _messageSound;
        private Label _messageResult;

        private void AddMessageBoxSection(PropertyTable table)
        {
            table.AddSection("MessageBox.Show: a modal message");

            Button show = UIStyles.Buttons.CreateStandard("Show", size: new Size(110, 32));
            show.Click += (sender, e) => ShowSampleMessage();
            table.AddRow("Message box", show);

            _messageResult = UIStyles.Labels.CreateStandard("(nothing shown yet)");
            table.AddRow("Result (the button that was clicked)", _messageResult);

            _messageLook = CreateFormChoice("Standard", "Primary", "colors: Yellow");
            _messageText = CreateFormChoice("short", "a few lines", "a long text", "a long word");
            _messageButtons = CreateFormChoice("OK", "OKCancel", "YesNo", "YesNoCancel", "RetryCancel", "AbortRetryIgnore");
            _messageIcon = CreateFormChoice("Info", "Warning", "Error", "Question", "Success", "None");
            _messageSize = CreateFormChoice("Small", "Medium", "Large", SizeChoiceOwn);
            _messageSize.SelectedIndex = 1;
            _messageOwner = UIStyles.ToggleSwitches.CreateStandard(true);

            table.AddSection("UIStyles.Forms.CreateMessageStandard / CreateMessagePrimary (message, title, buttons, icon, colors, size)");
            table.AddRow("Colors (the title bar)", _messageLook);
            table.AddRow("message", _messageText);
            table.AddRow("buttons", _messageButtons);
            table.AddRow("icon", _messageIcon);
            table.AddRow("size (FormSize or own Size)", _messageSize);
            table.AddRow("owner (stands over this window)", _messageOwner);

            _messageDefault = CreateFormChoice("Safe (No, Cancel)", "Button1", "Button2", "Button3");
            _messageButtonTexts = CreateFormChoice("the usual ones", "Save / Discard / Cancel", "Delete / Keep");
            _messageDetails = UIStyles.ToggleSwitches.CreateStandard(false);
            _messageCheckBox = UIStyles.ToggleSwitches.CreateStandard(false);
            _messageHelp = UIStyles.ToggleSwitches.CreateStandard(false);
            _messageSound = UIStyles.ToggleSwitches.CreateStandard(false);

            table.AddSection("More: details, default button, own texts, check box, help, sound");
            table.AddRow("details (Details button, closed at first)", _messageDetails);
            table.AddRow("defaultButton (focus and Enter)", _messageDefault);
            table.AddRow("buttonTexts (replace the symbols)", _messageButtonTexts);
            table.AddRow("CheckBoxText (Do not ask again)", _messageCheckBox);
            table.AddRow("ShowHelpButton (raises HelpClicked)", _messageHelp);
            table.AddRow("playSound (the sound of the icon)", _messageSound);
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
                    text = string.Join(" ", System.Linq.Enumerable.Repeat("A long message that goes on and on, to see how the box wraps it and how much room it needs.", 12));
                    break;
                case "a long word":
                    text = new string('W', 120);
                    break;
                default:
                    text = "Done.";
                    break;
            }

            FormSizeOption? size = ParseSizeChoice(_messageSize);
            MessageBoxButtons buttons = (MessageBoxButtons)Enum.Parse(typeof(MessageBoxButtons), (string)_messageButtons.SelectedItem);
            MessageBoxIcon icon = (MessageBoxIcon)Enum.Parse(typeof(MessageBoxIcon), (string)_messageIcon.SelectedItem);
            MessageBoxDefault defaultButton = _messageDefault.SelectedIndex == 0 ? MessageBoxDefault.Safe : (MessageBoxDefault)Enum.Parse(typeof(MessageBoxDefault), (string)_messageDefault.SelectedItem);
            string details = _messageDetails.Checked ? "System.IO.IOException: The device is not ready.\r\n   at Sample.Save(String path)\r\n   at Sample.Run()" : null;

            string[] texts = null;

            switch ((string)_messageButtonTexts.SelectedItem)
            {
                case "Save / Discard / Cancel":
                    texts = new[] { "Save", "Discard", null };
                    break;
                case "Delete / Keep":
                    texts = new[] { "Delete", "Keep" };
                    break;
            }

            string look = (string)_messageLook.SelectedItem;
            MessageBoxForm form = look == "Primary"
                ? UIStyles.Forms.CreateMessagePrimary(text, "Message", buttons, icon, size, details, defaultButton, texts, _messageSound.Checked)
                : UIStyles.Forms.CreateMessageStandard(text, "Message", buttons, icon, look == "colors: Yellow" ? UIStyles.Colors.Sets.Yellow : null, size, details, defaultButton, texts, _messageSound.Checked);

            int helpClicks = 0;

            if (_messageCheckBox.Checked)
                form.CheckBoxText = "Do not ask again";

            if (_messageHelp.Checked)
            {
                form.ShowHelpButton = true;
                form.HelpClicked += (sender, e) => helpClicks++;
            }

            DialogResult result;

            try
            {
                result = _messageOwner.Checked ? form.ShowDialog(this) : form.ShowDialog();
                string extra = _messageCheckBox.Checked ? "; check box " + (form.CheckBoxChecked ? "checked" : "not checked") : "";
                extra += _messageHelp.Checked ? "; help clicked " + helpClicks + "x" : "";
                _messageResult.Text = result + extra;
            }
            finally
            {
                form.Dispose();
            }
        }
    }
}
