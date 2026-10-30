using System;
using System.Drawing;
using ErikwnkWFUI.Helpers;

#pragma warning disable CS0618

namespace ErikwnkWFUI.Forms
{
    // The old names of the forms, kept until the projects that use them have been changed (see docs/MIGRATION.md); they are removed together.

    /// <summary>The old name of <see cref="AppForm"/>.</summary>
    [Obsolete("Use AppForm (the same, a name that says what it is).")]
    public class StyledForm : AppForm
    {
        public StyledForm(FormOptions options = null)
            : base(options)
        {
        }

        /// <summary>The old quick-start constructor; <c>UIStyles.Forms.CreateStandard/CreateDialog/CreateSettings</c> make the same forms.</summary>
        public StyledForm(string title, StyledFormType type = StyledFormType.Standard)
            : base(type == StyledFormType.Dialog
                ? FormOptions.CreateDialog(title)
                : type == StyledFormType.Settings
                    ? FormOptions.CreateSettings(title)
                    : FormOptions.CreateStandard(title))
        {
        }
    }

    /// <summary>Only <see cref="Settings"/> did something (the version strip); <see cref="FormOptions.CreateSettings"/> does it now.</summary>
    [Obsolete("Use FormOptions.CreateStandard/CreateDialog/CreateSettings (or UIStyles.Forms) - the type is not needed any more.")]
    public enum StyledFormType
    {
        Standard,
        Dialog,
        Settings
    }

    /// <summary>The old name of <see cref="FormOptions"/>: its factory methods still work (and give a <see cref="FormOptions"/>); code that declares a variable of the type <c>StyledFormOptions</c> has to use <see cref="FormOptions"/>.</summary>
    [Obsolete("Use FormOptions (the same).")]
    public class StyledFormOptions : FormOptions
    {
        private StyledFormType _type = StyledFormType.Standard;

        /// <summary>Settings fills in the version text, as it always did (when none is set).</summary>
        public StyledFormType Type
        {
            get { return _type; }
            set
            {
                _type = value;

                if (value == StyledFormType.Settings && string.IsNullOrEmpty(VersionText))
                    VersionText = EntryAssemblyVersion.GetText();
            }
        }
    }

    /// <summary>The old name of <see cref="OptionsForm"/>.</summary>
    [Obsolete("Use OptionsForm (the same).")]
    public class StyledOptionsForm : OptionsForm
    {
        /// <summary>Makes an options window; the same as <see cref="OptionsForm"/>.</summary>
        /// <param name="title">The title of the window.</param>
        public StyledOptionsForm(string title = "Options")
            : base(title)
        {
        }
    }
}
