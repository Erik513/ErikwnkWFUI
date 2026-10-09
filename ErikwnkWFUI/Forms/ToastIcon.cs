namespace ErikwnkWFUI.Forms
{
    /// <summary>The symbol at the left of a <see cref="ToastForm"/>: it tells at a glance what kind of message it is.</summary>
    public enum ToastIcon
    {
        /// <summary>No symbol; the text uses the whole width (the default).</summary>
        None,

        /// <summary>A check mark: something was done or saved.</summary>
        Success,

        /// <summary>An i in a circle: a hint.</summary>
        Info,

        /// <summary>A triangle with an exclamation mark: something to be careful about.</summary>
        Warning,

        /// <summary>A no-entry sign: something failed.</summary>
        Error
    }
}
