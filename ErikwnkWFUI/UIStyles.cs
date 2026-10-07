using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
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
        /// Separate from <see cref="ErikwnkCore.AppLocalization"/>.Language (a
        /// consuming app's OWN strings) - see <see cref="SetLanguage"/> to set
        /// both together, which is what most apps actually want.
        /// </summary>
        public static UILanguage Language
        {
            get { return UIStrings.Language; }
            set { UIStrings.Language = value; }
        }

        /// <summary>
        /// Sets this library's own <see cref="Language"/> and a consuming
        /// app's <see cref="ErikwnkCore.AppLocalization"/>.Language together,
        /// from one <see cref="ErikwnkCore.AppLanguage"/> value. The two are
        /// separate properties, in separate assemblies with separate enums,
        /// because AppLocalization has no WinForms dependency at all and
        /// doesn't know this library exists - but almost every app that uses
        /// both wants them switched in lockstep, and forgetting to set one of
        /// the two is an easy way to end up with the app's own text in one
        /// language and ErikwnkWFUI's built-in dialogs (update prompt, title
        /// bar tooltips) in another. Prefer this over setting
        /// <see cref="Language"/> and AppLocalization.Language individually
        /// unless an app specifically wants them to diverge.
        /// </summary>
        public static void SetLanguage(AppLanguage language)
        {
            AppLocalization.Language = language;
            Language = language == AppLanguage.German ? UILanguage.German : UILanguage.English;
        }
    }
}
