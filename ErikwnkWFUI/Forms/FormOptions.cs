using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// Full set of knobs for a <see cref="AppForm"/>. Build one with
    /// <see cref="CreateStandard"/> or <see cref="CreateDialog"/> and tweak
    /// individual properties from there, rather than constructing it with
    /// <c>new FormOptions { ... }</c> directly - the two factory
    /// methods keep related properties in sync (e.g. a dialog has no
    /// maximize button AND can't be snapped/maximized by dragging).
    /// </summary>
    public class FormOptions
    {
        /// <summary>True (the default) removes the native Windows title bar/frame in favor of ErikwnkWFUI's own <see cref="TitleBarControl"/>.</summary>
        public bool Borderless { get; set; } = true;
        public int BorderSize { get; set; } = 1;
        /// <summary>Whether the window can be resized by dragging its edges - only meaningful when <see cref="Borderless"/> is true.</summary>
        public bool Resizable { get; set; } = true;

        public string Title { get; set; } = "";
        public ContentAlignment TitleTextAlign { get; set; } =
            ContentAlignment.MiddleCenter;

        /// <summary>The small logo shown at the top-left of the title bar. See <see cref="AppForm.FormIcon"/> for the equivalent post-construction property.</summary>
        public Image Icon { get; set; } = null;

        public bool ShowMinimizeButton { get; set; } = true;
        public bool ShowMaximizeButton { get; set; } = true;
        public bool ShowCloseButton { get; set; } = true;

        /// <summary>Whether double-clicking the title bar or dragging it to the top of the screen maximizes the window - independent of <see cref="ShowMaximizeButton"/>, but the two built-in presets always keep them in sync.</summary>
        public bool AllowWindowSnapAndMaximize { get; set; } = true;

        public Color? TitleBarBackColor { get; set; }

        /// <summary>The OS-level icon (taskbar, Alt-Tab, system menu) - not the title bar logo, see <see cref="Icon"/> for that. Falls back to the running exe's own icon when left null.</summary>
        public Icon WindowIcon { get; set; } = null;

        /// <summary>
        /// When set, reserves a thin strip at the bottom of the window with
        /// this text right-aligned - typically the app version, e.g.
        /// "v1.0.0". Left null (the default) there is no strip at all;
        /// <see cref="CreateSettings"/> fills it in from the entry
        /// assembly's own version. Set it explicitly to use another text (e.g. a custom
        /// format, or a version string from a different assembly).
        /// </summary>
        public string VersionText { get; set; } = null;

        /// <summary>A normal, resizable window with minimize/maximize/close.</summary>
        public static FormOptions CreateStandard(string title = "", ContentAlignment titleTextAlign = ContentAlignment.MiddleCenter, Color? backColor = null, Image icon = null, Icon windowIcon = null)
        {
            return new FormOptions
            {
                Borderless = true,
                Resizable = true,
                Title = title,
                TitleTextAlign = titleTextAlign,
                TitleBarBackColor = backColor,
                Icon = icon,
                WindowIcon = windowIcon,
                ShowMinimizeButton = true,
                ShowMaximizeButton = true,
                ShowCloseButton = true,
                AllowWindowSnapAndMaximize = true
            };
        }

        /// <summary>A fixed-size dialog: no minimize/maximize, can't be snapped or maximized by dragging.</summary>
        public static FormOptions CreateDialog(string title = "", ContentAlignment titleTextAlign = ContentAlignment.MiddleCenter, Color? backColor = null, Image icon = null, Icon windowIcon = null)
        {
            return new FormOptions
            {
                Borderless = true,
                Resizable = false,
                Title = title,
                TitleTextAlign = titleTextAlign,
                TitleBarBackColor = backColor,
                Icon = icon,
                WindowIcon = windowIcon,
                ShowMinimizeButton = false,
                ShowMaximizeButton = false,
                ShowCloseButton = true,
                AllowWindowSnapAndMaximize = false
            };
        }

        /// <summary>
        /// A fixed-size dialog like <see cref="CreateDialog"/>, but marked
        /// as a settings screen: <see cref="AppForm"/> automatically
        /// shows the entry assembly's version (e.g. "v1.0.0") in a reserved
        /// bottom-right strip, without the caller having to look up or pass
        /// a version string itself. Use this for every "Settings" window -
        /// that's what makes the version strip show up consistently across
        /// all of them instead of each app wiring it up separately.
        /// </summary>
        public static FormOptions CreateSettings(string title = "", ContentAlignment titleTextAlign = ContentAlignment.MiddleLeft, Color? backColor = null, Image icon = null, Icon windowIcon = null)
        {
            FormOptions options = CreateDialog(title, titleTextAlign, backColor, icon, windowIcon);
            options.VersionText = EntryAssemblyVersion.GetText();
            return options;
        }
    }
}
