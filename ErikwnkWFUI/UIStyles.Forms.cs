using System.Drawing;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// Windows with the title bar of the library, ready to use: <c>UIStyles.Forms.CreateStandard("My app")</c>, then put the content into
        /// its <c>ContentPanel</c>. A window of your own derives from <see cref="ErikwnkWFUI.Forms.AppForm"/> and takes the same options
        /// (<see cref="ErikwnkWFUI.Forms.FormOptions"/>) in its constructor. Standard windows can be resized, minimized and maximized; a dialog has only the
        /// close button and a fixed size; a settings window is a dialog with the version of the program in a strip at the bottom (a darker gray
        /// than the content, in every window). The title bar is the darkest background of the theme, in the basic color of a color set
        /// (<c>colors:</c>) or, with <c>CreatePrimary...</c>, in a very dark shade of the accent that follows a change of the accent; its three buttons
        /// have the colors of the set (the symbols look as in every window). The thin edge around the window is black in all of them
        /// (<c>AppForm.BorderColor</c> changes it). A window that can be resized has a least size (<c>FormOptions.MinimumSize</c>).
        /// </summary>
        public static class Forms
        {
            /// <summary>A resizable window with minimize, maximize and close.</summary>
            /// <param name="title">The title of the window.</param>
            /// <param name="colors">A color set for the title bar: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="Color"/>; null (the default) is the normal dark frame.</param>
            /// <param name="titleTextAlign">Where the title stands in the title bar.</param>
            /// <param name="icon">The small logo at the left of the title bar (null: none).</param>
            /// <param name="windowIcon">The icon of the window in the taskbar and Alt-Tab (null: the icon of the program).</param>
            public static ErikwnkWFUI.Forms.AppForm CreateStandard(string title = "", UIColorSet colors = null, ContentAlignment titleTextAlign = ContentAlignment.MiddleCenter, Image icon = null, Icon windowIcon = null)
            {
                return UIFormFactory.Create(ErikwnkWFUI.Forms.FormOptions.CreateStandard(title, titleTextAlign, null, icon, windowIcon), colors);
            }

            /// <summary>A resizable window with the title bar in a very dark shade of the accent.</summary>
            /// <param name="title">The title of the window.</param>
            /// <param name="titleTextAlign">Where the title stands in the title bar.</param>
            /// <param name="icon">The small logo at the left of the title bar (null: none).</param>
            /// <param name="windowIcon">The icon of the window in the taskbar and Alt-Tab (null: the icon of the program).</param>
            public static ErikwnkWFUI.Forms.AppForm CreatePrimary(string title = "", ContentAlignment titleTextAlign = ContentAlignment.MiddleCenter, Image icon = null, Icon windowIcon = null)
            {
                return UIFormFactory.Create(ErikwnkWFUI.Forms.FormOptions.CreatePrimary(title, titleTextAlign, icon, windowIcon), null);
            }

            /// <summary>A fixed-size dialog: only the close button, and it cannot be maximized by dragging.</summary>
            /// <param name="title">The title of the window.</param>
            /// <param name="colors">A color set for the title bar; null (the default) is the normal dark one.</param>
            /// <param name="titleTextAlign">Where the title stands in the title bar.</param>
            /// <param name="icon">The small logo at the left of the title bar (null: none).</param>
            /// <param name="windowIcon">The icon of the window in the taskbar and Alt-Tab (null: the icon of the program).</param>
            public static ErikwnkWFUI.Forms.AppForm CreateDialogStandard(string title = "", UIColorSet colors = null, ContentAlignment titleTextAlign = ContentAlignment.MiddleCenter, Image icon = null, Icon windowIcon = null)
            {
                return UIFormFactory.Create(ErikwnkWFUI.Forms.FormOptions.CreateDialog(title, titleTextAlign, null, icon, windowIcon), colors);
            }

            /// <summary>A fixed-size dialog with the title bar in a very dark shade of the accent.</summary>
            /// <param name="title">The title of the window.</param>
            /// <param name="titleTextAlign">Where the title stands in the title bar.</param>
            /// <param name="icon">The small logo at the left of the title bar (null: none).</param>
            /// <param name="windowIcon">The icon of the window in the taskbar and Alt-Tab (null: the icon of the program).</param>
            public static ErikwnkWFUI.Forms.AppForm CreateDialogPrimary(string title = "", ContentAlignment titleTextAlign = ContentAlignment.MiddleCenter, Image icon = null, Icon windowIcon = null)
            {
                return UIFormFactory.Create(ErikwnkWFUI.Forms.FormOptions.CreateDialogPrimary(title, titleTextAlign, icon, windowIcon), null);
            }

            /// <summary>A dialog like <see cref="CreateDialogStandard"/> with the version of the program (the entry assembly) in a strip at the bottom.</summary>
            /// <param name="title">The title of the window.</param>
            /// <param name="colors">A color set for the title bar; null (the default) is the normal dark one.</param>
            /// <param name="titleTextAlign">Where the title stands in the title bar.</param>
            /// <param name="icon">The small logo at the left of the title bar (null: none).</param>
            /// <param name="windowIcon">The icon of the window in the taskbar and Alt-Tab (null: the icon of the program).</param>
            public static ErikwnkWFUI.Forms.AppForm CreateSettingsStandard(string title = "", UIColorSet colors = null, ContentAlignment titleTextAlign = ContentAlignment.MiddleLeft, Image icon = null, Icon windowIcon = null)
            {
                return UIFormFactory.Create(ErikwnkWFUI.Forms.FormOptions.CreateSettings(title, titleTextAlign, null, icon, windowIcon), colors);
            }

            /// <summary>A settings window with the title bar in a very dark shade of the accent.</summary>
            /// <param name="title">The title of the window.</param>
            /// <param name="titleTextAlign">Where the title stands in the title bar.</param>
            /// <param name="icon">The small logo at the left of the title bar (null: none).</param>
            /// <param name="windowIcon">The icon of the window in the taskbar and Alt-Tab (null: the icon of the program).</param>
            public static ErikwnkWFUI.Forms.AppForm CreateSettingsPrimary(string title = "", ContentAlignment titleTextAlign = ContentAlignment.MiddleLeft, Image icon = null, Icon windowIcon = null)
            {
                return UIFormFactory.Create(ErikwnkWFUI.Forms.FormOptions.CreateSettingsPrimary(title, titleTextAlign, icon, windowIcon), null);
            }

            /// <summary>
            /// An input box (<see cref="ErikwnkWFUI.Forms.InputBoxForm"/>): a small dialog with a prompt, one text box and OK / Cancel. Show it with
            /// <c>ShowDialog</c>; after OK the text is in <c>Value</c>. <see cref="ErikwnkWFUI.Forms.InputBox.Show"/> does all of it in one call.
            /// </summary>
            /// <param name="prompt">The text above the box.</param>
            /// <param name="title">The title of the window ("Input" or "Eingabe" when none is given).</param>
            /// <param name="text">The text the box starts with (selected).</param>
            /// <param name="password">Whether what is typed is hidden (with an eye to show it).</param>
            /// <param name="colors">A color set for the title bar; null (the default) is the normal dark one.</param>
            /// <param name="placeholder">A hint in the empty box that has not the focus.</param>
            public static ErikwnkWFUI.Forms.InputBoxForm CreateInputStandard(string prompt, string title = null, string text = "", bool password = false, UIColorSet colors = null, string placeholder = "")
            {
                return UIFormFactory.CreateInput(ErikwnkWFUI.Forms.FormOptions.CreateDialog(title ?? Localization.UIStrings.Get("InputBox.Title"), ContentAlignment.MiddleLeft), colors, prompt, text, password, placeholder);
            }

            /// <summary>An input box with the title bar in a very dark shade of the accent.</summary>
            /// <param name="prompt">The text above the box.</param>
            /// <param name="title">The title of the window ("Input" or "Eingabe" when none is given).</param>
            /// <param name="text">The text the box starts with (selected).</param>
            /// <param name="password">Whether what is typed is hidden (with an eye to show it).</param>
            /// <param name="placeholder">A hint in the empty box that has not the focus.</param>
            public static ErikwnkWFUI.Forms.InputBoxForm CreateInputPrimary(string prompt, string title = null, string text = "", bool password = false, string placeholder = "")
            {
                return UIFormFactory.CreateInput(ErikwnkWFUI.Forms.FormOptions.CreateDialogPrimary(title ?? Localization.UIStrings.Get("InputBox.Title"), ContentAlignment.MiddleLeft), null, prompt, text, password, placeholder);
            }

            /// <summary>
            /// A settings window around a property table, with the buttons Save and Cancel and the version of the program at the bottom
            /// (<see cref="ErikwnkWFUI.Forms.OptionsForm"/>): it starts with the ready-made options of the library (theme, accent color, language,
            /// always on top), then add rows to its <c>PropertyTable</c> and call <c>FitToContent()</c>.
            /// </summary>
            /// <param name="title">The title of the window ("Options" or "Optionen" when none is given).</param>
            /// <param name="colors">A color set for the title bar; null (the default) is the normal dark one.</param>
            /// <param name="presets">The ready-made options to start with, in this order; null (the default) is all four, an empty array none (<c>AddOption</c> adds them later).</param>
            public static ErikwnkWFUI.Forms.OptionsForm CreateOptionsStandard(string title = null, UIColorSet colors = null, ErikwnkWFUI.Forms.OptionsPreset[] presets = null)
            {
                return UIFormFactory.CreateOptions(ErikwnkWFUI.Forms.FormOptions.CreateSettings(title ?? Localization.UIStrings.Get("OptionsForm.Title")), colors, presets);
            }

            /// <summary>An options window with the title bar in a very dark shade of the accent.</summary>
            /// <param name="title">The title of the window ("Options" or "Optionen" when none is given).</param>
            /// <param name="presets">The ready-made options to start with; null (the default) is all four, an empty array none.</param>
            public static ErikwnkWFUI.Forms.OptionsForm CreateOptionsPrimary(string title = null, ErikwnkWFUI.Forms.OptionsPreset[] presets = null)
            {
                return UIFormFactory.CreateOptions(ErikwnkWFUI.Forms.FormOptions.CreateSettingsPrimary(title ?? Localization.UIStrings.Get("OptionsForm.Title")), null, presets);
            }
        }
    }
}
