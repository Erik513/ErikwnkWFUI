namespace ErikwnkWFUI.Forms
{
    /// <summary>How an <see cref="UpdatePrompt.ShowUpdateAvailableAsync"/> dialog ended.</summary>
    public enum UpdateOutcome
    {
        /// <summary>Closed without clicking "Update now".</summary>
        Declined,
        /// <summary>applyUpdateAsync returned true.</summary>
        Applied,
        /// <summary>applyUpdateAsync returned false or threw.</summary>
        Failed
    }
}
