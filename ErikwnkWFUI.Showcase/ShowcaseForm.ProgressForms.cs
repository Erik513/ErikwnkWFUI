using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using Button = System.Windows.Forms.Button;
using ComboBox = System.Windows.Forms.ComboBox;
using Label = System.Windows.Forms.Label;

namespace ErikwnkWFUI.Showcase
{
    // The ProgressForm page of the forms tab: one sample task that runs for about ten seconds - once in a window the page owns (Open), once
    // through ProgressBox.Run (modal, like the box of Windows). The rows set what the window has and how the task ends.
    public partial class ShowcaseForm
    {
        private const string ProgressEndsClose = "the window closes at once";
        private const string ProgressEndsSucceed = "Succeed (green, Close button)";
        private const string ProgressEndsFail = "Fail (red, message stays)";

        private ComboBox _progressLook;
        private ComboBox _progressSize;
        private ComboBox _progressEnds;
        private ToggleSwitch _progressCancelable;
        private ToggleSwitch _progressTime;
        private ToggleSwitch _progressIndeterminate;
        private ToggleSwitch _progressPercentage;
        private ToggleSwitch _progressShowResult;
        private Label _progressResult;
        private ProgressForm _progressSample;
        private System.Windows.Forms.Timer _progressTimer;

        private void AddProgressFormSection(PropertyTable table)
        {
            CloseProgressSample();

            table.AddSection("The sample task: about ten seconds of work");

            Button open = UIStyles.Buttons.CreateStandard("Open", size: new Size(110, 32));
            Button run = UIStyles.Buttons.CreateStandard("ProgressBox.Run", size: new Size(150, 32));
            open.Click += (sender, e) => OpenProgressSample();
            run.Click += (sender, e) => RunProgressSample();
            table.AddRow("Progress dialog", open, run);

            _progressResult = UIStyles.Labels.CreateStandard("(nothing run yet)");
            table.AddRow("Result (how the task ended)", _progressResult);

            _progressLook = CreateFormChoice("Standard", "Primary", "colors: Yellow");
            _progressSize = CreateFormChoice(SizeChoiceDefault, "Small", "Medium", "Large", SizeChoiceOwn);
            _progressEnds = CreateFormChoice(ProgressEndsClose, ProgressEndsSucceed, ProgressEndsFail);
            table.AddSection("UIStyles.Forms.CreateProgressStandard / CreateProgressPrimary");
            table.AddRow("Colors (the Open button; Run has the standard ones)", _progressLook);
            table.AddRow("size (width of the window)", _progressSize);
            table.AddRow("how the task ends", _progressEnds);

            _progressCancelable = UIStyles.ToggleSwitches.CreateStandard(true);
            _progressTime = UIStyles.ToggleSwitches.CreateStandard(true);
            _progressIndeterminate = UIStyles.ToggleSwitches.CreateStandard(false);
            _progressPercentage = UIStyles.ToggleSwitches.CreateStandard(true);
            table.AddSection("ProgressForm");
            table.AddRow("Cancelable (Cancel button, Escape)", _progressCancelable);
            table.AddRow("ShowTime (elapsed, time left)", _progressTime);
            table.AddRow("IsIndeterminate (the bar just moves)", _progressIndeterminate);
            table.AddRow("ShowPercentage", _progressPercentage);

            _progressShowResult = UIStyles.ToggleSwitches.CreateStandard(true);
            table.AddSection("ProgressBox.Run");
            table.AddRow("showResult (a failure stays in the window)", _progressShowResult);
        }

        private FormSizeOption? GetProgressSize()
        {
            return ParseSizeChoice(_progressSize);
        }

        private ProgressForm CreateProgressSampleForm()
        {
            string look = (string)_progressLook.SelectedItem;
            bool cancelable = _progressCancelable.Checked;
            FormSizeOption? size = GetProgressSize();
            ProgressForm form = look == "Primary"
                ? UIStyles.Forms.CreateProgressPrimary("Loading the sample data ...", "Sample task", cancelable, size)
                : UIStyles.Forms.CreateProgressStandard("Loading the sample data ...", "Sample task", cancelable, look == "colors: Yellow" ? UIStyles.Colors.Sets.Yellow : null, size);

            form.ShowTime = _progressTime.Checked;
            form.ShowPercentage = _progressPercentage.Checked;
            form.IsIndeterminate = _progressIndeterminate.Checked;

            return form;
        }

        // ---- Open: a window of the page, driven by a timer (the task runs on the thread of the window here, to keep the sample short)

        private void OpenProgressSample()
        {
            CloseProgressSample();

            ProgressForm form = CreateProgressSampleForm();
            int step = 0;
            string ends = (string)_progressEnds.SelectedItem;
            bool moving = _progressIndeterminate.Checked;

            _progressTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _progressTimer.Tick += (sender, e) =>
            {
                step++;

                if (!moving)
                    form.Report(step, "Loading the sample data: step " + step + " of 100");

                if (step < 100)
                    return;

                _progressTimer.Stop();
                EndProgressSample(form, ends);
            };

            form.CancelRequested += (sender, e) =>
            {
                _progressTimer.Stop();
                _progressResult.Text = "cancelled by the user (IsCancelled = true)";
                form.Close();
            };

            form.FormClosed += (sender, e) =>
            {
                if (_progressTimer != null)
                    _progressTimer.Stop();

                if (ReferenceEquals(_progressSample, form))
                    _progressSample = null;
            };

            _progressSample = form;
            _progressResult.Text = "running ...";
            form.Show(this);
            _progressTimer.Start();
            Activate();
        }

        private void EndProgressSample(ProgressForm form, string ends)
        {
            if (ends == ProgressEndsSucceed)
            {
                form.Succeed("The sample data is loaded.");
                _progressResult.Text = "Succeed: the user closes the window with the button";
            }
            else if (ends == ProgressEndsFail)
            {
                form.Fail("The sample server did not answer.");
                _progressResult.Text = "Fail: the message stays until the user closes the window";
            }
            else
            {
                _progressResult.Text = "completed: the program closed the window";
                form.Close();
            }
        }

        private void CloseProgressSample()
        {
            if (_progressTimer != null)
            {
                _progressTimer.Stop();
                _progressTimer.Dispose();
                _progressTimer = null;
            }

            if (_progressSample != null && !_progressSample.IsDisposed)
                _progressSample.Close();

            _progressSample = null;
        }

        // ---- ProgressBox.Run: modal, the task on another thread

        private void RunProgressSample()
        {
            CloseProgressSample();

            string ends = (string)_progressEnds.SelectedItem;
            bool moving = _progressIndeterminate.Checked;
            _progressResult.Text = "running ...";

            try
            {
                ProgressOutcome outcome = ProgressBox.Run(
                    "Sample task",
                    async (progress, token) =>
                    {
                        for (int step = 1; step <= 100; step++)
                        {
                            token.ThrowIfCancellationRequested();
                            await Task.Delay(100, token);

                            if (!moving)
                                progress.Report(step);
                        }

                        if (ends == ProgressEndsFail)
                            throw new InvalidOperationException("The sample server did not answer.");
                    },
                    "Loading the sample data ...",
                    _progressCancelable.Checked,
                    this,
                    GetProgressSize(),
                    _progressShowResult.Checked,
                    form =>
                    {
                        form.ShowTime = _progressTime.Checked;
                        form.ShowPercentage = _progressPercentage.Checked;
                        form.IsIndeterminate = moving;
                    });

                _progressResult.Text = outcome == ProgressOutcome.Cancelled ? "Cancelled: the task ended with an OperationCanceledException" : "Completed";
            }
            catch (InvalidOperationException exception)
            {
                _progressResult.Text = "Exception thrown by Run: " + exception.Message;
            }
        }
    }
}
