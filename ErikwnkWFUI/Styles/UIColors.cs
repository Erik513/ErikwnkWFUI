using System;
using System.Drawing;

namespace ErikwnkWFUI.Styles
{
    public static class UIColors
    {
        public static readonly Color Black = Color.Black;

        // Base surface/text/border shades - mutable (unlike the semantic
        // Green/Yellow/Red families below, which stay fixed across themes)
        // so ApplyTheme can swap all of them at once. Defaults are exactly
        // today's dark palette, so ApplyTheme is strictly opt-in, same as
        // SetAccent below.
        public static Color BackgroundBlack { get; set; } = Color.FromArgb(10, 10, 10);
        public static Color BackgroundDark { get; set; } = Color.FromArgb(20, 20, 20);
        public static Color BackgroundDarkElevated { get; set; } = Color.FromArgb(25, 25, 25);
        public static Color BackgroundMedium { get; set; } = Color.FromArgb(35, 35, 35);
        public static Color BackgroundMediumElevated { get; set; } = Color.FromArgb(40, 40, 40);
        public static Color BackgroundLight { get; set; } = Color.FromArgb(50, 50, 50);
        public static Color BackgroundLighter { get; set; } = Color.FromArgb(60, 60, 60);

        // All shades of the app's single "accent" color - blue by default,
        // like every built-in Windows control. Not readonly (unlike the
        // rest of this file) because SetAccent below needs to overwrite
        // all of them at once when a consuming app wants its own accent
        // instead (e.g. DealOrNoDeal's black/yellow theme). Left untouched,
        // they keep exactly today's values, so calling SetAccent is
        // strictly opt-in and every existing consumer is unaffected.
        public static Color PrimaryDarkDark { get; set; } = Color.FromArgb(0, 30, 60);
        public static Color PrimaryDark { get; set; } = Color.FromArgb(0, 50, 90);
        public static Color Primary { get; set; } = Color.FromArgb(0, 90, 158);
        public static Color PrimaryLight { get; set; } = Color.FromArgb(0, 120, 215);

        public static Color SecondaryDark { get; set; } = Color.FromArgb(20, 80, 140);
        public static Color Secondary { get; set; } = Color.FromArgb(30, 100, 180);
        public static Color SecondaryLight { get; set; } = Color.FromArgb(50, 130, 210);

        public static readonly Color GreenDark = Color.FromArgb(20, 100, 50);
        public static readonly Color Green = Color.FromArgb(30, 150, 70);
        public static readonly Color GreenLight = Color.FromArgb(40, 180, 90);
        public static readonly Color GreenLighter = Color.FromArgb(50, 210, 110);

        public static readonly Color YellowDark = Color.FromArgb(170, 125, 0);
        public static readonly Color Yellow = Color.FromArgb(200, 150, 0);
        public static readonly Color YellowLight = Color.FromArgb(230, 180, 30);
        public static readonly Color YellowLighter = Color.FromArgb(255, 210, 60);

        public static readonly Color RedDark = Color.FromArgb(150, 20, 30);
        public static readonly Color Red = Color.FromArgb(180, 40, 50);
        public static readonly Color RedLight = Color.FromArgb(210, 60, 70);

        public static readonly Color White = Color.White;
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
        public static readonly Color DarkForeColor = Color.FromArgb(20, 20, 20);
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
        public static readonly Color DisabledGray = Color.FromArgb(120, 120, 120);
        public static Color TextPrimaryDim { get; set; } = Color.FromArgb(220, 220, 220);
        public static Color TextSecondary { get; set; } = Color.FromArgb(180, 180, 180);
        public static Color TextTertiary { get; set; } = Color.FromArgb(140, 140, 140);
        public static Color TextDisabled { get; set; } = Color.FromArgb(110, 110, 110);
        public static Color TextMuted { get; set; } = Color.FromArgb(120, 120, 120);

        public static Color BorderDark { get; set; } = Color.FromArgb(50, 50, 50);
        public static Color BorderMedium { get; set; } = Color.FromArgb(70, 70, 70);
        public static Color BorderLight { get; set; } = Color.FromArgb(90, 90, 90);
        public static Color BorderPrimary { get; set; } = Color.FromArgb(0, 100, 180);
        public static readonly Color BorderRed = Color.FromArgb(180, 40, 50);

        // Text/icon color to use on top of an accent-colored surface (e.g.
        // CreatePrimary buttons, ToastForm). White by default, matching
        // every existing surface today - SetAccent recomputes it so a
        // bright accent (e.g. yellow) gets dark text instead of white
        // text nobody can read.
        public static Color AccentForeColor { get; set; } = Color.FromArgb(240, 240, 240);

        public static Color HoverOverlay { get; set; } = Color.FromArgb(30, 30, 30, 80);
        public static Color ActiveOverlay { get; set; } = Color.FromArgb(40, 40, 40, 120);
        // Color.FromArgb(alpha, r, g, b) - this previously had alpha=0
        // (fully transparent, i.e. invisible no matter what it was painted
        // over) because the arguments were in the wrong order for a
        // translucent tint of Primary (0, 90, 158). Fixed to match the same
        // (alpha, r, g, b) pattern as HoverOverlay/ActiveOverlay above.
        public static Color Selection { get; set; } = Color.FromArgb(60, 0, 90, 158);

        public static readonly Color Transparent = Color.Transparent;
        public static readonly Color OverlayDark = Color.FromArgb(0, 0, 0, 180);
        public static readonly Color OverlayMedium = Color.FromArgb(0, 0, 0, 120);
        public static readonly Color OverlayLight = Color.FromArgb(0, 0, 0, 60);

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
            bool isNeutralAccent = accent.R == accent.G && accent.G == accent.B;
            double darkDarkAmount = isNeutralAccent ? 0.30 : 0.55;
            double darkAmount = isNeutralAccent ? 0.15 : 0.30;

            PrimaryDarkDark = Darken(accent, darkDarkAmount);
            PrimaryDark = Darken(accent, darkAmount);
            Primary = accent;
            PrimaryLight = Lighten(accent, 0.35);

            SecondaryDark = Darken(accent, 0.10);
            Secondary = Lighten(accent, 0.10);
            SecondaryLight = Lighten(accent, 0.25);

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

        // Above this, background is bright enough that dark text wins;
        // at/below it, light text wins. Deliberately above a literal 50%
        // midpoint (not just "which one contrasts more") - button press
        // colors like PrimaryLight/GreenLight sit right around 50% and
        // read better with light text by UI convention (light text on a
        // saturated blue/green button), even on the rare background where
        // dark text would technically score a marginally higher contrast
        // ratio. Still comfortably below genuinely bright colors like
        // Yellow (~0.58) and YellowLighter (~0.81), which correctly keep
        // dark text.
        private const double LightBackgroundThreshold = 0.55;

        /// <summary>
        /// White or near-black, whichever reads better on top of the given
        /// background - e.g. white text is unreadable on a bright yellow
        /// accent even though it's fine on the default dark blue. Not the
        /// current theme's TextPrimary - callers use this specifically for
        /// surfaces (accent/semantic colors) that don't follow the base
        /// theme, so the answer must not depend on it either.
        /// </summary>
        public static Color GetContrastingForeColor(Color background)
        {
            return IsLight(background) ? DarkForeColor : LightForeColor;
        }

        /// <summary>Whether a color is bright enough that dark text is the one to put on it.</summary>
        public static bool IsLight(Color color)
        {
            return RelativeLuminance(color) > LightBackgroundThreshold;
        }

        /// <summary>
        /// The see-through tint a selected row gets in the given color. A hued
        /// color stands out from a row's own gray even at a low alpha; a
        /// neutral one has no hue to lean on and needs a higher one.
        /// </summary>
        internal static Color CreateSelectionOverlay(Color accent)
        {
            bool isNeutral = accent.R == accent.G && accent.G == accent.B;

            return Color.FromArgb(isNeutral ? 130 : 60, accent.R, accent.G, accent.B);
        }

        // ---- color sets: one color in, every state out ----

        /// <summary>
        /// The yellow look: white text that stays white in every state and in both
        /// themes. The disabled yellow is the resting one darkened as far as any
        /// dark fill gets.
        /// </summary>
        internal static readonly UIColorSet YellowSet = CreateYellowSet();

        private static UIColorSet CreateYellowSet()
        {
            Color disabled = Darken(Yellow, DisabledDarkenAmount);

            return new UIColorSet(
                Yellow,
                YellowLight,
                YellowLighter,
                disabled,
                White,
                White,
                White,
                BlendToward(White, disabled, DisabledTextBlend));
        }

        /// <summary>The look of the accent color: darker at rest, the accent hovered, lighter pressed.</summary>
        internal static UIColorSet PrimarySet
        {
            get { return CreateSet(PrimaryDark, Primary, PrimaryLight); }
        }

        // How far a disabled fill is darkened: a light fill only dims a
        // little, a dark one more. Text on it is the best text color blended
        // this far toward the fill - readable, but visibly muted.
        private const double DisabledDarkenAmount = 0.35;
        private const double LightDisabledDarkenAmount = 0.1;
        private const double DisabledTextBlend = 0.35;

        /// <summary>
        /// A color set from a color of your choice, in the same style as the
        /// built-in Primary look: the color is what the control shows while
        /// hovered, rest is a darker shade of it and pressed a lighter one.
        /// A light color (yellow, white) keeps its look at rest instead and
        /// gets darker when hovered and pressed, since a lighter shade would
        /// vanish. Text and disabled colors are worked out from the fills.
        /// </summary>
        internal static UIColorSet CreateSet(Color color)
        {
            if (IsLight(color))
                return CreateSet(color, ShiftLightness(color, -0.08), ShiftLightness(color, -0.16));

            bool isNeutral = color.R == color.G && color.G == color.B;

            return CreateSet(ShiftLightness(color, isNeutral ? -0.06 : -0.12), color, ShiftLightness(color, 0.12));
        }

        /// <summary>
        /// A color set from the three fills you want; the text colors (the
        /// ones that read best on each fill) and the disabled look are worked
        /// out from them.
        /// </summary>
        internal static UIColorSet CreateSet(Color basic, Color hover, Color pressed)
        {
            Color disabled = Darken(basic, IsLight(basic) ? LightDisabledDarkenAmount : DisabledDarkenAmount);
            Color basicText = GetBestTextColor(basic);

            return new UIColorSet(
                basic,
                hover,
                pressed,
                disabled,
                basicText,
                GetTextOn(hover, basicText, KeepTextRatio),
                GetTextOn(pressed, basicText, PressedKeepTextRatio),
                BlendToward(GetBestTextColor(disabled), disabled, DisabledTextBlend));
        }

        /// <summary>
        /// Like <see cref="CreateSet(Color, Color, Color)"/>, but the text has the
        /// one color you give in every state (muted when disabled) instead of
        /// the best one for each fill - for a look whose text color is fixed,
        /// such as the white symbols of the button presets.
        /// </summary>
        internal static UIColorSet CreateSet(Color basic, Color hover, Color pressed, Color text)
        {
            Color disabled = Darken(basic, IsLight(basic) ? LightDisabledDarkenAmount : DisabledDarkenAmount);

            return new UIColorSet(
                basic,
                hover,
                pressed,
                disabled,
                text,
                text,
                text,
                BlendToward(text, disabled, DisabledTextBlend));
        }

        /// <summary>
        /// The same look as <paramref name="set"/>, but with <paramref name="text"/> in
        /// every state (muted when disabled). With a <paramref name="minimumContrast"/>
        /// the fills that would not carry that text well enough are darkened until they
        /// do - a white symbol on a set whose pressed fill is bright, say.
        /// </summary>
        internal static UIColorSet WithText(UIColorSet set, Color text, double minimumContrast = 0)
        {
            return new UIColorSet(
                DarkenUntilReadable(set.Basic, text, minimumContrast),
                DarkenUntilReadable(set.Hover, text, minimumContrast),
                DarkenUntilReadable(set.Pressed, text, minimumContrast),
                set.Disabled,
                text,
                text,
                text,
                BlendToward(text, set.Disabled, DisabledTextBlend));
        }

        /// <summary>
        /// <paramref name="fill"/> shifted in lightness just far enough that <paramref name="text"/>
        /// reads on it (<paramref name="minimum"/>:1) - for a set whose fill is so mid-tone that
        /// neither light nor dark text gets there. A fill that already carries the text stays as it is.
        /// </summary>
        internal static Color GetReadableFill(Color fill, Color text, double minimum = 4.5)
        {
            double step = GetContrastRatio(text, Color.White) < GetContrastRatio(text, Color.Black) ? -0.01 : 0.01;
            Color result = fill;

            for (int i = 0; i < 40 && GetContrastRatio(text, result) < minimum; i++)
                result = ShiftLightness(result, step);

            return result;
        }

        private static Color DarkenUntilReadable(Color fill, Color text, double minimumContrast)
        {
            Color result = fill;

            for (int step = 0; step < 40 && GetContrastRatio(text, result) < minimumContrast; step++)
                result = ShiftLightness(result, -0.01);

            return result;
        }

        /// <summary>
        /// The outline of a control on <paramref name="page"/>: the best text color for the page,
        /// moved toward it just far enough to keep 3:1 against it (the bar for the boundary of a
        /// control), so it stands out on any page color.
        /// </summary>
        internal static Color GetOutlineColor(Color page)
        {
            Color text = GetBestTextColor(page);

            for (double toward = 0.7; toward > 0; toward -= 0.05)
            {
                Color outline = BlendToward(text, page, toward);

                if (GetContrastRatio(outline, page) >= 3.0)
                    return outline;
            }

            return text;
        }

        /// <summary>
        /// The quiet line of a panel (frame, lines of a table or a card) on <paramref name="surface"/>: the theme's
        /// <see cref="BorderMedium"/>, moved only as far as needed to keep a ratio of 1.5:1 against the surface - on a color (green,
        /// accent) the gray of the theme can be as dark as the surface itself.
        /// </summary>
        internal static Color GetLineColor(Color surface)
        {
            return EnsureContrast(BorderMedium, surface, QuietLineContrast);
        }

        private const double QuietLineContrast = 1.5;

        /// <summary>
        /// <paramref name="color"/> moved lighter (on a dark page) or darker (on a light one), in
        /// small steps, until it has at least <paramref name="minimum"/> contrast against the page.
        /// </summary>
        internal static Color EnsureContrast(Color color, Color page, double minimum)
        {
            double step = IsLight(page) ? -0.03 : 0.03;

            for (int i = 0; i < 40 && GetContrastRatio(color, page) < minimum; i++)
                color = ShiftLightness(color, step);

            return color;
        }

        /// <summary>
        /// A track for a slider on <paramref name="surface"/>: the surface itself moved lighter (on a dark one) or darker (on a light one)
        /// until it stands out by the quiet 1.5:1 - and no more, in either theme. (<see cref="GetLineColor"/> keeps the gray of the theme, which
        /// may lie far from a surface of another brightness: no color would then stand out from both.)
        /// </summary>
        internal static Color GetQuietTrackColor(Color surface)
        {
            return EnsureContrast(surface, surface, QuietLineContrast);
        }

        /// <summary>
        /// The same against two colors at once: the nearest lightness of <paramref name="color"/> (hue and saturation kept) that has at least
        /// <paramref name="minimum"/> contrast against the page and against <paramref name="other"/> (a color that lies on the page, such as the
        /// track of a slider), looking in the direction of the page first (lighter on a dark page, darker on a light one) and then the other way:
        /// the other color may lie on the far side of the color, and moving it for each of the two in turn would move it back and forth.
        /// Where no lightness has both, the one whose worse contrast is the best.
        /// </summary>
        internal static Color EnsureContrast(Color color, Color page, Color other, double minimum)
        {
            double toward = IsLight(page) ? -0.01 : 0.01;
            Color best = color;
            double bestWorst = -1;

            for (int i = 0; i <= 100; i++)
            {
                foreach (double direction in new[] { toward, -toward })
                {
                    Color candidate = i == 0 ? color : ShiftLightness(color, direction * i);
                    double worst = Math.Min(GetContrastRatio(candidate, page), GetContrastRatio(candidate, other));

                    if (worst >= minimum)
                        return candidate;

                    if (worst > bestWorst)
                    {
                        bestWorst = worst;
                        best = candidate;
                    }
                }
            }

            return best;
        }

        // Text keeps the color it has at rest as long as it stays clearly
        // readable on the next fill; only when it would not, the better one
        // takes over. Otherwise the text would flip between light and dark each
        // time the mouse moves over a button. Hovering can last, so it asks for
        // 3:1; pressing is a moment, and light text on the bright pressed fills of
        // green, orange and teal is still plainly readable at about 2.1-2.4:1, so
        // there 2:1 is enough.
        private const double KeepTextRatio = 3.0;
        private const double PressedKeepTextRatio = 2.0;

        private static Color GetTextOn(Color fill, Color preferred, double keepRatio)
        {
            return GetContrastRatio(preferred, fill) >= keepRatio ? preferred : GetBestTextColor(fill);
        }

        /// <summary>
        /// The dark or the light text color, whichever has the higher WCAG
        /// contrast on the background. Unlike <see cref="GetContrastingForeColor"/>
        /// this is the true ratio, not a brightness rule of thumb.
        /// </summary>
        internal static Color GetBestTextColor(Color background)
        {
            return GetContrastRatio(LightForeColor, background) >= GetContrastRatio(DarkForeColor, background)
                ? LightForeColor
                : DarkForeColor;
        }

        /// <summary>
        /// The color with its HSL lightness moved by <paramref name="delta"/>
        /// (-1 to 1), hue and saturation kept. Unlike blending toward white or
        /// black this keeps a color saturated, so a lighter blue is still a
        /// blue and not a pale one.
        /// </summary>
        internal static Color ShiftLightness(Color color, double delta)
        {
            double hue = color.GetHue();
            double saturation = color.GetSaturation();
            double lightness = Math.Max(0, Math.Min(1, color.GetBrightness() + delta));

            double chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            double x = chroma * (1 - Math.Abs(hue / 60 % 2 - 1));
            double m = lightness - chroma / 2;
            double r, g, b;

            if (hue < 60) { r = chroma; g = x; b = 0; }
            else if (hue < 120) { r = x; g = chroma; b = 0; }
            else if (hue < 180) { r = 0; g = chroma; b = x; }
            else if (hue < 240) { r = 0; g = x; b = chroma; }
            else if (hue < 300) { r = x; g = 0; b = chroma; }
            else { r = chroma; g = 0; b = x; }

            return Color.FromArgb(
                color.A,
                (int)Math.Round((r + m) * 255),
                (int)Math.Round((g + m) * 255),
                (int)Math.Round((b + m) * 255));
        }

        /// <summary>The WCAG 2.1 contrast ratio (1 to 21) between two colors.</summary>
        internal static double GetContrastRatio(Color first, Color second)
        {
            double a = GetWcagLuminance(first);
            double b = GetWcagLuminance(second);

            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }

        private static double GetWcagLuminance(Color color)
        {
            return 0.2126 * ToLinear(color.R) + 0.7152 * ToLinear(color.G) + 0.0722 * ToLinear(color.B);
        }

        private static double ToLinear(int channel)
        {
            double value = channel / 255.0;

            return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        private static double RelativeLuminance(Color color)
        {
            return (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
        }

        internal static Color Darken(Color color, double amount)
        {
            return Color.FromArgb(
                (int)(color.R * (1 - amount)),
                (int)(color.G * (1 - amount)),
                (int)(color.B * (1 - amount)));
        }

        // The red -> yellow -> green of a status progress bar. Two segments rather than one
        // straight blend, which would pass through a muddy brown around the middle instead of a
        // clear yellow. The breakpoint is not the middle (0.5): green should only dominate near
        // the top, so red -> yellow gets the larger share (0-65%) and only the last third blends
        // yellow -> green; a flat 50/50 split turned green too early.
        private const double YellowBreakpoint = 0.65;

        /// <summary>
        /// The status color of <paramref name="value"/> between <paramref name="minimum"/> and
        /// <paramref name="maximum"/>: red at the low end, yellow towards the middle, green at the top.
        /// </summary>
        internal static Color GetStatusColor(int value, int minimum, int maximum)
        {
            double range = maximum - minimum;
            double fraction = range > 0 ? (value - minimum) / range : 0;
            fraction = Math.Max(0, Math.Min(1, fraction));

            if (fraction <= YellowBreakpoint)
                return Blend(Red, Yellow, fraction / YellowBreakpoint);

            return Blend(Yellow, Green, (fraction - YellowBreakpoint) / (1 - YellowBreakpoint));
        }

        /// <summary>The color the share <paramref name="t"/> (0-1) of the way from <paramref name="from"/> to <paramref name="to"/>, rounded.</summary>
        internal static Color Blend(Color from, Color to, double t)
        {
            t = Math.Max(0, Math.Min(1, t));

            return Color.FromArgb(
                (int)Math.Round(from.R + (to.R - from.R) * t),
                (int)Math.Round(from.G + (to.G - from.G) * t),
                (int)Math.Round(from.B + (to.B - from.B) * t));
        }

        /// <summary>
        /// Text of a disabled control on <paramref name="surface"/>: the best text color blended
        /// toward the surface, as on the buttons and the check box. The theme's
        /// <see cref="TextDisabled"/> is too faint on the light theme.
        /// </summary>
        internal static Color GetDisabledText(Color surface)
        {
            return BlendToward(GetContrastingForeColor(surface), surface, 0.35);
        }

        /// <summary>
        /// Quiet text on <paramref name="surface"/> (a count, a version): the best text color blended toward the surface by
        /// <paramref name="amount"/> (0-1) - but never so far that it falls below <paramref name="minimumContrast"/>, so it stays
        /// readable on every surface (a fixed blend is too faint on a light one or on a color).
        /// </summary>
        internal static Color GetMutedText(Color surface, double amount, double minimumContrast = 4.5)
        {
            Color text = GetBestTextColor(surface);
            Color muted = BlendToward(text, surface, amount);

            for (double reduced = amount; reduced > 0 && GetContrastRatio(muted, surface) < minimumContrast; )
            {
                reduced = Math.Max(0, reduced - 0.05);
                muted = BlendToward(text, surface, reduced);
            }

            return muted;
        }

        /// <summary>
        /// The color a given share (0-1) of the way from <paramref name="color"/>
        /// to <paramref name="target"/> - for muting a text toward its
        /// background.
        /// </summary>
        internal static Color BlendToward(Color color, Color target, double amount)
        {
            amount = Math.Max(0, Math.Min(1, amount));

            return Color.FromArgb(
                color.A,
                color.R + (int)((target.R - color.R) * amount),
                color.G + (int)((target.G - color.G) * amount),
                color.B + (int)((target.B - color.B) * amount));
        }

        private static Color Lighten(Color color, double amount)
        {
            return Color.FromArgb(
                color.R + (int)((255 - color.R) * amount),
                color.G + (int)((255 - color.G) * amount),
                color.B + (int)((255 - color.B) * amount));
        }

        // Flat RGB-unit variants - a different blend than the fractional
        // (0-1 "amount") private overloads just above, which this palette's
        // own generation uses to derive Primary/Secondary shades from the
        // accent. These exist for the much smaller "row N alternates with a
        // slightly darker shade of row N-1" case several controls
        // (ListBox, ListView, ReadOnlyDataGridView) each used to carry
        // their own identical copy of - consolidated here since the exact
        // same formula doesn't belong duplicated three times over.
        public static Color Darken(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Max(0, color.R - amount),
                Math.Max(0, color.G - amount),
                Math.Max(0, color.B - amount));
        }

        public static Color Lighten(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Min(255, color.R + amount),
                Math.Min(255, color.G + amount),
                Math.Min(255, color.B + amount));
        }
    }
}
