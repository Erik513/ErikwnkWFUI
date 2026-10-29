namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// How long a popup stays before it closes itself: a <see cref="ToastForm"/> 1.5, 2.5 or
    /// 4 seconds, an <see cref="InfoPopupForm"/> 3, 5 or 8 seconds (it is read, not just seen).
    /// A toast whose text is long stays longer (see <see cref="ToastForm.DurationGrowsWithText"/>), and <see cref="UntilClicked"/> keeps it until it is clicked.
    /// </summary>
    public enum PopupDuration
    {
        /// <summary>A short time, for a brief message.</summary>
        Short,

        /// <summary>The default.</summary>
        Medium,

        /// <summary>A long time, for a message that takes longer to read.</summary>
        Long,

        /// <summary>
        /// A toast stays until it is clicked (a small cross at its right says so), or until a newer toast replaces it or its window closes. For a very long
        /// text, a message that must not be missed, or one the user is to acknowledge. An <see cref="InfoPopupForm"/> treats it as <see cref="Long"/>.
        /// </summary>
        UntilClicked
    }

    internal static class PopupDurationExtensions
    {
        // The time of each level for a toast, in milliseconds.
        internal static int ToToastMilliseconds(this PopupDuration duration)
        {
            switch (duration)
            {
                case PopupDuration.Short:
                    return 1500;
                case PopupDuration.Long:
                case PopupDuration.UntilClicked: // (no time: the toast has no timer then; this is what the levels below it would have)
                    return 4000;
                default:
                    return 2500;
            }
        }

        // The time of each level for an info popup, in milliseconds.
        internal static int ToInfoMilliseconds(this PopupDuration duration)
        {
            switch (duration)
            {
                case PopupDuration.Short:
                    return 3000;
                case PopupDuration.Long:
                case PopupDuration.UntilClicked: // (an info popup is always closed by its time or by Hide)
                    return 8000;
                default:
                    return 5000;
            }
        }
    }
}
