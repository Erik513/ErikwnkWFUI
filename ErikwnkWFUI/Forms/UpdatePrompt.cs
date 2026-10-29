using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ErikwnkWFUI.Localization;

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
        public static Task<UpdateOutcome> ShowUpdateAvailableAsync(
            string currentVersion,
            string latestVersion,
            Func<IProgress<int>, Task<bool>> applyUpdateAsync,
            Form owner = null,
            string title = null)
        {
            using (UpdateAvailableForm form = new UpdateAvailableForm(
                currentVersion,
                latestVersion,
                title ?? UIStrings.Get("Update.AvailableTitle")))
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
