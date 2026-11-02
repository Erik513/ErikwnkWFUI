using System;
using System.Drawing;

namespace ErikwnkWFUI.Styles
{
    /// <summary>The colors of the library: the base colors of the theme (backgrounds, text, borders), the shades of the accent and the fixed green, yellow and red families. A control reads them when it paints and follows <see cref="ApplyTheme"/> and <see cref="SetAccent"/> by itself (see <see cref="ThemeChanged"/>). The values are static and not locked: change the theme and the accent from the UI thread.</summary>
    public static partial class UIColors
    {
        /// <summary>Pure black. Fixed: the same in every theme.</summary>
        public static readonly Color Black = Color.Black;

        // Base surface/text/border shades - mutable (unlike the semantic
        // Green/Yellow/Red families below, which stay fixed across themes)
        // so ApplyTheme can swap all of them at once. Defaults are exactly
        // today's dark palette, so ApplyTheme is strictly opt-in, same as
        // SetAccent below.
        /// <summary>The deepest surface of the theme, behind everything else (the page of a window).</summary>
        public static Color BackgroundBlack { get; set; } = Color.FromArgb(10, 10, 10);
        /// <summary>The usual dark surface of a window or a panel.</summary>
        public static Color BackgroundDark { get; set; } = Color.FromArgb(20, 20, 20);
        /// <summary>A surface slightly raised from <see cref="BackgroundDark"/>.</summary>
        public static Color BackgroundDarkElevated { get; set; } = Color.FromArgb(25, 25, 25);
        /// <summary>The surface of the standard control: a text field, a list, a track.</summary>
        public static Color BackgroundMedium { get; set; } = Color.FromArgb(35, 35, 35);
        /// <summary>A surface slightly raised from <see cref="BackgroundMedium"/>.</summary>
        public static Color BackgroundMediumElevated { get; set; } = Color.FromArgb(40, 40, 40);
        /// <summary>A raised surface: a hovered control, a header.</summary>
        public static Color BackgroundLight { get; set; } = Color.FromArgb(50, 50, 50);
        /// <summary>The most raised or active surface of the theme.</summary>
        public static Color BackgroundLighter { get; set; } = Color.FromArgb(60, 60, 60);

        // All shades of the app's single "accent" color - blue by default,
        // like every built-in Windows control. Not readonly (unlike the
        // rest of this file) because SetAccent below needs to overwrite
        // all of them at once when a consuming app wants its own accent
        // instead (e.g. DealOrNoDeal's black/yellow theme). Left untouched,
        // they keep exactly today's values, so calling SetAccent is
        // strictly opt-in and every existing consumer is unaffected.
        /// <summary>The darkest shade of the accent (a panel in the color of the accent). Follows <see cref="SetAccent"/>.</summary>
        public static Color PrimaryDarkDark { get; set; } = Color.FromArgb(0, 30, 60);
        /// <summary>The accent, darkened: the resting fill of a primary control. Follows <see cref="SetAccent"/>.</summary>
        public static Color PrimaryDark { get; set; } = Color.FromArgb(0, 50, 90);
        /// <summary>The accent color itself. Follows <see cref="SetAccent"/>.</summary>
        public static Color Primary { get; set; } = Color.FromArgb(0, 90, 158);
        /// <summary>The accent, lightened: the pressed fill and highlights. Follows <see cref="SetAccent"/>.</summary>
        public static Color PrimaryLight { get; set; } = Color.FromArgb(0, 120, 215);

        /// <summary>A slightly darker shade of the accent. Follows <see cref="SetAccent"/>.</summary>
        public static Color SecondaryDark { get; set; } = Color.FromArgb(20, 80, 140);
        /// <summary>A slightly lighter shade of the accent. Follows <see cref="SetAccent"/>.</summary>
        public static Color Secondary { get; set; } = Color.FromArgb(30, 100, 180);
        /// <summary>A lighter shade of the accent. Follows <see cref="SetAccent"/>.</summary>
        public static Color SecondaryLight { get; set; } = Color.FromArgb(50, 130, 210);

        /// <summary>The dark green of the fixed green family (the same in every theme).</summary>
        public static readonly Color GreenDark = Color.FromArgb(20, 100, 50);
        /// <summary>The green of the fixed green family (the same in every theme).</summary>
        public static readonly Color Green = Color.FromArgb(30, 150, 70);
        /// <summary>The light green of the fixed green family (the same in every theme).</summary>
        public static readonly Color GreenLight = Color.FromArgb(40, 180, 90);
        /// <summary>The lightest green of the fixed green family (the same in every theme).</summary>
        public static readonly Color GreenLighter = Color.FromArgb(50, 210, 110);

        /// <summary>The dark yellow of the fixed yellow family (the same in every theme).</summary>
        public static readonly Color YellowDark = Color.FromArgb(170, 125, 0);
        /// <summary>The yellow of the fixed yellow family (the same in every theme).</summary>
        public static readonly Color Yellow = Color.FromArgb(200, 150, 0);
        /// <summary>The light yellow of the fixed yellow family (the same in every theme).</summary>
        public static readonly Color YellowLight = Color.FromArgb(230, 180, 30);
        /// <summary>The lightest yellow of the fixed yellow family (the same in every theme).</summary>
        public static readonly Color YellowLighter = Color.FromArgb(255, 210, 60);

        /// <summary>The dark red of the fixed red family (the same in every theme).</summary>
        public static readonly Color RedDark = Color.FromArgb(150, 20, 30);
        /// <summary>The red of the fixed red family (the same in every theme).</summary>
        public static readonly Color Red = Color.FromArgb(180, 40, 50);
        /// <summary>The light red of the fixed red family (the same in every theme).</summary>
        public static readonly Color RedLight = Color.FromArgb(210, 60, 70);

        /// <summary>Pure white. Fixed: the same in every theme.</summary>
        public static readonly Color White = Color.White;
        /// <summary>The main text color of the theme.</summary>
        public static Color TextPrimary { get; set; } = Color.FromArgb(240, 240, 240);
        // Fixed dark/light text colors for use against a surface whose shade
        // doesn't follow the current theme - accent shades (PrimaryDark,
        // GreenDark, RedDark, ...) stay the same in Dark and Light, e.g.
        // every button's mouse-down background, which is always a lighter
        // shade than its idle one regardless of hue, so pressed text needs
        // to go dark unconditionally rather than depend on that particular
        // hue's computed contrast. Deliberately NOT TextPrimary/TextPrimaryDim
        // - those are theme roles that flip between near-white and
        // near-black, which is exactly wrong here: GetContrastingForeColor
        // needs a text color that stays readable on a fixed-shade surface
        // regardless of which base theme happens to be active.
        /// <summary>Dark text for a surface whose shade does not follow the theme (an accent, a green button). Fixed: not a theme role.</summary>
        public static readonly Color DarkForeColor = Color.FromArgb(20, 20, 20);
        /// <summary>Light text for a surface whose shade does not follow the theme (an accent, a green button). Fixed: not a theme role.</summary>
        public static readonly Color LightForeColor = Color.FromArgb(240, 240, 240);

        // Same idea as the two above, but for a whole disabled CONTROL
        // surface rather than just text on one - TextDisabled/BorderDark are
        // theme roles (100/170 and 50/220 respectively), so a disabled
        // ToggleSwitch's track came out a visibly different gray depending
        // on which theme was active, even though "disabled" is supposed to
        // look the same everywhere, the same way a disabled CreatePrimary/
        // CreateGreen/CreateRed button already does (their disabled color
        // derives from the theme-independent accent, not a theme role). A
        // true middle gray reads with reasonable contrast against both a
        // near-black Dark background and a near-white Light one.
        /// <summary>The gray of a disabled control surface (a switch track): a middle gray that reads against a dark and a light page alike. Fixed: not a theme role.</summary>
        public static readonly Color DisabledGray = Color.FromArgb(120, 120, 120);
        /// <summary>The main text color, a little dimmed.</summary>
        public static Color TextPrimaryDim { get; set; } = Color.FromArgb(220, 220, 220);
        /// <summary>Secondary text: captions, hints.</summary>
        public static Color TextSecondary { get; set; } = Color.FromArgb(180, 180, 180);
        /// <summary>The quietest regular text.</summary>
        public static Color TextTertiary { get; set; } = Color.FromArgb(140, 140, 140);
        /// <summary>Text of a disabled control. (On the light theme it is faint: the controls work their own disabled text out with <c>GetDisabledText</c>.)</summary>
        public static Color TextDisabled { get; set; } = Color.FromArgb(110, 110, 110);
        /// <summary>Muted text: a count, a version.</summary>
        public static Color TextMuted { get; set; } = Color.FromArgb(120, 120, 120);

        /// <summary>The darkest border or line of the theme.</summary>
        public static Color BorderDark { get; set; } = Color.FromArgb(50, 50, 50);
        /// <summary>The usual border or line of the theme.</summary>
        public static Color BorderMedium { get; set; } = Color.FromArgb(70, 70, 70);
        /// <summary>The lightest border or line of the theme (a hovered border).</summary>
        public static Color BorderLight { get; set; } = Color.FromArgb(90, 90, 90);
        /// <summary>The border in the color of the accent. Follows <see cref="SetAccent"/>.</summary>
        public static Color BorderPrimary { get; set; } = Color.FromArgb(0, 100, 180);
        /// <summary>A red border (an entry that was not right). Fixed: the same in every theme.</summary>
        public static readonly Color BorderRed = Color.FromArgb(180, 40, 50);

        // Text/icon color to use on top of an accent-colored surface (e.g.
        // CreatePrimary buttons, ToastForm). White by default, matching
        // every existing surface today - SetAccent recomputes it so a
        // bright accent (e.g. yellow) gets dark text instead of white
        // text nobody can read.
        /// <summary>The text or icon color on an accent-colored surface: white, or dark on a very bright accent. Follows <see cref="SetAccent"/>.</summary>
        public static Color AccentForeColor { get; set; } = Color.FromArgb(240, 240, 240);

        /// <summary>The see-through tint laid over a surface while the mouse is over it.</summary>
        public static Color HoverOverlay { get; set; } = Color.FromArgb(30, 30, 30, 80);
        /// <summary>The see-through tint laid over a surface while it is pressed.</summary>
        public static Color ActiveOverlay { get; set; } = Color.FromArgb(40, 40, 40, 120);
        // Color.FromArgb(alpha, r, g, b) - this previously had alpha=0
        // (fully transparent, i.e. invisible no matter what it was painted
        // over) because the arguments were in the wrong order for a
        // translucent tint of Primary (0, 90, 158). Fixed to match the same
        // (alpha, r, g, b) pattern as HoverOverlay/ActiveOverlay above.
        /// <summary>The see-through tint of a selected row, in the color of the accent. Follows <see cref="SetAccent"/>.</summary>
        public static Color Selection { get; set; } = Color.FromArgb(60, 0, 90, 158);

        /// <summary>Fully transparent.</summary>
        public static readonly Color Transparent = Color.Transparent;
        /// <summary>A strong black veil (alpha 180), for dimming what lies behind a window.</summary>
        public static readonly Color OverlayDark = Color.FromArgb(180, 0, 0, 0);
        /// <summary>A medium black veil (alpha 120).</summary>
        public static readonly Color OverlayMedium = Color.FromArgb(120, 0, 0, 0);
        /// <summary>A light black veil (alpha 60).</summary>
        public static readonly Color OverlayLight = Color.FromArgb(60, 0, 0, 0);

        /// <summary>
        /// Replaces every base surface color (backgrounds, text, borders,
        /// hover/active tints - everything except the accent, which is set
        /// independently via <see cref="SetAccent"/>) with the ones from the
        /// given theme, e.g. <see cref="UIThemes.Light"/> to switch away from
        /// the dark default. May be called at any time, also while windows are
        /// shown: every control of the library follows the change by itself
        /// (see <see cref="ThemeChanged"/>); only a color set on a control by
        /// hand stays as it was set.
        /// </summary>
        public static void ApplyTheme(UIColorTheme theme)
        {
            if (theme == null)
                return;

            BackgroundBlack = theme.BackgroundBlack;
            BackgroundDark = theme.BackgroundDark;
            BackgroundDarkElevated = theme.BackgroundDarkElevated;
            BackgroundMedium = theme.BackgroundMedium;
            BackgroundMediumElevated = theme.BackgroundMediumElevated;
            BackgroundLight = theme.BackgroundLight;
            BackgroundLighter = theme.BackgroundLighter;

            TextPrimary = theme.TextPrimary;
            TextPrimaryDim = theme.TextPrimaryDim;
            TextSecondary = theme.TextSecondary;
            TextTertiary = theme.TextTertiary;
            TextDisabled = theme.TextDisabled;
            TextMuted = theme.TextMuted;

            BorderDark = theme.BorderDark;
            BorderMedium = theme.BorderMedium;
            BorderLight = theme.BorderLight;

            HoverOverlay = theme.HoverOverlay;
            ActiveOverlay = theme.ActiveOverlay;

            RaiseThemeChanged();
        }

        /// <summary>
        /// Raised after the colors have changed (<see cref="ApplyTheme"/>, <see cref="SetAccent"/>): every control of
        /// the library follows the change by itself, the ones that exist as well as the ones made later. Only a color
        /// that was set on a control by hand stays as it was set.
        /// </summary>
        public static event EventHandler ThemeChanged;

        // Counts the changes: a control without a window (never shown, hidden) misses the event and catches up with this
        // when its window is created (see ThemeWatcher).
        internal static int ThemeVersion { get; private set; }

        internal static void RaiseThemeChanged()
        {
            ThemeVersion++;
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Replaces every accent shade (Primary/Secondary/Selection/
        /// BorderPrimary - everything a consuming app would otherwise have
        /// to override control-by-control) with tints and shades computed
        /// from a single base color. May be called at any time, also while
        /// windows are shown: the controls of the library follow the change by
        /// themselves (see <see cref="ThemeChanged"/>).
        /// </summary>
        public static void SetAccent(Color accent)
        {
            // Gentler than a straight percentage-of-blue-value darken would
            // suggest - blue's own default (0, 90, 158) was already fairly
            // dark, so darkening it hard still left a visible dark blue.
            // Doing the same to a bright, near-max-brightness accent (e.g.
            // a bright yellow) would crush it into a muddy brown instead.
            //
            // A neutral (grayscale) accent has no hue to keep it looking
            // rich once darkened this much - the same percentage that reads
            // as a moody dark blue just reads as a flat slab of gray, and
            // BlackOrWhite (near-white in Dark, near-black in Light) sits
            // right at the brightness extreme where that's most obvious.
            // Darkening it gentler keeps popups/panels built from these
            // shades closer to the accent's own brightness instead of
            // crushing toward black.
            bool isNeutralAccent = IsNeutral(accent);
            double darkDarkAmount = isNeutralAccent ? 0.30 : 0.55;
            double darkAmount = isNeutralAccent ? 0.15 : 0.30;

            PrimaryDarkDark = DarkenByShare(accent, darkDarkAmount);
            PrimaryDark = DarkenByShare(accent, darkAmount);
            Primary = accent;
            PrimaryLight = LightenByShare(accent, 0.35);

            SecondaryDark = DarkenByShare(accent, 0.10);
            Secondary = LightenByShare(accent, 0.10);
            SecondaryLight = LightenByShare(accent, 0.25);

            BorderPrimary = accent;

            // A hued accent stands out from a row's own gray shade through
            // the hue shift alone, even at a low overlay alpha - a neutral
            // accent has no hue to lean on, so it needs the same higher
            // alpha ListView's own CreateStandard selection default uses
            // for the same reason.
            Selection = CreateSelectionOverlay(accent);

            AccentForeColor = GetContrastingForeColor(accent);

            RaiseThemeChanged();
        }

        /// <summary>
        /// The see-through tint a selected row gets in the given color. A hued
        /// color stands out from a row's own gray even at a low alpha; a
        /// neutral one has no hue to lean on and needs a higher one.
        /// </summary>
        internal static Color CreateSelectionOverlay(Color accent)
        {
            return Color.FromArgb(IsNeutral(accent) ? 130 : 60, accent.R, accent.G, accent.B);
        }
    }
}
