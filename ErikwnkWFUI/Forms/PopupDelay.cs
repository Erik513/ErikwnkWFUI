namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// How long a popup waits before it shows: a hover <see cref="InfoPopupForm"/> (default
    /// medium, so passing the mouse over items shows nothing) or a <see cref="ToastForm"/>
    /// (default instant).
    /// </summary>
    public enum PopupDelay
    {
        /// <summary>No wait: the popup shows at once.</summary>
        Instant,

        /// <summary>A short wait (0.2 seconds).</summary>
        Short,

        /// <summary>The default (0.4 seconds).</summary>
        Medium,

        /// <summary>A long wait (0.8 seconds), so passing the mouse over many items shows nothing.</summary>
        Long
    }

    internal static class PopupDelayExtensions
    {
        // The wait of each level in milliseconds (an instant popup has none).
        internal static int ToMilliseconds(this PopupDelay delay)
        {
            switch (delay)
            {
                case PopupDelay.Short:
                    return 200;
                case PopupDelay.Long:
                    return 800;
                default:
                    return 400;
            }
        }
    }
}
