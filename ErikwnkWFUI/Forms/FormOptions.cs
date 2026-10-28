using System;
using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// Everything that can be set for an <see cref="AppForm"/>. Start from one of the factory methods and change single properties from
    /// there, rather than writing <c>new FormOptions { ... }</c>: the presets keep related properties together (a dialog has no maximize
    /// button AND cannot be maximized by dragging, and closes with Escape). <see cref="CreateStandard"/> is a resizable window,
    /// <see cref="CreateDialog"/> a fixed-size one with only the close button, <see cref="CreateSettings"/> a dialog with the version of the
    /// program; the <c>CreatePrimary...</c> ones have the frame in the accent. <c>UIStyles.Forms</c> makes the window itself from such options.
    /// </summary>
    public class FormOptions
    {
        /// <summary>True (the default) removes the native Windows title bar/frame in favor of ErikwnkWFUI's own <see cref="TitleBarControl"/>. False keeps the native frame (and has no title bar of the library): <see cref="Resizable"/>, the three buttons and <see cref="Title"/> then are the ones of the native window.</summary>
        public bool Borderless { get; set; } = true;
        /// <summary>Whether the window can be resized by dragging its edges - only meaningful when <see cref="Borderless"/> is true.</summary>
        public bool Resizable { get; set; } = true;

        /// <summary>The title of the window: in the title bar, the taskbar and Alt-Tab.</summary>
        public string Title { get; set; } = "";

        /// <summary>Where the title stands in the title bar (a title aligned to the left or right keeps clear of the logo and the buttons).</summary>
        public ContentAlignment TitleTextAlign { get; set; } =
            ContentAlignment.MiddleCenter;

        /// <summary>The small logo shown at the top-left of the title bar. See <see cref="AppForm.FormIcon"/> for the equivalent post-construction property.</summary>
        public Image Icon { get; set; } = null;

        /// <summary>Whether the title bar has the minimize button (the window then can also be minimized from the taskbar and the system menu).</summary>
        public bool ShowMinimizeButton { get; set; } = true;

        /// <summary>Whether the title bar has the maximize button (and the system menu the entry).</summary>
        public bool ShowMaximizeButton { get; set; } = true;

        /// <summary>Whether the title bar has the close button.</summary>
        public bool ShowCloseButton { get; set; } = true;

        /// <summary>Whether double-clicking the title bar or dragging it to the top of the screen maximizes the window - independent of <see cref="ShowMaximizeButton"/>, but the two built-in presets always keep them in sync.</summary>
        public bool AllowWindowSnapAndMaximize { get; set; } = true;

        /// <summary>The background of the title bar alone, a color of its own (it does not follow the theme); <see cref="Colors"/> colors the whole frame of the window.</summary>
        public Color? TitleBarBackColor { get; set; }

        /// <summary>Whether a window without a frame has a drop shadow (the default). Has no meaning with the native frame, which has its own.</summary>
        public bool Shadow { get; set; } = true;

        /// <summary>Whether the Escape key closes the window when it has no <c>CancelButton</c> (as a click on the close button): true for the dialog presets, false for a standard window.</summary>
        public bool CloseOnEscape { get; set; }

        /// <summary>
        /// A name under which the window remembers its position and size (and whether it was maximized) between runs: it is saved when the
        /// window is closed and restored when it opens, if it is still on a screen that is there. Null (the default) remembers nothing.
        /// Use one name per window of the program. A fixed-size window remembers only its position.
        /// </summary>
        public string RememberBoundsKey { get; set; }

        private UIColorSet _colors;

        /// <summary>
        /// A color set for the frame of the window: the edge around the content, the title bar (its text and its buttons take the colors of
        /// the set) and the version strip are in the basic color of the set; the content stays as it is. Null (the default) is the normal
        /// dark frame. <see cref="CreatePrimary"/> makes one that follows the accent. Not for a color set that is replaced while the window is shown.
        /// </summary>
        public UIColorSet Colors
        {
            get { return _colors; }
            set
            {
                _colors = value;
                ColorSource = ColorSetSource.Of(value);
                FrameSource = value == null ? (Func<Color>)null : () => value.Basic;
            }
        }

        // What AppForm reads: the set (asked again each time) and the color of the frame - for the primary look the same very dark shade of
        // the accent as the primary panels, not the basic color of the set.
        internal Func<UIColorSet> ColorSource { get; set; }
        internal Func<Color> FrameSource { get; set; }

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
                AllowWindowSnapAndMaximize = false,
                CloseOnEscape = true
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

        /// <summary>A normal window with the frame in a very dark shade of the accent (follows a change of the accent).</summary>
        public static FormOptions CreatePrimary(string title = "", ContentAlignment titleTextAlign = ContentAlignment.MiddleCenter, Image icon = null, Icon windowIcon = null)
        {
            return MakePrimary(CreateStandard(title, titleTextAlign, null, icon, windowIcon));
        }

        /// <summary>A fixed-size dialog with the frame in a very dark shade of the accent.</summary>
        public static FormOptions CreatePrimaryDialog(string title = "", ContentAlignment titleTextAlign = ContentAlignment.MiddleCenter, Image icon = null, Icon windowIcon = null)
        {
            return MakePrimary(CreateDialog(title, titleTextAlign, null, icon, windowIcon));
        }

        /// <summary>A settings window (see <see cref="CreateSettings"/>) with the frame in a very dark shade of the accent.</summary>
        public static FormOptions CreatePrimarySettings(string title = "", ContentAlignment titleTextAlign = ContentAlignment.MiddleLeft, Image icon = null, Icon windowIcon = null)
        {
            return MakePrimary(CreateSettings(title, titleTextAlign, null, icon, windowIcon));
        }

        private static FormOptions MakePrimary(FormOptions options)
        {
            options.ColorSource = () => UIColors.PrimarySet;
            options.FrameSource = () => UIColors.PrimaryDarkDark;

            return options;
        }
    }
}
