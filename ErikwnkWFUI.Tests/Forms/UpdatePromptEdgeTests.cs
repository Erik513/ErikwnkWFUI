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