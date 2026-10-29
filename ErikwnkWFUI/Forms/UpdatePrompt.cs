using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Forms
{
    public static class UpdatePrompt
    {
        /// <summary>
        /// Shows a standardized "a new version is available" dialog. If the user clicks "Update now", the dialog closes and a
        /// <see cref="ProgressForm"/> takes its place while applyUpdateAsync runs (on another thread), reporting its progress into the bar; the
        /// user cannot close that window. Returns how it ended: <see cref="UpdateOutcome.Declined"/> (Later, or the dialog was closed),
        /// <see cref="UpdateOutcome.Skipped"/> (with <paramref name="allowSkip"/>), <see cref="UpdateOutcome.Applied"/> or
        /// <see cref="UpdateOutcome.Failed"/> (applyUpdateAsync returned false or threw; the window then stays with the message until the user
        /// closes it). It is not asynchronous in the sense of returning early: the dialogs are modal, the task that comes back is already done.
        /// </summary>
        /// <param name="currentVersion">The version that runs now, as it is shown ("1.2.0").</param>
        /// <param name="latestVersion">The new version.</param>
        /// <param name="applyUpdateAsync">Downloads and prepares the update, reporting 0 to 100; true when it is ready.</param>
        /// <param name="owner">The window it stands over (null: the middle of the screen).</param>
        /// <param name="title">The title of the windows ("Update available" or "Update verfügbar" when none is given).</param>
        /// <param name="colors">A color set for the title bar of the question; null is the normal dark one.</param>
        /// <param name="size">The size of the question: one of the three or an own <c>Size</c>, in pixels as they are.</param>
        /// <param name="releaseNotes">The text of the release: the question gets a "What's new" button that opens it (null or empty: none).</param>
        /// <param name="allowSkip">Whether the question has a "Skip this version" button (off by default).</param>
        /// <param name="failureMessage">What the progress window says when the update failed ("The update could not be downloaded." when none is given).</param>
        public static Task<UpdateOutcome> ShowUpdateAvailableAsync(
            string currentVersion,
            string latestVersion,
            Func<IProgress<int>, Task<bool>> applyUpdateAsync,
            Form owner = null,
            string title = null,
            UIColorSet colors = null,
            FormSizeOption? size = null,
            string releaseNotes = null,
            bool allowSkip = false,
            string failureMessage = null)
        {
            if (applyUpdateAsync == null)
                throw new ArgumentNullException(nameof(applyUpdateAsync));

            return ShowUpdateAvailableAsync(
                currentVersion,
                latestVersion,
                (progress, token) => applyUpdateAsync(progress),
                owner,
                title,
                colors,
                size,
                releaseNotes,
                allowSkip,
                false,
                failureMessage);
        }

        /// <summary>
        /// The same, with an update that can be cancelled: <paramref name="applyUpdateAsync"/> gets a token that says when the user clicked Cancel
        /// in the progress window (a task that stops by an <see cref="OperationCanceledException"/>, or returns false after the token was
        /// cancelled, gives <see cref="UpdateOutcome.Cancelled"/>).
        /// </summary>
        /// <param name="currentVersion">The version that runs now, as it is shown ("1.2.0").</param>
        /// <param name="latestVersion">The new version.</param>
        /// <param name="applyUpdateAsync">Downloads and prepares the update, reporting 0 to 100; true when it is ready.</param>
        /// <param name="owner">The window it stands over (null: the middle of the screen).</param>
        /// <param name="title">The title of the windows ("Update available" or "Update verfügbar" when none is given).</param>
        /// <param name="colors">A color set for the title bar of the question; null is the normal dark one.</param>
        /// <param name="size">The size of the question: one of the three or an own <c>Size</c>, in pixels as they are.</param>
        /// <param name="releaseNotes">The text of the release: the question gets a "What's new" button that opens it (null or empty: none).</param>
        /// <param name="allowSkip">Whether the question has a "Skip this version" button (off by default).</param>
        /// <param name="cancelable">Whether the progress window has a Cancel button (on by default).</param>
        /// <param name="failureMessage">What the progress window says when the update failed ("The update could not be downloaded." when none is given).</param>
        public static Task<UpdateOutcome> ShowUpdateAvailableAsync(
            string currentVersion,
            string latestVersion,
            Func<IProgress<int>, CancellationToken, Task<bool>> applyUpdateAsync,
            Form owner = null,
            string title = null,
            UIColorSet colors = null,
            FormSizeOption? size = null,
            string releaseNotes = null,
            bool allowSkip = false,
            bool cancelable = true,
            string failureMessage = null)
        {
            if (applyUpdateAsync == null)
                throw new ArgumentNullException(nameof(applyUpdateAsync));

            using (UpdateAvailableForm form = UIStyles.Forms.CreateUpdateAvailableStandard(
                currentVersion,
                latestVersion,
                title,
                colors,
                size,
                releaseNotes,
                allowSkip))
            {
                if (owner != null)
                {
                    form.ShowDialog(owner);
                }
                else
                {
                    form.ShowDialog();
                }

                if (form.SkipChosen)
                    return Task.FromResult(UpdateOutcome.Skipped);

                if (!form.UpdateChosen)
                    return Task.FromResult(UpdateOutcome.Declined);
            }

            bool applied = false;
            string message = string.IsNullOrEmpty(failureMessage) ? UIStrings.Get("Update.DownloadFailed") : failureMessage;

            try
            {
                ProgressOutcome outcome = ProgressBox.Run(
                    title ?? UIStrings.Get("Update.AvailableTitle"),
                    async (progress, token) =>
                    {
                        bool ready;

                        try
                        {
                            ready = await applyUpdateAsync(progress, token);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            // The window says what went wrong in the words of the library, not in those of a network exception.
                            throw new InvalidOperationException(message, exception);
                        }

                        if (!ready)
                        {
                            token.ThrowIfCancellationRequested();

                            throw new InvalidOperationException(message);
                        }

                        applied = true;
                    },
                    UIStrings.Get("Update.Downloading"),
                    cancelable: cancelable,
                    owner: owner,
                    showResult: true,
                    configure: progressForm => progressForm.ShowTime = true);

                if (outcome == ProgressOutcome.Cancelled)
                    return Task.FromResult(UpdateOutcome.Cancelled);
            }
            catch (Exception)
            {
                applied = false;
            }

            return Task.FromResult(applied ? UpdateOutcome.Applied : UpdateOutcome.Failed);
        }
    }
}
