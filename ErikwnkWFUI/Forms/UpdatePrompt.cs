using System;
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
        /// <see cref="UpdateOutcome.Applied"/> or <see cref="UpdateOutcome.Failed"/> (applyUpdateAsync returned false or threw).
        /// </summary>
        /// <param name="currentVersion">The version that runs now, as it is shown ("1.2.0").</param>
        /// <param name="latestVersion">The new version.</param>
        /// <param name="applyUpdateAsync">Downloads and prepares the update, reporting 0 to 100; true when it is ready.</param>
        /// <param name="owner">The window it stands over (null: the middle of the screen).</param>
        /// <param name="title">The title of the windows ("Update available" or "Update verfügbar" when none is given).</param>
        /// <param name="colors">A color set for the title bar of the question; null is the normal dark one.</param>
        /// <param name="size">The size of the question: one of the three or an own <c>Size</c>, in pixels as they are.</param>
        public static Task<UpdateOutcome> ShowUpdateAvailableAsync(
            string currentVersion,
            string latestVersion,
            Func<IProgress<int>, Task<bool>> applyUpdateAsync,
            Form owner = null,
            string title = null,
            UIColorSet colors = null,
            FormSizeOption? size = null)
        {
            using (UpdateAvailableForm form = UIStyles.Forms.CreateUpdateAvailableStandard(
                currentVersion,
                latestVersion,
                title,
                colors,
                size))
            {
                if (owner != null)
                {
                    form.ShowDialog(owner);
                }
                else
                {
                    form.ShowDialog();
                }

                if (!form.UpdateChosen)
                {
                    return Task.FromResult(UpdateOutcome.Declined);
                }
            }

            bool applied = false;

            try
            {
                ProgressBox.Run(
                    title ?? UIStrings.Get("Update.AvailableTitle"),
                    async (progress, token) => applied = await applyUpdateAsync(progress),
                    UIStrings.Get("Update.Downloading"),
                    cancelable: false,
                    owner: owner);
            }
            catch
            {
                applied = false;
            }

            return Task.FromResult(applied ? UpdateOutcome.Applied : UpdateOutcome.Failed);
        }
    }
}
