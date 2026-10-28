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
        /// close button and a fixed size; a settings window is a dialog with the version of the program in a strip at the bottom. The title
        /// bar and the strip are the darkest background of the theme, in the basic color of a color set (<c>colors:</c>) or, with
        /// <c>CreatePrimary...</c>, in a very dark shade of the accent that follows a change of the accent; the three buttons of the title bar have the
        /// colors of the set (the symbols look as in every window). The thin edge around the window is black in all of them (<c>AppForm.BorderColor</c> changes it). A window that can
        /// be resized has a least size (<c>FormOptions.MinimumSize</c>).
        /// </summary>
        public static class Forms
        {
            /// <summary>A resizable window with minimize, maximize and close.</summary>
            /// <param name="title">The title of the window.</param>
            /// <param name="colors">A color set for the title bar and the version strip: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="Color"/>; null (the default) is the normal dark frame.</param>
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
            /// <param name="colors">A color set for the title bar and the version strip; null (the default) is the normal dark one.</param>
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
            /// <param name="colors">A color set for the title bar and the version strip; null (the default) is the normal dark one.</param>
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
        }
    }
}
