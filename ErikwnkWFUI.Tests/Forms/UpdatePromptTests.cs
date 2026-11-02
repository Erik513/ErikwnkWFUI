// The tests wait for ShowUpdateAvailableAsync with .Result / .Wait() on purpose: the prompt is shown as a modal window on the test thread
// (the timers of Act work inside its message loop), and awaiting would resume somewhere else.
#pragma warning disable xUnit1031
using System.Windows.Forms;
using System.Threading;
using ErikwnkCore;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The "update available" prompt: a dialog with Later and Update now, and - after Update now - a progress dialog while the update is applied.</summary>
[Collection(AccentColorTestCollection.Name)]
public class UpdatePromptTests : IDisposable
{
    public void Dispose()
    {
        UIStrings.Language = UILanguage.English;
        AppLocalization.Language = AppLanguage.English;
    }

    // Acts on whatever dialog is open (a timer inside the message loop of ShowDialog): the prompt gets the button that was told, the progress dialog is watched.
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

    private static UpdateOutcome Show(Func<IProgress<int>, Task<bool>> apply, string? title = null)
    {
        return UpdatePrompt.ShowUpdateAvailableAsync("1.0.0", "1.1.0", apply, null, title).Result;
    }

    [Fact]
    public void Later_GivesDeclined_AndTheUpdateIsNeverStarted()
    {
        bool started = false;
        using System.Windows.Forms.Timer timer = Act(prompt => prompt.CancelButton!.PerformClick());

        UpdateOutcome outcome = Show(progress => { started = true; return Task.FromResult(true); });

        Assert.Equal(UpdateOutcome.Declined, outcome);
        Assert.False(started);
        Assert.Empty(Application.OpenForms.OfType<ProgressForm>());
    }

    [Fact]
    public void ClosingThePromptWithTheCloseButtonOrEscape_AlsoGivesDeclined()
    {
        using (System.Windows.Forms.Timer timer = Act(prompt => prompt.Close()))
            Assert.Equal(UpdateOutcome.Declined, Show(progress => Task.FromResult(true)));

        using (System.Windows.Forms.Timer timer = Act(prompt => prompt.InvokePrivate<bool>("ProcessDialogKey", Keys.Escape)))
            Assert.Equal(UpdateOutcome.Declined, Show(progress => Task.FromResult(true)));
    }

    [Fact]
    public void UpdateNow_ShowsAProgressDialog_ThatTheUserCannotClose_AndTheProgressReachesItsBar()
    {
        bool? closeEnabled = null;
        int? percentSeen = null;
        string? titleSeen = null;
        string? statusSeen = null;
        int promptsOpenDuringDownload = -1;

        using System.Windows.Forms.Timer timer = Act(
            prompt => prompt.AcceptButton!.PerformClick(),
            progress =>
            {
                closeEnabled = progress.CloseEnabled;
                percentSeen = progress.Percent;
                titleSeen = progress.Text;
                statusSeen = progress.Status;
                promptsOpenDuringDownload = Application.OpenForms.OfType<UpdateAvailableForm>().Count();
            });

        UpdateOutcome outcome = Show(
            async progress =>
            {
                progress.Report(40);
                await Task.Delay(300);

                return true;
            },
            "My update");

        Assert.Equal(UpdateOutcome.Applied, outcome);
        Assert.False(closeEnabled);
        Assert.Equal(40, percentSeen);
        Assert.Equal("My update", titleSeen);
        Assert.Equal("Downloading update...", statusSeen);
        Assert.Equal(0, promptsOpenDuringDownload);
        Assert.Empty(Application.OpenForms.OfType<ProgressForm>());
    }

    // The failed window stays with its message (the user reads it and closes it); the test reads it and closes it.
    private static Action<ProgressForm> CloseFailed(List<string> messages)
    {
        return progress =>
        {
            if (progress.State != ProgressState.Failed)
                return;

            messages.Add(progress.Status);
            progress.Close();
        };
    }

    [Fact]
    public void AnUpdateThatReturnsFalse_OrThrows_GivesFailed_TheWindowSaysSoUntilItIsClosed()
    {
        List<string> messages = new List<string>();

        using (System.Windows.Forms.Timer timer = Act(prompt => prompt.AcceptButton!.PerformClick(), CloseFailed(messages)))
            Assert.Equal(UpdateOutcome.Failed, Show(progress => Task.FromResult(false)));

        // An exception of the update: the window has the words of the library, not those of the exception.
        using (System.Windows.Forms.Timer timer = Act(prompt => prompt.AcceptButton!.PerformClick(), CloseFailed(messages)))
            Assert.Equal(UpdateOutcome.Failed, Show(progress => throw new InvalidOperationException("Name resolution failed.")));

        Assert.Equal(new[] { "The update could not be downloaded.", "The update could not be downloaded." }, messages);
        Assert.Empty(Application.OpenForms.OfType<ProgressForm>());
        Assert.Empty(Application.OpenForms.OfType<UpdateAvailableForm>());
    }

    [Fact]
    public void AMessageThatWasGiven_IsWhatAFailedWindowSays_AndItFollowsTheLanguageOtherwise()
    {
        List<string> messages = new List<string>();

        using (System.Windows.Forms.Timer timer = Act(prompt => prompt.AcceptButton!.PerformClick(), CloseFailed(messages)))
            UpdatePrompt.ShowUpdateAvailableAsync("1.0.0", "1.1.0", progress => Task.FromResult(false), failureMessage: "Own words.").Wait();

        UIStrings.Language = UILanguage.German;

        using (System.Windows.Forms.Timer timer = Act(prompt => prompt.AcceptButton!.PerformClick(), CloseFailed(messages)))
            Show(progress => Task.FromResult(false));

        Assert.Equal(new[] { "Own words.", "Das Update konnte nicht heruntergeladen werden." }, messages);
    }

    [Fact]
    public void SkipThisVersion_GivesSkipped_AndTheUpdateIsNeverStarted_TheButtonIsThereOnlyWhenWanted()
    {
        bool started = false;
        bool? skipShownByDefault = null;

        using (System.Windows.Forms.Timer timer = Act(prompt => { skipShownByDefault = prompt.AllowSkip; prompt.CancelButton!.PerformClick(); }))
            UpdatePrompt.ShowUpdateAvailableAsync("1.0.0", "1.1.0", progress => { started = true; return Task.FromResult(true); }).Wait();

        Assert.False(skipShownByDefault);

        UpdateOutcome outcome;

        using (System.Windows.Forms.Timer timer = Act(prompt =>
        {
            Button skip = prompt.GetPrivateField<Button>("_skipButton")!;
            Assert.True(skip.Visible);
            skip.PerformClick();
        }))
            outcome = UpdatePrompt.ShowUpdateAvailableAsync("1.0.0", "1.1.0", progress => { started = true; return Task.FromResult(true); }, allowSkip: true).Result;

        Assert.Equal(UpdateOutcome.Skipped, outcome);
        Assert.False(started);
        Assert.Empty(Application.OpenForms.OfType<ProgressForm>());
    }

    [Fact]
    public void TheDownloadCanBeCancelled_AndTheOutcomeSaysSo_AnUpdateThatCannotBeCancelledHasNoButtonForIt()
    {
        UpdateOutcome outcome;
        bool tokenWasCancelled = false;

        using (System.Windows.Forms.Timer timer = Act(
            prompt => prompt.AcceptButton!.PerformClick(),
            progress => progress.GetPrivateField<Button>("_cancelButton")!.PerformClick()))
        {
            outcome = UpdatePrompt.ShowUpdateAvailableAsync(
                "1.0.0",
                "1.1.0",
                async (progress, token) =>
                {
                    try
                    {
                        await Task.Delay(5000, token);
                    }
                    finally
                    {
                        tokenWasCancelled = token.IsCancellationRequested;
                    }

                    return true;
                }).Result;
        }

        Assert.Equal(UpdateOutcome.Cancelled, outcome);
        Assert.True(tokenWasCancelled);

        // A download that reports "false" after the token was cancelled is a cancelled one too, not a failed one.
        using (System.Windows.Forms.Timer timer = Act(
            prompt => prompt.AcceptButton!.PerformClick(),
            progress => progress.GetPrivateField<Button>("_cancelButton")!.PerformClick()))
        {
            outcome = UpdatePrompt.ShowUpdateAvailableAsync(
                "1.0.0",
                "1.1.0",
                async (progress, token) =>
                {
                    while (!token.IsCancellationRequested)
                        await Task.Delay(20);

                    return false;
                }).Result;
        }

        Assert.Equal(UpdateOutcome.Cancelled, outcome);

        // Without the button (cancelable: false, and the old overload) the window cannot be cancelled.
        bool? cancelVisible = null;

        using (System.Windows.Forms.Timer timer = Act(
            prompt => prompt.AcceptButton!.PerformClick(),
            progress => cancelVisible = progress.GetPrivateField<Button>("_cancelButton")!.Visible))
            Show(async progress => { await Task.Delay(150); return true; });

        Assert.False(cancelVisible);
    }

    [Fact]
    public void TheDownloadWindow_ShowsTheTimeAndThePercent()
    {
        bool? showTime = null;
        bool? showPercentage = null;

        using (System.Windows.Forms.Timer timer = Act(
            prompt => prompt.AcceptButton!.PerformClick(),
            progress => { showTime = progress.ShowTime; showPercentage = progress.ShowPercentage; }))
            Show(async progress => { await Task.Delay(150); return true; });

        Assert.True(showTime);
        Assert.True(showPercentage);
    }

    [Fact]
    public void ThePromptHasNoProgressViewAnyMore_AndTheTitleAndTextsFollowTheLanguage()
    {
        UIStrings.Language = UILanguage.German;
        string? title = null;
        string? status = null;

        using System.Windows.Forms.Timer timer = Act(
            prompt => { title = prompt.Text; prompt.AcceptButton!.PerformClick(); },
            progress => status = progress.Status);

        Show(async progress => { await Task.Delay(200); return true; });

        Assert.Equal("Update verfügbar", title);
        Assert.Equal("Update wird heruntergeladen...", status);
        Assert.DoesNotContain(typeof(UpdateAvailableForm).GetMethods(), method => method.Name == "ShowProgressState" || method.Name == "SetProgress");
    }

    [Fact]
    public void TheDialogIsAFixedWindow_WithAnUpdateNowButtonThatIsTheEnterKeyAndALaterButtonThatIsEscape()
    {
        using UpdateAvailableForm form = new UpdateAvailableForm("1.0.0", "1.1.0");

        Assert.NotNull(form.AcceptButton);
        Assert.NotNull(form.CancelButton);
        Assert.Equal(form.Size, form.MinimumSize);
        Assert.Equal(form.Size, form.MaximumSize);
        Assert.False(form.UpdateChosen);
    }
}
