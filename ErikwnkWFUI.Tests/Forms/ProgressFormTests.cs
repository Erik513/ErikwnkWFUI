using System.Drawing;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using ProgressBar = ErikwnkWFUI.Controls.ProgressBar;

// A report from another thread is waited for on purpose in two tests (the point is that the call comes from a thread of its own).
#pragma warning disable xUnit1031

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The progress dialog: a text and a bar, filled by a task from any thread; the user cannot close it, but can cancel.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ProgressFormTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();

    public void Dispose()
    {
        _host.Dispose();
        UIStrings.Language = UILanguage.English;
        AppLocalization.Language = AppLanguage.English;
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static ProgressBar Bar(ProgressForm form)
    {
        return form.GetPrivateField<ProgressBar>("_bar")!;
    }

    private static Label StatusLabel(ProgressForm form)
    {
        return form.GetPrivateField<Label>("_statusLabel")!;
    }

    private static Button CancelButton(ProgressForm form)
    {
        return form.GetPrivateField<Button>("_cancelButton")!;
    }

    private static Size Scaled(Control control, Size size96)
    {
        return new Size(Dpi.Scale(size96.Width, control.DeviceDpi), Dpi.Scale(size96.Height, control.DeviceDpi));
    }

    // ---- the factories

    [Fact]
    public void TheFactories_MakeAFixedDialog_WithTheTitleOfTheLanguage_OrTheOneGiven()
    {
        ProgressForm standard = _host.Add(UIStyles.Forms.CreateProgressStandard("Loading"));
        ProgressForm primary = _host.Add(UIStyles.Forms.CreateProgressPrimary("Loading", "Update"));

        Assert.Equal("Please wait", standard.Text);
        Assert.Equal("Update", primary.Text);
        Assert.Equal(standard.Size, standard.MinimumSize);
        Assert.Equal(standard.Size, standard.MaximumSize);
        Assert.False(standard.TitleBar.ShowMaximizeButton);
        Assert.Equal("Loading", standard.Status);

        UIStrings.Language = UILanguage.German;
        Assert.Equal("Bitte warten", UIStyles.Forms.CreateProgressStandard().Text);
        Assert.Equal("Bitte warten", UIStyles.Forms.CreateProgressPrimary().Text);
    }

    [Fact]
    public void TheTitleBar_TakesAColorSet_OrTheAccentWithPrimary()
    {
        ProgressForm plain = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));
        ProgressForm yellow = _host.Show(UIStyles.Forms.CreateProgressStandard("x", colors: UIStyles.Colors.Sets.Yellow));
        ProgressForm primary = _host.Show(UIStyles.Forms.CreateProgressPrimary("x"));

        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), plain.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Yellow.Basic.ToArgb(), yellow.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), primary.TitleBar.BackColor.ToArgb());
    }

    [Fact]
    public void TheUserCannotCloseIt_ButTheProgramCan()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));

        Assert.False(form.CloseEnabled);
        form.InvokePrivate<bool>("ProcessDialogKey", Keys.Escape);
        Assert.False(form.IsDisposed);
        Assert.False(form.Disposing);

        form.Close();

        Assert.True(form.IsDisposed);
    }

    // ---- sizes

    [Fact]
    public void TheWidth_IsOneOfThree_AndAnOwnSizeIsTheWidthAndTheLeastHeight()
    {
        ProgressForm small = _host.Show(UIStyles.Forms.CreateProgressStandard("x", size: FormSize.Small));
        ProgressForm medium = _host.Show(UIStyles.Forms.CreateProgressStandard("x", size: FormSize.Medium));
        ProgressForm large = _host.Show(UIStyles.Forms.CreateProgressPrimary("x", size: FormSize.Large));
        ProgressForm plain = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));
        ProgressForm tall = _host.Show(UIStyles.Forms.CreateProgressStandard("x", size: new Size(500, 400)));
        ProgressForm low = _host.Show(UIStyles.Forms.CreateProgressStandard("x", size: new Size(500, 20)));

        Assert.True(small.Width < medium.Width && medium.Width < large.Width);
        Assert.Equal(medium.Size, plain.Size);
        Assert.Equal(Scaled(small, new Size(380, 0)).Width, small.ClientSize.Width);
        Assert.Equal(small.Height, medium.Height);
        Assert.Equal(Scaled(tall, new Size(500, 400)).Height, tall.ClientSize.Height);
        Assert.Equal(Scaled(tall, new Size(500, 0)).Width, tall.ClientSize.Width);
        Assert.Equal(medium.ClientSize.Height, low.ClientSize.Height);
    }

    [Fact]
    public void OnAnotherDpi_EverythingIsScaled()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x", cancelable: true));
        int width96 = form.ClientSize.Width;
        int bar96 = Bar(form).Height;

        form.DpiOverride = 192;

        Assert.Equal(width96 * 2, form.ClientSize.Width);
        Assert.Equal(bar96 * 2, Bar(form).Height);
    }

    // ---- the progress

    [Fact]
    public void ThePercentIsTheValueOfTheBar_CutToZeroAndOneHundred()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));

        form.Percent = 40;
        Assert.Equal(40, form.Percent);
        Assert.Equal(40, Bar(form).Value);

        form.Percent = 250;
        Assert.Equal(100, Bar(form).Value);

        form.Report(-5);
        Assert.Equal(0, Bar(form).Value);

        IProgress<int> progress = form;
        progress.Report(70);
        Assert.Equal(70, Bar(form).Value);
    }

    [Fact]
    public void ReportWithAText_SetsTheStatus_AndTheWindowGrowsToLongTexts()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("Short."));
        int before = form.Height;

        form.Report(30, string.Join(" ", Enumerable.Repeat("A status text that needs more than one line.", 5)));

        Assert.Equal(30, Bar(form).Value);
        Assert.True(form.Height > before);
        Assert.True(Bar(form).Top >= StatusLabel(form).Bottom);

        form.Status = "Short.";
        Assert.Equal(before, form.Height);

        form.Status = null;
        Assert.Equal("", form.Status);
        Assert.False(StatusLabel(form).Visible);
    }

    [Fact]
    public void AnIndeterminateBarMoves_HasNoPercentage_AndASetPercentTurnsItOff()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));

        form.IsIndeterminate = true;
        Assert.Equal(ProgressBarStyle.Marquee, Bar(form).Style);
        Assert.False(Bar(form).ShowPercentage);

        form.Percent = 20;
        Assert.False(form.IsIndeterminate);
        Assert.Equal(ProgressBarStyle.Continuous, Bar(form).Style);
        Assert.True(Bar(form).ShowPercentage);
        Assert.Equal(20, Bar(form).Value);

        form.ShowPercentage = false;
        Assert.False(Bar(form).ShowPercentage);
    }

    [Fact]
    public void ReportsFromAnotherThread_ReachTheBar()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));

        Task.Run(() => form.Report(65, "From a thread.")).Wait();
        TestPictures.Pump(200);

        Assert.Equal(65, Bar(form).Value);
        Assert.Equal("From a thread.", StatusLabel(form).Text);
    }

    [Fact]
    public void ReportsBeforeTheWindowIsShown_AreThereWhenItOpens_AndAfterItIsClosedTheyAreNoProblem()
    {
        ProgressForm form = UIStyles.Forms.CreateProgressStandard("x");
        form.Report(55, "Early.");
        Assert.Equal(55, form.Percent);

        _host.Show(form);

        Assert.Equal(55, Bar(form).Value);
        Assert.Equal("Early.", StatusLabel(form).Text);

        form.Close();
        Assert.Null(Record.Exception(() => Task.Run(() => form.Report(80, "Late.")).Wait()));
        Assert.Null(Record.Exception(() => form.Percent = 90));
    }

    // ---- cancel

    [Fact]
    public void WithoutCancelable_ThereIsNoCancelButton_AndEscapeDoesNothing()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));

        Assert.False(CancelButton(form).Visible);
        Assert.Null(form.CancelButton);
        Assert.False(form.IsCancelled);

        form.InvokePrivate<bool>("ProcessDialogKey", Keys.Escape);

        Assert.False(form.IsCancelled);
        Assert.False(form.IsDisposed);
    }

    [Fact]
    public void Cancel_AsksTheTaskToStop_Once_AndTheWindowStaysOpen()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x", cancelable: true));
        int raised = 0;
        form.CancelRequested += (sender, e) => raised++;

        Assert.True(CancelButton(form).Visible);
        Assert.False(form.CancellationToken.IsCancellationRequested);

        CancelButton(form).InvokePrivate("OnClick", EventArgs.Empty);
        CancelButton(form).InvokePrivate("OnClick", EventArgs.Empty);

        Assert.True(form.IsCancelled);
        Assert.True(form.CancellationToken.IsCancellationRequested);
        Assert.Equal(1, raised);
        Assert.False(CancelButton(form).Enabled);
        Assert.False(form.IsDisposed);
    }

    [Fact]
    public void EscapeCancels_WhenTheWindowIsCancelable_AndCancelableCanBeSwitchedWhileOpen()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));
        int before = form.Height;

        form.Cancelable = true;
        Assert.True(CancelButton(form).Visible);
        Assert.True(form.Height > before);

        Assert.True(form.InvokePrivate<bool>("ProcessDialogKey", Keys.Escape));
        Assert.True(form.IsCancelled);

        ProgressForm other = _host.Show(UIStyles.Forms.CreateProgressStandard("x", cancelable: true));
        other.Cancelable = false;
        Assert.False(CancelButton(other).Visible);
        Assert.Equal(before, other.Height);
    }

    [Fact]
    public void TheCancelButtonText_FollowsTheLanguage_ATextThatWasSetStays()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x", cancelable: true));
        Assert.Equal("Cancel", CancelButton(form).Text);

        UIStrings.Language = UILanguage.German;
        Assert.Equal("Abbrechen", CancelButton(form).Text);

        form.CancelButtonText = "Stop";
        UIStrings.Language = UILanguage.English;
        Assert.Equal("Stop", CancelButton(form).Text);

        form.CancelButtonText = null;
        Assert.Equal("Cancel", CancelButton(form).Text);
    }

    [Fact]
    public void ADisposedWindow_ListensToTheLanguageNoMore_AndDisposingTwiceIsNoProblem()
    {
        System.Reflection.FieldInfo? field = typeof(UIStrings).GetField("LanguageChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        int before = ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0;

        ProgressForm form = UIStyles.Forms.CreateProgressStandard("x", cancelable: true);
        form.Dispose();
        form.Dispose();

        Assert.Equal(before, ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0);
    }

    // ---- the end is final

    [Fact]
    public void OnceTheTaskHasEnded_LateReportsAndAnotherEndChangeNothing()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("Loading"));
        form.Succeed("Done.");

        form.Report(10, "Late.");
        form.Percent = 20;
        form.Status = "Later.";
        form.IsIndeterminate = true;
        form.Fail("Too late.");

        Assert.Equal(ProgressState.Succeeded, form.State);
        Assert.Equal(100, Bar(form).Value);
        Assert.Equal("Done.", StatusLabel(form).Text);
        Assert.Equal(ProgressBarStyle.Continuous, Bar(form).Style);
        Assert.Equal(UIStyles.Colors.Sets.Green.Basic.ToArgb(), Bar(form).ForeColor.ToArgb());

        ProgressForm failed = _host.Show(UIStyles.Forms.CreateProgressStandard("Loading"));
        failed.Percent = 35;
        failed.Fail("No.");
        failed.Succeed("Yes.");
        failed.Report(90);

        Assert.Equal(ProgressState.Failed, failed.State);
        Assert.Equal(35, Bar(failed).Value);
        Assert.Equal("No.", StatusLabel(failed).Text);
    }

    [Fact]
    public void ManyThreadsReportingAtOnce_NeverBreakTheWindow()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));

        Assert.Null(Record.Exception(() =>
        {
            Parallel.For(0, 400, i =>
            {
                form.Report(i % 120, "Step " + i);
                form.Percent = (i * 7) % 100;
            });
        }));

        TestPictures.Pump(300);

        Assert.InRange(Bar(form).Value, 0, 100);
        Assert.Equal(form.Percent, Bar(form).Value);
    }
    // ---- edge cases

    [Fact]
    public void AVeryLongStatus_NeverMakesTheWindowHigherThanTheScreen_AndTheBarAndTheButtonStayReachable()
    {
        int room = Screen.PrimaryScreen!.WorkingArea.Height;
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard(string.Join(" ", Enumerable.Repeat("A very long status text that goes on and on.", 400)), cancelable: true));

        Assert.True(form.Height <= room, "the window is " + form.Height + " high, the screen has " + room);
        Assert.True(form.ContentPanel.ClientRectangle.Contains(Bar(form).Bounds));
        Assert.True(form.ContentPanel.ClientRectangle.Contains(CancelButton(form).Bounds));
    }

    [Fact]
    public void NoStatusAtAll_AndNullOptions_AreNoProblem()
    {
        ProgressForm none = _host.Show(new ProgressForm(null, null!));

        Assert.Equal("", none.Status);
        Assert.Equal("Please wait", none.Text);
        Assert.False(StatusLabel(none).Visible);
        Assert.True(none.ContentPanel.ClientRectangle.Contains(Bar(none).Bounds));
    }

    // ---- ProgressBox.Run

    // Acts on the open progress dialog from a timer inside the message loop of ShowDialog.
    private static System.Windows.Forms.Timer ActOnOpenForm(Action<ProgressForm> act)
    {
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 30 };
        timer.Tick += (sender, e) =>
        {
            ProgressForm? open = Application.OpenForms.OfType<ProgressForm>().FirstOrDefault();

            if (open == null)
                return;

            timer.Stop();
            act(open);
        };
        timer.Start();

        return timer;
    }

    [Fact]
    public void Run_ShowsTheDialog_RunsTheTask_ReportsReachTheBar_AndClosesWhenTheTaskIsDone()
    {
        int? seenPercent = null;
        string? seenTitle = null;
        bool openWhileRunning = false;

        ProgressOutcome outcome = ProgressBox.Run(
            "Loading",
            async (progress, token) =>
            {
                progress.Report(60);
                await Task.Delay(150);
                ProgressForm? open = Application.OpenForms.OfType<ProgressForm>().FirstOrDefault();
                openWhileRunning = open != null;
                seenPercent = open?.Percent;
                seenTitle = open?.Text;
            },
            "Please wait...");

        Assert.Equal(ProgressOutcome.Completed, outcome);
        Assert.True(openWhileRunning);
        Assert.Equal(60, seenPercent);
        Assert.Equal("Loading", seenTitle);
        Assert.Empty(Application.OpenForms.OfType<ProgressForm>());
    }

    [Fact]
    public void Run_ACancelledTask_GivesCancelled_WhenTheUserClickedCancel()
    {
        using System.Windows.Forms.Timer timer = ActOnOpenForm(form => CancelButton(form).InvokePrivate("OnClick", EventArgs.Empty));

        ProgressOutcome outcome = ProgressBox.Run(
            "Loading",
            async (progress, token) =>
            {
                for (int i = 0; i < 100; i++)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(40, token);
                }
            },
            cancelable: true);

        Assert.Equal(ProgressOutcome.Cancelled, outcome);
    }

    [Fact]
    public void Run_AnExceptionOfTheTask_IsThrownAfterTheWindowHasClosed()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            ProgressBox.Run("Loading", (progress, token) => throw new InvalidOperationException("The task failed.")));

        Assert.Equal("The task failed.", error.Message);
        Assert.Empty(Application.OpenForms.OfType<ProgressForm>());
    }

    [Fact]
    public void Run_NeedsATask_AndATaskThatEndsAtOnceIsNoProblem()
    {
        Assert.Throws<ArgumentNullException>(() => ProgressBox.Run("x", null!));
        Assert.Equal(ProgressOutcome.Completed, ProgressBox.Run("x", (progress, token) => Task.CompletedTask));
        Assert.Equal(ProgressOutcome.Completed, ProgressBox.Run(null, (progress, token) => Task.CompletedTask, size: FormSize.Small));
    }

    [Fact]
    public void Run_TakesTheSizeAndTheOwner()
    {
        AppForm owner = _host.Show(UIStyles.Forms.CreateStandard("Main"));
        owner.Bounds = new Rectangle(100, 100, 700, 500);
        Rectangle? seen = null;

        ProgressBox.Run(
            "Loading",
            async (progress, token) =>
            {
                await Task.Delay(100);
                seen = Application.OpenForms.OfType<ProgressForm>().FirstOrDefault()?.Bounds;
            },
            owner: owner,
            size: FormSize.Large);

        Assert.NotNull(seen);
        Assert.True(owner.Bounds.Contains(seen!.Value), "the dialog " + seen + " is not over the window " + owner.Bounds);
        Assert.Equal(Scaled(owner, new Size(640, 0)).Width, seen.Value.Width);
    }

    // ---- the time

    private static Label TimeLabel(ProgressForm form)
    {
        return form.GetPrivateField<Label>("_timeLabel")!;
    }

    [Fact]
    public void ADurationIsWrittenAsMinutesAndSeconds_AndWithHoursFromAnHourOn()
    {
        Assert.Equal("0:05", ProgressForm.FormatDuration(TimeSpan.FromSeconds(5)));
        Assert.Equal("1:05", ProgressForm.FormatDuration(TimeSpan.FromSeconds(65)));
        Assert.Equal("59:59", ProgressForm.FormatDuration(TimeSpan.FromSeconds(3599)));
        Assert.Equal("1:02:03", ProgressForm.FormatDuration(new TimeSpan(1, 2, 3)));
        Assert.Equal("26:00:00", ProgressForm.FormatDuration(TimeSpan.FromHours(26)));
        Assert.Equal("0:00", ProgressForm.FormatDuration(TimeSpan.FromSeconds(-4)));
    }

    [Fact]
    public void TheTimeLeftIsAnEstimate_OnlyWhenThePercentIsKnown_AndTheTaskHasRunForAMoment()
    {
        TimeSpan ten = TimeSpan.FromSeconds(10);

        Assert.Equal("0:10 elapsed \u00B7 about 0:10 left", ProgressForm.FormatTime(ten, 50, false, ProgressState.Running));
        Assert.Equal("0:10 elapsed \u00B7 about 0:30 left", ProgressForm.FormatTime(ten, 25, false, ProgressState.Running));

        // Too early, no progress yet, nothing left, a bar without an end, or already ended: only what has passed.
        Assert.Equal("0:01 elapsed", ProgressForm.FormatTime(TimeSpan.FromSeconds(1), 50, false, ProgressState.Running));
        Assert.Equal("0:10 elapsed", ProgressForm.FormatTime(ten, 0, false, ProgressState.Running));
        Assert.Equal("0:10 elapsed", ProgressForm.FormatTime(ten, 100, false, ProgressState.Running));
        Assert.Equal("0:10 elapsed", ProgressForm.FormatTime(ten, 50, true, ProgressState.Running));
        Assert.Equal("0:10 elapsed", ProgressForm.FormatTime(ten, 50, false, ProgressState.Succeeded));
        Assert.Equal("0:10 elapsed", ProgressForm.FormatTime(ten, 50, false, ProgressState.Failed));

        UIStrings.Language = UILanguage.German;
        Assert.Equal("0:10 vergangen \u00B7 noch ca. 0:10", ProgressForm.FormatTime(ten, 50, false, ProgressState.Running));
        Assert.Equal("0:10 vergangen", ProgressForm.FormatTime(ten, 0, false, ProgressState.Running));
    }

    [Fact]
    public void ShowTime_AddsAGrayLineUnderTheBar_AndTheWindowGrows()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));
        int before = form.Height;
        Assert.False(TimeLabel(form).Visible);
        Assert.False(form.ShowTime);

        form.Clock = () => TimeSpan.FromSeconds(12);
        form.ShowTime = true;
        form.Percent = 25;

        Assert.True(TimeLabel(form).Visible);
        Assert.Equal("0:12 elapsed \u00B7 about 0:36 left", TimeLabel(form).Text);
        Assert.True(TimeLabel(form).Top >= Bar(form).Bottom);
        Assert.True(form.Height > before);

        form.ShowTime = false;
        Assert.False(TimeLabel(form).Visible);
        Assert.Equal(before, form.Height);
    }

    [Fact]
    public void TheTimeLineChangesWithThePercent_AndTheLanguage()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));
        form.Clock = () => TimeSpan.FromSeconds(20);
        form.ShowTime = true;

        form.Percent = 50;
        Assert.Equal("0:20 elapsed \u00B7 about 0:20 left", TimeLabel(form).Text);

        form.Percent = 80;
        Assert.Equal("0:20 elapsed \u00B7 about 0:05 left", TimeLabel(form).Text);

        form.IsIndeterminate = true;
        Assert.Equal("0:20 elapsed", TimeLabel(form).Text);
    }

    [Fact]
    public void TheClockRunsWhileTheTimeIsWantedAndTheTaskRuns_AndStopsWhenItEnds()
    {
        System.Windows.Forms.Timer Timer(ProgressForm f) => f.GetPrivateField<System.Windows.Forms.Timer>("_timer")!;
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));
        Assert.False(Timer(form).Enabled);

        form.ShowTime = true;
        Assert.True(Timer(form).Enabled);

        form.Succeed("Done.");
        Assert.False(Timer(form).Enabled);

        ProgressForm other = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));
        other.ShowTime = true;
        other.Fail("Failed.");
        Assert.False(Timer(other).Enabled);

        ProgressForm never = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));
        never.ShowTime = true;
        never.ShowTime = false;
        Assert.False(Timer(never).Enabled);
    }

    // ---- Succeed and Fail

    [Fact]
    public void Succeed_FillsTheBar_MakesItGreen_SetsTheText_AndAllowsTheUserToClose()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("Loading"));
        int before = form.Height;
        form.Percent = 30;
        Assert.False(form.CloseEnabled);
        Assert.Equal(ProgressState.Running, form.State);

        form.Succeed("All done.");

        Assert.Equal(ProgressState.Succeeded, form.State);
        Assert.Equal(100, Bar(form).Value);
        Assert.Equal(UIStyles.Colors.Sets.Green.Basic.ToArgb(), Bar(form).ForeColor.ToArgb());
        Assert.Equal("All done.", StatusLabel(form).Text);
        Assert.True(form.CloseEnabled);
        Assert.True(CancelButton(form).Visible);
        Assert.Equal("Close", CancelButton(form).Text);
        Assert.True(form.Height > before, "room for the button");
    }

    [Fact]
    public void Fail_KeepsTheBarWhereItWas_MakesItRed_AndShowsWhatWentWrong()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("Loading"));
        form.Percent = 40;

        form.Fail("The server did not answer.");

        Assert.Equal(ProgressState.Failed, form.State);
        Assert.Equal(40, Bar(form).Value);
        Assert.Equal(UIStyles.Colors.Sets.Red.Basic.ToArgb(), Bar(form).ForeColor.ToArgb());
        Assert.Equal("The server did not answer.", StatusLabel(form).Text);
        Assert.True(form.CloseEnabled);
        Assert.Equal("Close", CancelButton(form).Text);
    }

    [Fact]
    public void WithoutAText_TheTextStaysAsItWas()
    {
        ProgressForm succeeded = _host.Show(UIStyles.Forms.CreateProgressStandard("Loading"));
        succeeded.Succeed();
        Assert.Equal("Loading", StatusLabel(succeeded).Text);

        ProgressForm failed = _host.Show(UIStyles.Forms.CreateProgressStandard("Loading"));
        failed.Fail();
        Assert.Equal("Loading", StatusLabel(failed).Text);
    }

    [Fact]
    public void TheButtonClosesTheWindowOnceTheTaskHasEnded_AndEscapeDoesToo()
    {
        ProgressForm byButton = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));
        byButton.Succeed();
        CancelButton(byButton).InvokePrivate("OnClick", EventArgs.Empty);
        Assert.True(byButton.IsDisposed);
        Assert.False(byButton.IsCancelled);

        ProgressForm byEscape = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));
        byEscape.Fail("No.");
        Assert.True(byEscape.InvokePrivate<bool>("ProcessDialogKey", Keys.Escape));
        Assert.True(byEscape.IsDisposed);
    }

    [Fact]
    public void ACancelledWindowThatEnds_GetsItsButtonBackAsClose()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x", cancelable: true));
        CancelButton(form).InvokePrivate("OnClick", EventArgs.Empty);
        Assert.False(CancelButton(form).Enabled);

        form.Fail("Stopped.");

        Assert.True(CancelButton(form).Enabled);
        Assert.Equal("Close", CancelButton(form).Text);
    }

    [Fact]
    public void CloseTextFollowsTheLanguage_AndSucceedFromAnotherThreadOrBeforeTheWindowIsShownWorks()
    {
        ProgressForm form = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));
        form.Succeed();
        UIStrings.Language = UILanguage.German;
        Assert.Equal("Schlie\u00DFen", CancelButton(form).Text);

        UIStrings.Language = UILanguage.English;
        ProgressForm thread = _host.Show(UIStyles.Forms.CreateProgressStandard("x"));
        Task.Run(() => thread.Fail("From a thread.")).Wait();
        TestPictures.Pump(200);
        Assert.Equal(ProgressState.Failed, thread.State);
        Assert.Equal("From a thread.", StatusLabel(thread).Text);
        Assert.True(thread.CloseEnabled);

        ProgressForm early = UIStyles.Forms.CreateProgressStandard("x");
        early.Succeed("Early.");
        _host.Show(early);
        Assert.Equal(100, Bar(early).Value);
        Assert.Equal("Early.", StatusLabel(early).Text);
        Assert.True(early.CloseEnabled);
    }

    [Fact]
    public void Run_WithShowResult_LeavesTheMessageInTheWindowUntilTheUserClosesIt_AndThenThrows()
    {
        ProgressState? state = null;
        string? message = null;
        bool? closeEnabled = null;

        using System.Windows.Forms.Timer timer = ActOnOpenForm(form =>
        {
            // The task has failed by now: the window says so and stays.
            System.Windows.Forms.Timer wait = new System.Windows.Forms.Timer { Interval = 100 };
            wait.Tick += (sender, e) =>
            {
                if (form.State != ProgressState.Failed)
                    return;

                wait.Stop();
                state = form.State;
                message = StatusLabel(form).Text;
                closeEnabled = form.CloseEnabled;
                CancelButton(form).InvokePrivate("OnClick", EventArgs.Empty);
            };
            wait.Start();
        });

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            ProgressBox.Run("Loading", (progress, token) => throw new InvalidOperationException("The task failed."), showResult: true));

        Assert.Equal("The task failed.", error.Message);
        Assert.Equal(ProgressState.Failed, state);
        Assert.Equal("The task failed.", message);
        Assert.True(closeEnabled);
    }
    // ---- both themes

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void EverythingCanBeReadAndSeen_InBothThemes(bool lightTheme)
    {
        ColorContrastTestSupport.Run(lightTheme, shortfalls =>
        {
            foreach (bool primary in new[] { false, true })
            {
                ProgressForm form = _host.Show(primary ? UIStyles.Forms.CreateProgressPrimary("Loading the update", cancelable: true) : UIStyles.Forms.CreateProgressStandard("Loading the update", cancelable: true));
                form.Percent = 45;
                form.ShowTime = true;
                string window = primary ? "primary" : "standard";
                Label status = StatusLabel(form);
                Color back = status.BackColor.A == 0 ? form.ContentPanel.BackColor : status.BackColor;

                ColorContrastTestSupport.Check(shortfalls, window + ", status", status.ForeColor, back, 4.5);
                ColorContrastTestSupport.Check(shortfalls, window + ", Cancel", CancelButton(form).ForeColor, CancelButton(form).BackColor, 4.5);
                ColorContrastTestSupport.Check(shortfalls, window + ", the bar against the window", Bar(form).ForeColor, form.ContentPanel.BackColor, 1.5);
                ColorContrastTestSupport.Check(shortfalls, window + ", the time", TimeLabel(form).ForeColor, back, 3.0);

                form.Succeed("Done.");
                ColorContrastTestSupport.Check(shortfalls, window + ", the green bar against the window", Bar(form).ForeColor, form.ContentPanel.BackColor, 1.5);
                form.Fail("Failed.");
                ColorContrastTestSupport.Check(shortfalls, window + ", the red bar against the window", Bar(form).ForeColor, form.ContentPanel.BackColor, 1.5);
                ColorContrastTestSupport.Check(shortfalls, window + ", Close", CancelButton(form).ForeColor, CancelButton(form).BackColor, 4.5);
            }
        });
    }
}
