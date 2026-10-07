namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// How long a popup stays before it closes itself: a <see cref="ToastForm"/> 1.5, 2.5 or
    /// 4 seconds, an <see cref="InfoPopupForm"/> 3, 5 or 8 seconds (it is read, not just seen).
    /// </summary>
    public enum PopupDuration
    {
        /// <summary>A short time, for a brief message.</summary>
        Short,

        /// <summary>The default.</summary>
        Medium,

        /// <summary>A long time, for a message that takes longer to read.</summary>
        Long
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
                    return 8000;
                default:
                    return 5000;
            }
        }
    }
}
