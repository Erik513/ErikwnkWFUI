using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Tests.Controls.ToastFormTests;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The toast over the window of the program once an update is ready - and no toast where nothing was done or there is no window.</summary>
// The tests wait for ShowUpdateAvailableAsync with .Result on purpose: the dialog is modal on the test thread (the timers of Act work
// inside its message loop), and awaiting would resume somewhere else.
#pragma warning disable xUnit1031
[Collection(AccentColorTestCollection.Name)]
public class UpdateToastTests : ToastTestBase
{
    public override void Dispose()
    {
        base.Dispose();
        UIStrings.Language = UILanguage.English;
    }

    // Acts on the dialog that is open: the button is clicked that the test says.
    private static System.Windows.Forms.Timer Act(bool update)
    {
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 30 };
        timer.Tick += (sender, e) =>
        {
            UpdateAvailableForm? prompt = Application.OpenForms.OfType<UpdateAvailableForm>().FirstOrDefault();

            if (prompt != null)
                (update ? prompt.AcceptButton : prompt.CancelButton)!.PerformClick();
        };
        timer.Start();

        return timer;
    }

    private UpdateOutcome Run(Form? owner, bool update, Func<IProgress<int>, Task<bool>>? apply = null, string? successMessage = null)
    {
        using System.Windows.Forms.Timer timer = Act(update);

        return UpdatePrompt.ShowUpdateAvailableAsync("1", "2", apply ?? (progress => Task.FromResult(true)), owner, successMessage: successMessage).Result;
    }

    [Fact]
    public void AnUpdateThatIsReady_ShowsAToastOverTheOwner_InTheLanguage()
    {
        Form owner = Host();

        Assert.Equal(UpdateOutcome.Applied, Run(owner, true));
        Assert.NotNull(Current());
        Assert.Equal("Update completed successfully.", Part("_messageLabel").Text);
        Current()!.Dispose();

        UIStrings.Language = UILanguage.German;
        Assert.Equal(UpdateOutcome.Applied, Run(owner, true));
        Assert.Equal("Update erfolgreich abgeschlossen.", Part("_messageLabel").Text);
    }

    [Fact]
    public void TheWordsOfTheToast_CanBeGiven_OrLeftEmptyForNoToast()
    {
        Form owner = Host();

        Run(owner, true, successMessage: "Version 2 is in place.");
        Assert.Equal("Version 2 is in place.", Part("_messageLabel").Text);
        Current()!.Dispose();

        Run(owner, true, successMessage: "");
        Assert.Null(Current());
    }

    [Fact]
    public void WithoutAnOwner_ADeclinedUpdate_OrAFailedOne_NoToastComes()
    {
        Assert.Equal(UpdateOutcome.Applied, Run(null, true));
        Assert.Null(Current());

        Form owner = Host();

        Assert.Equal(UpdateOutcome.Declined, Run(owner, false));
        Assert.Null(Current());

        Assert.Equal(UpdateOutcome.Failed, FailedRun(owner));
        Assert.Null(Current());
    }

    private UpdateOutcome FailedRun(Form owner)
    {
        using System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 30 };
        timer.Tick += (sender, e) =>
        {
            UpdateAvailableForm? prompt = Application.OpenForms.OfType<UpdateAvailableForm>().FirstOrDefault();

            if (prompt != null)
            {
                prompt.AcceptButton!.PerformClick();

                return;
            }

            ProgressForm? progress = Application.OpenForms.OfType<ProgressForm>().FirstOrDefault();

            if (progress != null && progress.State == ProgressState.Failed)
                progress.Close();
        };
        timer.Start();

        return UpdatePrompt.ShowUpdateAvailableAsync("1", "2", p => Task.FromResult(false), owner).Result;
    }
}