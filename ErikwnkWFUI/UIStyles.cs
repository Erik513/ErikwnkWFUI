using System;
using ErikwnkCore;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI
{
    /// <summary>
    /// The single entry point for ErikwnkWFUI's control factories and styling
    /// - start here rather than the individual classes under
    /// <see cref="ErikwnkWFUI.Factories"/>/<see cref="ErikwnkWFUI.Styles"/>
    /// (most of which are internal). Each nested class below (<see
    /// cref="Buttons"/>, <see cref="Labels"/>, ...) mirrors one control type
    /// with a set of <c>Create*</c> factory methods that return a
    /// pre-themed, ready-to-add WinForms control. See <see cref="Colors"/>
    /// for how to customize the color scheme (accent color and/or full
    /// light/dark theme) and <see cref="Language"/>/<see cref="SetLanguage"/>
    /// for switching the language of built-in dialog text.
    /// </summary>
    public static partial class UIStyles
    {
        /// <summary>
        /// Language used by built-in dialogs/controls that ship their own text
        /// (update prompt, title bar tooltips). Set once at startup, before any
        /// ErikwnkWFUI form is created, to switch away from the English default.
        /// The same setting as <see cref="ErikwnkCore.AppLocalization"/>.Language (the language of a consuming app's OWN texts): set either
        /// one and the other follows, and <see cref="LanguageChanged"/> tells everything that is already built.
        /// </summary>
        public static UILanguage Language
        {
            get { return UIStrings.Language; }
            set { UIStrings.Language = value; }
        }

        /// <summary>
        /// Sets the language from one <see cref="ErikwnkCore.AppLanguage"/> value. It is the same as setting <see cref="Language"/> or
        /// <see cref="ErikwnkCore.AppLocalization"/>.Language - they are one setting - and is kept for the programs that call it.
        /// </summary>
        public static void SetLanguage(AppLanguage language)
        {
            AppLocalization.Language = language;
        }

        /// <summary>
        /// Raised when the language changes, whichever way it was set (<see cref="Language"/>, <see cref="SetLanguage"/>,
        /// <see cref="ErikwnkCore.AppLocalization"/>.Language). The one event for everything that shows a text of its own: a table of an
        /// options window and the controls bound with <see cref="Localize{T}(T, System.Func{string})"/> listen to it themselves.
        /// </summary>
        public static event EventHandler LanguageChanged
        {
            add { UIStrings.LanguageChanged += value; }
            remove { UIStrings.LanguageChanged -= value; }
        }
    }
}
