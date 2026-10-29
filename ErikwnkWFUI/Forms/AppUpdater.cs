using System;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;
using ErikwnkCore.Updater;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// Self-contained "check for update" flow for apps hosting their releases on
    /// GitHub. Provides all the UI itself (prompt, progress, failure message) and
    /// only needs the repo coordinates and a couple of HttpClients to talk to
    /// ErikwnkCore.Updater - the consuming app never touches that library directly.
    /// </summary>
    public class AppUpdater
    {
        // How long the program waits for the toast "the update is ready" before it closes (about the time the toast stays for its text).
        private const int ReadyToastMilliseconds = 4000;

        // Set once an update is in place and the program is about to close: a second click on the update button during that time
        // must not start another download.
        private bool _updateApplied;

        private readonly GitHubUpdateChecker _updateChecker;
        private readonly SelfUpdater _selfUpdater;

        /// <summary>checkHttpClient/downloadHttpClient are typically two separate <see cref="HttpClient"/>s so the download's own timeout/headers don't have to match the (usually much shorter) update-check ones.</summary>
        public AppUpdater(
            string repositoryOwner,
            string repositoryName,
            HttpClient checkHttpClient,
            HttpClient downloadHttpClient)
        {
            _updateChecker = new GitHubUpdateChecker(repositoryOwner, repositoryName, checkHttpClient);
            _selfUpdater = new SelfUpdater(downloadHttpClient);
        }

        /// <summary>
        /// Best-effort check against GitHub's latest release that
        /// immediately shows the "update available" prompt if there is one. The check
        /// does not block the window (it is awaited); the prompt is modal like every
        /// <see cref="UpdatePrompt"/> window, so the task ends when its windows are closed. Never
        /// throws; a failed or slow check just means no prompt is shown. On "Update
        /// now", downloads and swaps in the new build and exits the app so the swap
        /// helper can finish the job.
        ///
        /// For a settings screen that would rather notify about a new version once
        /// (not on every single startup) and otherwise show a quiet "update
        /// available" button instead of an unprompted popup, use the silent
        /// <see cref="CheckForUpdateAsync(Version, TimeSpan)"/> overload together
        /// with <see cref="ShowUpdatePromptAsync"/> and
        /// <see cref="CreateUpdateAvailableButton(UpdateCheckResult, Version, Form)"/> instead.
        /// </summary>
        public async Task CheckForUpdateAsync(Version currentVersion, TimeSpan checkTimeout, Form owner)
        {
            try
            {
                UpdateCheckResult result = await CheckForUpdateAsync(currentVersion, checkTimeout);

                if (result != null)
                    await ShowUpdatePromptAsync(result, currentVersion, owner);
            }
            catch
            {
                // A check for an update runs on its own in the background: a failure (no network,
                // GitHub not reachable, a window that is gone) must never disturb the application.
            }
        }

        /// <summary>
        /// Same GitHub check as the other overload, but silent - never shows any
        /// UI, just returns the result (or null on no update/failure/timeout) so
        /// the caller can decide when, or whether, to actually prompt. Never
        /// throws.
        /// </summary>
        public async Task<UpdateCheckResult> CheckForUpdateAsync(Version currentVersion, TimeSpan checkTimeout)
        {
            return (await CheckAsync(currentVersion, checkTimeout)).Result;
        }

        /// <summary>
        /// The same silent check, but it says how it went: <see cref="UpdateCheckStatus.Available"/> with the release, <see cref="UpdateCheckStatus.UpToDate"/>
        /// or <see cref="UpdateCheckStatus.Failed"/> (offline, too slow, a bad answer) - what a "Check for updates" button needs to tell the user "You are up
        /// to date" or "The check did not work". Never shows any UI and never throws.
        /// </summary>
        public async Task<UpdateCheck> CheckAsync(Version currentVersion, TimeSpan checkTimeout)
        {
            try
            {
                using (CancellationTokenSource timeout = new CancellationTokenSource(checkTimeout))
                {
                    return await _updateChecker.CheckAsync(currentVersion, timeout.Token);
                }
            }
            catch
            {
                return new UpdateCheck(UpdateCheckStatus.Failed);
            }
        }

        /// <summary>
        /// Shows the "update available" prompt for a <paramref name="result"/> from
        /// the silent <see cref="CheckForUpdateAsync(Version, TimeSpan)"/> - the
        /// same dialog+download+failure-handling flow the combined overload uses
        /// internally, just callable on its own (from a one-time startup notice, or
        /// later from an update button click). The question has a "What's new" button when the release has a text, and a
        /// "Skip this version" button with <paramref name="allowSkip"/> (<see cref="UpdateOutcome.Skipped"/>: the program should remember the version and not ask
        /// again); the download can be cancelled (<see cref="UpdateOutcome.Cancelled"/>). After a successful download a toast over <paramref name="owner"/> says so and the program exits here a few seconds later, so the swap
        /// helper can finish the job; after a failed one the progress window shows the message and, once it is closed, the release page opens.
        /// Returns how the dialog ended.
        /// </summary>
        /// <param name="result">The release, from the silent check.</param>
        /// <param name="currentVersion">The version that runs now.</param>
        /// <param name="owner">The window the dialogs stand over (null: the middle of the screen).</param>
        /// <param name="allowSkip">Whether the question has a "Skip this version" button.</param>
        /// <param name="colors">A color set for the title bar of the question; null is the normal dark one.</param>
        /// <param name="size">The size of the question: one of the three or an own <c>Size</c>.</param>
        public async Task<UpdateOutcome> ShowUpdatePromptAsync(UpdateCheckResult result, Version currentVersion, Form owner, bool allowSkip = false, UIColorSet colors = null, FormSizeOption? size = null)
        {
            if (_updateApplied)
                return UpdateOutcome.Applied;

            string displayedCurrentVersion =
                currentVersion.Major + "." + currentVersion.Minor + "." + currentVersion.Build;

            UpdateOutcome outcome = await UpdatePrompt.ShowUpdateAvailableAsync(
                displayedCurrentVersion,
                result.LatestVersion.ToString(),
                (progress, token) => ApplyUpdateAsync(result, progress, token),
                owner,
                null,
                colors,
                size,
                result.ReleaseNotes,
                allowSkip,
                true,
                UIStrings.Get("Update.DownloadFailedMessage"),
                UIStrings.Get("Update.ReadyClosing"));

            // The update is in place: the program ends here, on the thread of the window, so that the swap helper can finish the job. The toast
            // that says so (over the owner) needs a moment to be read first; the window stays responsive meanwhile.
            if (outcome == UpdateOutcome.Applied)
            {
                _updateApplied = true;

                if (owner != null && !owner.IsDisposed)
                    await Task.Delay(ReadyToastMilliseconds);

                Application.Exit();
            }

            // The progress window has said what went wrong; the release page is the way on.
            if (outcome == UpdateOutcome.Failed)
                OpenReleasePage(result.ReleaseUrl);

            return outcome;
        }

        /// <summary>
        /// A small, ready-styled green button (with an update glyph and a
        /// tooltip) for <see cref="ErikwnkWFUI.Controls.VersionPanel"/> - clicking it shows
        /// the same prompt <see cref="ShowUpdatePromptAsync"/> does. Add it once
        /// you know an update is available (e.g. after
        /// <see cref="CheckForUpdateAsync(Version, TimeSpan)"/> returned a
        /// non-null result), typically when building the settings screen.
        /// </summary>
        public Button CreateUpdateAvailableButton(UpdateCheckResult result, Version currentVersion, Form owner)
        {
            return CreateUpdateAvailableButton(result, currentVersion, () => owner);
        }

        /// <summary>
        /// The same button, for when the window it belongs to is not known yet or may change: the owner of the prompt is asked for
        /// when the button is clicked.
        /// </summary>
        public Button CreateUpdateAvailableButton(UpdateCheckResult result, Version currentVersion, Func<Form> getOwner)
        {
            // A symbol button, green with a white symbol like the other confirm buttons.
            Button button = UIStyles.Buttons.CreateSymbol(
                UIStyles.Symbols.Update,
                UIStrings.Get("Update.AvailableTooltip"),
                new Size(22, 20),
                UIColors.WithText(UIStyles.Colors.Sets.Green, UIColors.White, 3.0));

            // A FlowLayoutPanel top-aligns its children; center the button in
            // the strip. No right margin - it should sit in the corner.
            button.Margin = new Padding(6, 2, 0, 2);

            // Keep the tooltip in step with a live language switch, the way the
            // title bar's own buttons do.
            EventHandler relocalize = (s, e) =>
                UIButtonFactory.UpdateTooltip(button, UIStrings.Get("Update.AvailableTooltip"));
            UIStrings.LanguageChanged += relocalize;
            button.Disposed += (s, e) => UIStrings.LanguageChanged -= relocalize;

            button.Click += async (sender, e) => await ShowUpdatePromptAsync(result, currentVersion, getOwner());

            return button;
        }

        private async Task<bool> ApplyUpdateAsync(UpdateCheckResult result, IProgress<int> downloadProgress, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(result.DownloadUrl))
            {
                return false;
            }

            bool prepared = await _selfUpdater.DownloadAndPrepareUpdateAsync(
                result.DownloadUrl,
                downloadProgress,
                cancellationToken);

            // The download reports a cancelled task as a failed one: say what it was.
            if (!prepared)
                cancellationToken.ThrowIfCancellationRequested();

            return prepared;
        }

        private static void OpenReleasePage(string releaseUrl)
        {
            if (string.IsNullOrWhiteSpace(releaseUrl))
            {
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = releaseUrl,
                UseShellExecute = true
            });
        }
    }
}
