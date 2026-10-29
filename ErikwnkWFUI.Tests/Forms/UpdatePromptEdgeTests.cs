using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>
/// The edge cases of the update prompt: what is passed on to the dialog, the text of the version line, the release notes and the skip
/// button when they are changed afterwards, an update that reports strange values or ends in a cancellation by itself.
/// [Collection]: the title bar reads the live accent.
/// </summary>
// The tests wait for ShowUpdateAvailableAsync with .Result on purpose: the dialog is modal on the test thread (the timers of Act work
// inside its message loop), and awaiting would resume somewhere else.
#pragma warning disable xUnit1031
[Collection(AccentColorTestCollection.Name)]
public class UpdatePromptEdgeTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();

    public void Dispose()
    {
        _host.Dispose();
        UIStrings.Language = UILanguage.English;
    }

    private static System.Windows.Forms.Timer Act(Action<UpdateAvailableForm> onPrompt, Action<ProgressForm>? onProgress = null)
    {
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 30 };
        timer.Tick += (sender, e) =>
        {
            UpdateAvailableForm? prompt = Application.OpenForms.OfType<UpdateAvailableForm>().FirstOrDefault();

            if (prompt != null)
            {
                onPrompt(prompt);

                return;
            }

            ProgressForm? progress = Application.OpenForms.OfType<ProgressForm>().FirstOrDefault();

            if (progress != null)
                onProgress?.Invoke(progress);
        };
        timer.Start();

        return timer;
    }

    private static IEnumerable<Control> All(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;

            foreach (Control deeper in All(child))
                yield return deeper;
        }
    }

    private static string VersionLine(Form form)
    {
        return All(form).OfType<Label>().First(label => label.Text.Contains("→")).Text;
    }

    // ---- the arguments ----

    [Fact]
    public void WithoutAnUpdateFunction_BothOverloadsThrow()
    {
        // (The check comes before any window or task: it throws at the call.)
        Assert.Throws<ArgumentNullException>(() => { _ = UpdatePrompt.ShowUpdateAvailableAsync("1", "2", (Func<IProgress<int>, Task<bool>>)null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = UpdatePrompt.ShowUpdateAvailableAsync("1", "2", (Func<IProgress<int>, CancellationToken, Task<bool>>)null!); });
    }

    [Fact]
    public void WhatIsGiven_ReachesTheDialog_NotesSkipColorsSizeAndTheOwner()
    {
        using Form owner = _host.Show(new Form { Text = "Owner" });
        UpdateAvailableForm? seen = null;
        string? notes = null;
        bool? skip = null;
        Color? titleColor = null;
        Size? size = null;
        Form? seenOwner = null;

        using (System.Windows.Forms.Timer timer = Act(prompt =>
        {
            seen = prompt;
            notes = prompt.ReleaseNotes;
            skip = prompt.AllowSkip;
            titleColor = prompt.TitleBar.BackColor;
            size = prompt.Size;
            seenOwner = prompt.Owner;
            prompt.CancelButton!.PerformClick();
        }))
        {
            UpdatePrompt.ShowUpdateAvailableAsync(
                "1.0.0",
                "1.1.0",
                progress => Task.FromResult(true),
                owner,
                colors: UIStyles.Colors.Sets.Yellow,
                size: new Size(520, 280),
                releaseNotes: "- a fix",
                allowSkip: true).Wait();
        }

        Assert.NotNull(seen);
        Assert.Equal("- a fix", notes);
        Assert.True(skip);
        Assert.Same(owner, seenOwner);
        Assert.Equal(520, size!.Value.Width);
        Assert.NotEqual(UIStyles.Forms.CreateUpdateAvailableStandard("1", "2").TitleBar.BackColor.ToArgb(), titleColor!.Value.ToArgb());
    }

    // ---- the version line ----

    [Theory]
    [InlineData("1.2.0", "1.3.0", "v1.2.0  →  v1.3.0")]
    [InlineData("v1.2.0", "V1.3.0", "v1.2.0  →  v1.3.0")]
    [InlineData(null, null, "v  →  v")]
    [InlineData("", "", "v  →  v")]
    public void TheVersionLine_ShowsOneV_AndNullsAreEmptyText(string? current, string? latest, string expected)
    {
        UpdateAvailableForm form = _host.Show(new UpdateAvailableForm(current!, latest!));

        Assert.Equal(expected, VersionLine(form));
    }

    // ---- the release notes and the skip button, changed afterwards ----

    [Fact]
    public void TheNotes_AreTrimmed_AndTheirLineBreaksAreThoseOfAText()
    {
        UpdateAvailableForm form = _host.Show(new UpdateAvailableForm("1", "2"));
        TextBox box = All(form).OfType<TextBox>().First();

        form.ReleaseNotes = "  first\nsecond\r\nthird  \n";
        Assert.Equal("first\r\nsecond\r\nthird", box.Text);
        Assert.Equal("first\nsecond\r\nthird", form.ReleaseNotes);

        form.ReleaseNotes = null!;
        Assert.Equal("", form.ReleaseNotes);
        Assert.Equal("", box.Text);
    }

    [Fact]
    public void NotesThatAreOpen_CloseWhenTheTextGoes_AndCannotBeOpenedWithoutText()
    {
        UpdateAvailableForm form = _host.Show(new UpdateAvailableForm("1", "2"));

        form.NotesExpanded = true;
        Assert.False(form.NotesExpanded);

        form.ReleaseNotes = "text";
        form.NotesExpanded = true;
        Assert.True(form.NotesExpanded);
        int open = form.Height;

        form.ReleaseNotes = "";
        Assert.False(form.NotesExpanded);
        Assert.True(form.Height < open);
    }

    [Fact]
    public void Skip_CanBeSwitchedOnAndOffAfterwards_AndTheWindowIsWideEnoughEachTime()
    {
        UpdateAvailableForm form = _host.Show(new UpdateAvailableForm("1", "2"));
        Button skip = (Button)All(form).First(control => control is Button button && button.Text == UIStrings.Get("Update.Skip"));
        int plain = form.Width;

        form.AllowSkip = true;
        Assert.True(skip.Visible);
        Assert.True(form.Width >= plain);

        form.AllowSkip = false;
        Assert.False(skip.Visible);
        Assert.Equal(plain, form.Width);
    }

    [Fact]
    public void AnOwnerThatIsGone_IsLeftOut_TheWindowsStandInTheMiddleOfTheScreen()
    {
        Form owner = new Form();
        owner.Dispose();
        UpdateOutcome outcome;

        using (System.Windows.Forms.Timer timer = Act(prompt => prompt.CancelButton!.PerformClick()))
            outcome = UpdatePrompt.ShowUpdateAvailableAsync("1", "2", progress => Task.FromResult(true), owner).Result;

        Assert.Equal(UpdateOutcome.Declined, outcome);
    }

    [Fact]
    public void AfterAnUpdateIsInPlace_AnotherClickOnTheUpdateButtonShowsNothingAndSaysApplied()
    {
        using System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
        AppUpdater updater = new AppUpdater("owner", "repo", client, client);
        typeof(AppUpdater).GetField("_updateApplied", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(updater, true);

        // (No dialog is opened: the call would not come back from a modal window that nobody closes.)
        UpdateOutcome outcome = updater.ShowUpdatePromptAsync(new ErikwnkCore.Updater.UpdateCheckResult(new Version(2, 0, 0), "https://example.invalid/app.zip", null), new Version(1, 0, 0), null).Result;

        Assert.Equal(UpdateOutcome.Applied, outcome);
        Assert.Empty(Application.OpenForms.OfType<UpdateAvailableForm>());
    }

    // ---- the progress window ----

    [Fact]
    public void TheDownloadBar_IsGreen_LikeTheUpdateNowButton()
    {
        Color? bar = null;

        using (System.Windows.Forms.Timer timer = Act(
            prompt => prompt.AcceptButton!.PerformClick(),
            progress => bar ??= progress.BarColors != null ? progress.GetPrivateField<ErikwnkWFUI.Controls.ProgressBar>("_bar")!.ForeColor : (Color?)null))
        {
            UpdatePrompt.ShowUpdateAvailableAsync("1", "2", async progress => { await Task.Delay(300); return true; }).Wait();
        }

        Assert.NotNull(bar);
        Assert.Equal(UIStyles.ProgressBars.CreateGreen().ForeColor.ToArgb(), bar!.Value.ToArgb());
    }

    // ---- the keyboard ----

    [Fact]
    public void TheKeyboardStartsOnUpdateNow_AndTheNotesAreNoTabStopWhileTheyAreClosed()
    {
        UpdateAvailableForm form = _host.Show(new UpdateAvailableForm("1", "2"));
        form.ReleaseNotes = "text";
        TextBox notes = All(form).OfType<TextBox>().First();

        Assert.Same(form.AcceptButton, form.ActiveControl);
        Assert.False(notes.Visible);

        form.NotesExpanded = true;
        Assert.True(notes.Visible);

        form.NotesExpanded = false;
        Assert.False(notes.Visible);
        Assert.Same(form.AcceptButton, form.ActiveControl);
    }

    // ---- the release notes box: its frame and its scroll bar ----

    private static string Lines(int count)
    {
        return string.Join("\n", Enumerable.Range(1, count).Select(number => "Line " + number));
    }

    [Fact]
    public void TheNotesBox_HasNoFrameOfItsOwn_TheFrameIsThePanelAroundIt_AndItFollowsTheFocus()
    {
        UpdateAvailableForm form = _host.Show(new UpdateAvailableForm("1", "2"));
        TextBox notes = All(form).OfType<TextBox>().First();
        Control frame = notes.Parent!;

        // (No frame of the box: the system's frame is painted in the area of the scroll bar, which overpaints it when the mouse is over the bar.)
        Assert.Equal(BorderStyle.None, notes.BorderStyle);

        Color quiet = frame.BackColor;
        notes.InvokePrivate("OnEnter", EventArgs.Empty);
        Color focused = frame.BackColor;
        notes.InvokePrivate("OnLeave", EventArgs.Empty);

        Assert.NotEqual(quiet.ToArgb(), focused.ToArgb());
        Assert.Equal(quiet.ToArgb(), frame.BackColor.ToArgb());
    }

    [Fact]
    public void TheNotesFrame_FollowsTheTheme_AlsoWhileTheNotesAreClosed()
    {
        UIColors.ApplyTheme(UIThemes.Dark);

        try
        {
            UpdateAvailableForm form = _host.Show(new UpdateAvailableForm("1", "2"));
            form.ReleaseNotes = "text";
            Control frame = All(form).OfType<TextBox>().First().Parent!;

            // (The quiet outline is a mid gray that is seen on both pages: 3:1, worked out for the page behind it.)
            form.NotesExpanded = true;
            Assert.Equal(UIColors.GetOutlineColor(UIColors.BackgroundMedium).ToArgb(), frame.BackColor.ToArgb());

            // Changed while the notes are open, and while they are closed (the frame has no window then and hears nothing).
            form.NotesExpanded = false;
            UIColors.ApplyTheme(UIThemes.Light);
            Application.DoEvents();
            form.NotesExpanded = true;

            Assert.Equal(UIColors.GetOutlineColor(UIColors.BackgroundMedium).ToArgb(), frame.BackColor.ToArgb());
            Assert.True(ColorContrastHelper.GetContrastRatio(frame.BackColor, UIColors.BackgroundMedium) >= 3.0);

            UIColors.ApplyTheme(UIThemes.Dark);
            Application.DoEvents();

            Assert.Equal(UIColors.GetOutlineColor(UIColors.BackgroundMedium).ToArgb(), frame.BackColor.ToArgb());
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }
    [Fact]
    public void TheScrollBar_IsThereOnlyWhenTheTextDoesNotFit()
    {
        UpdateAvailableForm form = _host.Show(new UpdateAvailableForm("1", "2"));
        TextBox notes = All(form).OfType<TextBox>().First();

        form.ReleaseNotes = Lines(2);
        form.NotesExpanded = true;
        Assert.Equal(ScrollBars.None, notes.ScrollBars);

        form.ReleaseNotes = Lines(60);
        Assert.Equal(ScrollBars.Vertical, notes.ScrollBars);

        form.ReleaseNotes = Lines(3);
        Assert.Equal(ScrollBars.None, notes.ScrollBars);
    }

    [Fact]
    public void ALongLineThatWraps_AlsoNeedsTheScrollBar()
    {
        UpdateAvailableForm form = _host.Show(new UpdateAvailableForm("1", "2"));
        TextBox notes = All(form).OfType<TextBox>().First();

        form.ReleaseNotes = string.Join(" ", Enumerable.Repeat("a long sentence that has to wrap", 40));
        form.NotesExpanded = true;

        Assert.Equal(ScrollBars.Vertical, notes.ScrollBars);
    }

    // ---- an update that behaves strangely ----

    [Fact]
    public void AnUpdateThatReportsValuesOutOfRange_DoesNotBreakTheWindow()
    {
        UpdateOutcome outcome;

        using (System.Windows.Forms.Timer timer = Act(prompt => prompt.AcceptButton!.PerformClick()))
        {
            outcome = UpdatePrompt.ShowUpdateAvailableAsync(
                "1",
                "2",
                async progress =>
                {
                    progress.Report(-20);
                    progress.Report(150);
                    progress.Report(int.MaxValue);
                    await Task.Delay(50);

                    return true;
                }).Result;
        }

        Assert.Equal(UpdateOutcome.Applied, outcome);
    }

    [Fact]
    public void AnUpdateThatIsReadyAtOnce_GivesApplied()
    {
        UpdateOutcome outcome;

        using (System.Windows.Forms.Timer timer = Act(prompt => prompt.AcceptButton!.PerformClick()))
            outcome = UpdatePrompt.ShowUpdateAvailableAsync("1", "2", progress => Task.FromResult(true)).Result;

        Assert.Equal(UpdateOutcome.Applied, outcome);
    }

    [Fact]
    public void AnUpdateThatEndsInACancellationOnItsOwn_GivesCancelled_NotFailed()
    {
        UpdateOutcome outcome;

        using (System.Windows.Forms.Timer timer = Act(prompt => prompt.AcceptButton!.PerformClick()))
        {
            outcome = UpdatePrompt.ShowUpdateAvailableAsync(
                "1",
                "2",
                (progress, token) => throw new OperationCanceledException("the update gave up")).Result;
        }

        Assert.Equal(UpdateOutcome.Cancelled, outcome);
        Assert.Empty(Application.OpenForms.OfType<ProgressForm>());
    }

    [Fact]
    public void TheEnterKeyAndEscape_DoNotLeaveAWindowBehind_WhenTheFormIsDisposedAfterwards()
    {
        using (System.Windows.Forms.Timer timer = Act(prompt => prompt.CancelButton!.PerformClick()))
            Assert.Equal(UpdateOutcome.Declined, UpdatePrompt.ShowUpdateAvailableAsync("1", "2", progress => Task.FromResult(true)).Result);

        Assert.Empty(Application.OpenForms.OfType<UpdateAvailableForm>());
    }
}