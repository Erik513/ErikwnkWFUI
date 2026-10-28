namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// A ready-made option of the library for an <see cref="OptionsForm"/>: <c>AddOption(OptionsPreset.Language)</c> puts the row with its choice
    /// into the table, and the window applies it as it is chosen. What the user chose stays when the window is closed with Save and is taken back
    /// when it is closed without (Cancel, Escape, the close button).
    /// </summary>
    public enum OptionsPreset
    {
        /// <summary>Dark or light theme of the whole program.</summary>
        Theme,

        /// <summary>The accent color of the whole program (one of <c>UIAccentColors</c>).</summary>
        AccentColor,

        /// <summary>The language of the library and of the program's own texts (<c>UIStyles.SetLanguage</c>): English or German.</summary>
        Language,

        /// <summary>Whether the window that owns the options window (the main window of the program) stays above all other windows.</summary>
        AlwaysOnTop
    }
}
