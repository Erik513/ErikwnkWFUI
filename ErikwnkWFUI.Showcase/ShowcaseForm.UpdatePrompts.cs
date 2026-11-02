using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using Button = System.Windows.Forms.Button;
using ComboBox = System.Windows.Forms.ComboBox;
using Label = System.Windows.Forms.Label;

namespace ErikwnkWFUI.Showcase
{
    // The update prompt page of the forms tab: UpdatePrompt.ShowUpdateAvailableAsync with a made-up update that takes a few seconds. The rows set
    // what the question has and how the "download" ends; the line Result says how the whole thing ended.
    public partial class ShowcaseForm
    {
        private const string UpdateEndsApplied = "it succeeds (Applied)";
        private const string UpdateEndsFails = "it fails half way (Failed, the message stays)";
        private const string UpdateEndsAtOnce = "it is ready at once";

        private ComboBox _updateLook;
        private ComboBox _updateSize;
        private ComboBox _updateEnds;
        private ToggleSwitch _updateNotes;
        private ToggleSwitch _updateSkip;
        private ToggleSwitch _updateCancelable;
        private Label _updateResult;

        private void AddUpdatePromptSection(PropertyTable table)
        {
            table.AddSection("The sample update: about three seconds of \"download\"");

            Button show = UIStyles.Buttons.CreateStandard("ShowUpdateAvailableAsync", size: new Size(210, 32));
            show.Click += (sender, e) => ShowUpdateSample(show);
            table.AddRow("Update prompt", show);

            _updateResult = UIStyles.Labels.CreateStandard("(nothing shown yet)");
            table.AddRow("Result (UpdateOutcome)", _updateResult);

            _updateLook = CreateFormChoice("Standard", "colors: Yellow", "colors: Green");
            _updateSize = CreateFormChoice(SizeChoiceDefault, "Small", "Medium", "Large", SizeChoiceOwn);
            _updateEnds = CreateFormChoice(UpdateEndsApplied, UpdateEndsFails, UpdateEndsAtOnce);
            table.AddSection("UpdatePrompt.ShowUpdateAvailableAsync");
            table.AddRow("colors (the title bar of the question)", _updateLook);
            table.AddRow("size (FormSize or own Size)", _updateSize);
            table.AddRow("what the update does", _updateEnds);

            _updateNotes = UIStyles.ToggleSwitches.CreateStandard(true);
            _updateSkip = UIStyles.ToggleSwitches.CreateStandard(true);
            _updateCancelable = UIStyles.ToggleSwitches.CreateStandard(true);
            table.AddRow("releaseNotes (a What's new button)", _updateNotes);
            table.AddRow("allowSkip (Skip this version)", _updateSkip);
            table.AddRow("cancelable (Cancel while it downloads)", _updateCancelable);
        }

        private void ShowUpdateSample(Control button)
        {
            string ends = (string)_updateEnds.SelectedItem;
            string look = (string)_updateLook.SelectedItem;
            UIColorSet colors = look == "colors: Yellow" ? UIStyles.Colors.Sets.Yellow : look == "colors: Green" ? UIStyles.Colors.Sets.Green : null;

            // The question and then the progress are modal: this call comes back when both are closed.
            UpdateOutcome outcome = UpdatePrompt.ShowUpdateAvailableAsync(
                "1.2.0",
                "1.3.0",
                async (progress, token) =>
                {
                    if (ends == UpdateEndsAtOnce)
                        return true;

                    for (int percent = 0; percent <= 100; percent += 5)
                    {
                        token.ThrowIfCancellationRequested();
                        progress.Report(percent);

                        if (percent == 50 && ends == UpdateEndsFails)
                            throw new InvalidOperationException("The sample update was made to fail.");

                        await Task.Delay(150, token);
                    }

                    return true;
                },
                button.FindForm(),
                colors: colors,
                size: ParseSizeChoice(_updateSize),
                releaseNotes: _updateNotes.Checked ? "- Faster start of the program\n- Fixed a crash when the list is empty\n- New: radio buttons and group boxes" : null,
                allowSkip: _updateSkip.Checked,
                cancelable: _updateCancelable.Checked).Result;

            _updateResult.Text = outcome.ToString();
        }
    }
}