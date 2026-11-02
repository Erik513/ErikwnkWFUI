namespace ErikwnkWFUI.Forms
{
    /// <summary>How an <c>UpdatePrompt.ShowUpdateAvailableAsync</c> dialog ended.</summary>
    public enum UpdateOutcome
    {
        /// <summary>Closed without clicking "Update now".</summary>
        Declined,
        /// <summary>applyUpdateAsync returned true.</summary>
        Applied,
        /// <summary>applyUpdateAsync returned false or threw.</summary>
        Failed,
        /// <summary>The user clicked "Skip this version" (only with <c>allowSkip</c>): the program should not ask again for this version.</summary>
        Skipped,
        /// <summary>The user cancelled the download (only when it can be cancelled), or the update ended by itself with an <see cref="System.OperationCanceledException"/>.</summary>
        Cancelled
    }
}
