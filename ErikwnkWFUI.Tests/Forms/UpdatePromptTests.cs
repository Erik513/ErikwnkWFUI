using System.Windows.Forms;
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

    [Fact]
    public void AnUpdateThatReturnsFalse_OrThrows_GivesFailed_AndTheWindowIsClosed()
    {
        using (System.Windows.Forms.Timer timer = Act(prompt => prompt.AcceptButton!.PerformClick()))
            Assert.Equal(UpdateOutcome.Failed, Show(progress => Task.FromResult(false)));

        using (System.Windows.Forms.Timer timer = Act(prompt => prompt.AcceptButton!.PerformClick()))
            Assert.Equal(UpdateOutcome.Failed, Show(progress => throw new InvalidOperationException("The download failed.")));

        Assert.Empty(Application.OpenForms.OfType<ProgressForm>());
        Assert.Empty(Application.OpenForms.OfType<UpdateAvailableForm>());
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
