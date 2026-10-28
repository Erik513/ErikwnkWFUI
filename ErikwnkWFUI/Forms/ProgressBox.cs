using System;
using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ErikwnkWFUI.Forms
{
    /// <summary>How <see cref="ProgressBox.Run"/> ended.</summary>
    public enum ProgressOutcome
    {
        /// <summary>The task ran to its end.</summary>
        Completed,

        /// <summary>The task stopped because the user cancelled it (it ended with an <see cref="OperationCanceledException"/>).</summary>
        Cancelled
    }

    /// <summary>
    /// Shows a progress dialog while a task runs - call <see cref="Run"/>. It is shown as a dialog and closes by itself when the task is done.
    /// </summary>
    public static class ProgressBox
    {
        /// <summary>
        /// Shows a <see cref="ProgressForm"/> modally and runs <paramref name="work"/> (on another thread, so the window stays alive) until it is
        /// done. The task reports with the <see cref="IProgress{T}"/> it gets (0 to 100) and stops when its <see cref="CancellationToken"/> is
        /// cancelled (the user clicked Cancel). Returns <see cref="ProgressOutcome.Cancelled"/> when the task ended by an
        /// <see cref="OperationCanceledException"/>; an exception of the task is thrown again here, after the window has closed.
        /// </summary>
        /// <param name="title">The title of the window ("Please wait" or "Bitte warten" when none is given).</param>
        /// <param name="work">The task: it gets the progress to report to and the token that says when the user cancelled.</param>
        /// <param name="status">The text above the bar.</param>
        /// <param name="cancelable">Whether the window has a Cancel button.</param>
        /// <param name="owner">The window it stands over (null: the middle of the screen).</param>
        /// <param name="size">One of the three widths (<see cref="FormSize.Medium"/> by default) or an own <see cref="Size"/>.</param>
        /// <param name="showResult">Whether a task that failed leaves the window open with its message (<see cref="ProgressForm.Fail"/>) until the user closes it; the exception is thrown after that. By default the window closes at once.</param>
        public static ProgressOutcome Run(
            string title,
            Func<IProgress<int>, CancellationToken, Task> work,
            string status = "",
            bool cancelable = false,
            Form owner = null,
            FormSizeOption? size = null,
            bool showResult = false)
        {
            if (work == null)
                throw new ArgumentNullException(nameof(work));

            FormOptions options = title == null ? null : FormOptions.CreateDialog(title, ContentAlignment.MiddleLeft);
            Exception error = null;
            bool cancelled = false;

            using (ProgressForm form = new ProgressForm(options, status, cancelable, size))
            {
                form.Shown += async (sender, e) =>
                {
                    try
                    {
                        await Task.Run(() => work(form, form.CancellationToken));
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled = true;
                    }
                    catch (Exception exception)
                    {
                        error = exception;

                        // The message stays in the window: the user reads it and closes it with the button.
                        if (showResult)
                        {
                            form.Fail(exception.Message);

                            return;
                        }
                    }

                    form.Close();
                };

                if (owner != null)
                    form.ShowDialog(owner);
                else
                    form.ShowDialog();
            }

            if (error != null)
                ExceptionDispatchInfo.Capture(error).Throw();

            return cancelled ? ProgressOutcome.Cancelled : ProgressOutcome.Completed;
        }
    }
}