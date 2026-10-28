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
    // The InputBox part of the forms tab: the switches say what the input box has, the buttons show it (it is modal, like the box of Windows)
    // and the result line says what came back.
    public partial class ShowcaseForm
    {
        private ComboBox _inputLook;
        private ComboBox _inputMaxLength;
        private ComboBox _inputSize;
        private ToggleSwitch _inputPassword;
        private ToggleSwitch _inputHint;
        private ToggleSwitch _inputPlaceholder;
        private ToggleSwitch _inputError;
        private ToggleSwitch _inputValidator;
        private ToggleSwitch _inputTrim;
        private ToggleSwitch _inputAllowEmpty;
        private ToggleSwitch _inputStartText;
        private Label _inputResult;

        private void AddInputBoxSection(PropertyTable table)
        {
            table.AddSection("InputBox: a prompt, one text box and OK / Cancel (modal)");

            Button showForm = UIStyles.Buttons.CreateStandard("Open", size: new Size(110, 32));
            Button showLine = UIStyles.Buttons.CreateStandard("InputBox.Show", size: new Size(150, 32));
            showForm.Click += (sender, e) => ShowSampleInputForm();
            showLine.Click += (sender, e) => ShowSampleInputLine();
            table.AddRow("Input box", showForm, showLine);

            _inputResult = UIStyles.Labels.CreateStandard("(nothing asked yet)");
            table.AddRow("Result (Value, or null when cancelled)", _inputResult);

            _inputLook = CreateFormChoice("Standard", "Primary", "colors: Yellow");
            table.AddRow("Colors (CreateInputStandard / CreateInputPrimary)", _inputLook);

            _inputSize = CreateFormChoice(SizeChoiceDefault, "Small", "Medium", "Large", SizeChoiceOwn);
            table.AddRow("size (width of the box)", _inputSize);

            _inputPassword = UIStyles.ToggleSwitches.CreateStandard(false);
            _inputStartText = UIStyles.ToggleSwitches.CreateStandard(false);
            _inputPlaceholder = UIStyles.ToggleSwitches.CreateStandard(false);
            _inputMaxLength = CreateFormChoice("0 (no limit)", "5", "10");
            table.AddRow("password", _inputPassword);
            table.AddRow("text (starts with a text, selected)", _inputStartText);
            table.AddRow("placeholder", _inputPlaceholder);
            table.AddRow("MaxLength", _inputMaxLength);

            _inputHint = UIStyles.ToggleSwitches.CreateStandard(false);
            _inputError = UIStyles.ToggleSwitches.CreateStandard(false);
            _inputValidator = UIStyles.ToggleSwitches.CreateStandard(false);
            _inputTrim = UIStyles.ToggleSwitches.CreateStandard(false);
            _inputAllowEmpty = UIStyles.ToggleSwitches.CreateStandard(false);
            table.AddRow("Hint (gray note under the prompt)", _inputHint);
            table.AddRow("ErrorText (red line, goes when typing)", _inputError);
            table.AddRow("Validator (the code has 5 digits)", _inputValidator);
            table.AddRow("TrimValue", _inputTrim);
            table.AddRow("AllowEmpty", _inputAllowEmpty);
        }

        private const string InputPrompt = "Please type the code that was sent to you.";
        private const string InputStartText = "12345";

        private static string CheckInputCode(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c))
                    return "Only digits, please.";
            }

            return text.Length == 5 ? null : "The code has 5 digits.";
        }

        // The factory way: every option of the form.
        private void ShowSampleInputForm()
        {
            string look = (string)_inputLook.SelectedItem;
            string placeholder = _inputPlaceholder.Checked ? "12345" : "";
            string text = _inputStartText.Checked ? InputStartText : "";

            using (InputBoxForm form = look == "Primary"
                ? UIStyles.Forms.CreateInputPrimary(InputPrompt, "Code", text, _inputPassword.Checked, placeholder, ParseSizeChoice(_inputSize))
                : UIStyles.Forms.CreateInputStandard(InputPrompt, "Code", text, _inputPassword.Checked, look == "colors: Yellow" ? UIStyles.Colors.Sets.Yellow : null, placeholder, ParseSizeChoice(_inputSize)))
            {
                if (_inputHint.Checked)
                    form.Hint = "It is the one in the message from the service.";

                if (_inputError.Checked)
                    form.ErrorText = "The last code was wrong.";

                if (_inputValidator.Checked)
                    form.Validator = CheckInputCode;

                form.TrimValue = _inputTrim.Checked;
                form.AllowEmpty = _inputAllowEmpty.Checked;
                form.MaxLength = int.Parse(((string)_inputMaxLength.SelectedItem).Split(' ')[0]);

                _inputResult.Text = form.ShowDialog(this) == DialogResult.OK ? "\"" + form.Value + "\"" : "null (cancelled)";
            }
        }

        // The one-line way: the same, without the colors.
        private void ShowSampleInputLine()
        {
            string value = InputBox.Show(
                InputPrompt,
                "Code",
                _inputStartText.Checked ? InputStartText : "",
                _inputPassword.Checked,
                this,
                _inputHint.Checked ? "It is the one in the message from the service." : null,
                _inputPlaceholder.Checked ? "12345" : "",
                int.Parse(((string)_inputMaxLength.SelectedItem).Split(' ')[0]),
                _inputValidator.Checked ? (Func<string, string>)CheckInputCode : null,
                _inputTrim.Checked,
                ParseSizeChoice(_inputSize));

            _inputResult.Text = value == null ? "null (cancelled)" : "\"" + value + "\"";
        }
    }
}
