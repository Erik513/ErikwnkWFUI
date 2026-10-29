using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// The windows of an update: the question "a new version is available" and, after "Update now", the progress of the download. Both are
    /// modal and have to be shown from the thread of the application's windows (the UI thread).
    /// </summary>
    public static class UpdatePrompt
    {
        // A toast over the window of the program: the update is ready. (A toast belongs to a window, so without an owner there is none.)
        private static void ShowReadyToast(Form owner, string message)
        {
            if (owner == null || owner.IsDisposed || message == string.Empty)
                return;

            UIStyles.Toasts.ShowStandard(
                message ?? UIStrings.Get("Update.Ready"),
                owner,
                UIStyles.Colors.Sets.Green,
                icon: ToastIcon.Success);
        }

        /// <summary>
        /// Shows a standardized "a new version is available" dialog. If the user clicks "Update now", the dialog closes and a
        /// <see cref="ProgressForm"/> takes its place while applyUpdateAsync runs (on another thread), reporting its progress into the bar; the
        /// user cannot close that window. Returns how it ended: <see cref="UpdateOutcome.Declined"/> (Later, or the dialog was closed),
        /// <see cref="UpdateOutcome.Skipped"/> (with <paramref name="allowSkip"/>), <see cref="UpdateOutcome.Applied"/> or
        /// <see cref="UpdateOutcome.Failed"/> (applyUpdateAsync returned false or threw; the window then stays with the message until the user
        /// closes it). It is not asynchronous in the sense of returning early: the dialogs are modal, so the call does not come back before the
        /// user has answered and the update has ended, and the task that comes back is already done. Awaiting it is fine, but nothing else of
        /// the calling thread runs meanwhile except the message loops of these windows - start it where blocking for that time is wanted
        /// (not in the middle of a handler that has more to do afterwards).
        /// </summary>
        /// <param name="currentVersion">The version that runs now, as it is shown ("1.2.0"; a leading "v" is not doubled).</param>
        /// <param name="latestVersion">The new version.</param>
        /// <param name="applyUpdateAsync">Downloads and prepares the update, reporting 0 to 100; true when it is ready.</param>
        /// <param name="owner">The window it stands over (null: the middle of the screen).</param>
        /// <param name="title">The title of the windows ("Update available" or "Update verfügbar" when none is given).</param>
        /// <param name="colors">A color set for the title bar of the question; null is the normal dark one.</param>
        /// <param name="size">The size of the question: one of the three or an own <c>Size</c>, in pixels as they are.</param>
        /// <param name="releaseNotes">The text of the release: the question gets a "What's new" button that opens it (null or empty: none).</param>
        /// <param name="allowSkip">Whether the question has a "Skip this version" button (off by default).</param>
        /// <param name="failureMessage">What the progress window says when the update failed ("The update could not be downloaded." when none is given).</param>
        /// <param name="successMessage">What the toast says over <paramref name="owner"/> when the update is ready ("Update completed successfully." when none is given; an empty text shows no toast - there is none without an owner either).</param>
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
            string failureMessage = null,
            string successMessage = null)
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
                failureMessage,
                successMessage);
        }

        /// <summary>
        /// The same, with an update that can be cancelled: <paramref name="applyUpdateAsync"/> gets a token that says when the user clicked Cancel
        /// in the progress window (a task that stops by an <see cref="OperationCanceledException"/>, or returns false after the token was
        /// cancelled, gives <see cref="UpdateOutcome.Cancelled"/>). Like the overload without a token it blocks until everything has ended.
        /// </summary>
        /// <param name="currentVersion">The version that runs now, as it is shown ("1.2.0"; a leading "v" is not doubled).</param>
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
        /// <param name="successMessage">What the toast says over <paramref name="owner"/> when the update is ready ("Update completed successfully." when none is given; an empty text shows no toast - there is none without an owner either).</param>
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
            string failureMessage = null,
            string successMessage = null)
        {
            if (applyUpdateAsync == null)
                throw new ArgumentNullException(nameof(applyUpdateAsync));

            // A window that is gone cannot be stood over (ShowDialog would throw): the windows then stand in the middle of the screen.
            if (owner != null && owner.IsDisposed)
                owner = null;

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
                    configure: progressForm =>
                    {
                        progressForm.ShowTime = true;

                        // As green as the Update now button of the question.
                        progressForm.BarColors = UIStyles.Colors.Sets.Green;
                    });

                if (outcome == ProgressOutcome.Cancelled)
                    return Task.FromResult(UpdateOutcome.Cancelled);
            }
            catch (Exception)
            {
                // The task failed: the window has said so already (showResult) in the words of the library, and the outcome is Failed.
                applied = false;
            }

            if (applied)
                ShowReadyToast(owner, successMessage);

            return Task.FromResult(applied ? UpdateOutcome.Applied : UpdateOutcome.Failed);
        }
    }
}
