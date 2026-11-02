using System;
using System.Drawing;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// Read-only access to every color used across ErikwnkWFUI's controls,
        /// plus <see cref="SetAccent"/>/<see cref="ApplyTheme"/> to customize
        /// them. Defaults to a dark gray theme with a blue accent; nothing
        /// here needs to be set unless you want to change that.
        /// </summary>
        public static class Colors
        {
            /// <summary>Pure black. Fixed: the same in every theme.</summary>
            public static Color Black { get { return UIColors.Black; } }

            /// <summary>The deepest surface of the theme, behind everything else (the page of a window).</summary>
            public static Color BackgroundBlack { get { return UIColors.BackgroundBlack; } }
            /// <summary>The usual dark surface of a window or a panel.</summary>
            public static Color BackgroundDark { get { return UIColors.BackgroundDark; } }
            /// <summary>A surface slightly raised from <see cref="BackgroundDark"/>.</summary>
            public static Color BackgroundDarkElevated { get { return UIColors.BackgroundDarkElevated; } }
            /// <summary>The surface of the standard control: a text field, a list, a track.</summary>
            public static Color BackgroundMedium { get { return UIColors.BackgroundMedium; } }
            /// <summary>A surface slightly raised from <see cref="BackgroundMedium"/>.</summary>
            public static Color BackgroundMediumElevated { get { return UIColors.BackgroundMediumElevated; } }
            /// <summary>A raised surface: a hovered control, a header.</summary>
            public static Color BackgroundLight { get { return UIColors.BackgroundLight; } }
            /// <summary>The most raised or active surface of the theme.</summary>
            public static Color BackgroundLighter { get { return UIColors.BackgroundLighter; } }

            /// <summary>The darkest shade of the accent (a panel in the color of the accent). Follows <see cref="SetAccent"/>.</summary>
            public static Color PrimaryDarkDark { get { return UIColors.PrimaryDarkDark; } }
            /// <summary>The accent, darkened: the resting fill of a primary control. Follows <see cref="SetAccent"/>.</summary>
            public static Color PrimaryDark { get { return UIColors.PrimaryDark; } }
            /// <summary>The accent color itself. Follows <see cref="SetAccent"/>.</summary>
            public static Color Primary { get { return UIColors.Primary; } }
            /// <summary>The accent, lightened: the pressed fill and highlights. Follows <see cref="SetAccent"/>.</summary>
            public static Color PrimaryLight { get { return UIColors.PrimaryLight; } }

            /// <summary>A slightly darker shade of the accent. Follows <see cref="SetAccent"/>.</summary>
            public static Color SecondaryDark { get { return UIColors.SecondaryDark; } }
            /// <summary>A slightly lighter shade of the accent. Follows <see cref="SetAccent"/>.</summary>
            public static Color Secondary { get { return UIColors.Secondary; } }
            /// <summary>A lighter shade of the accent. Follows <see cref="SetAccent"/>.</summary>
            public static Color SecondaryLight { get { return UIColors.SecondaryLight; } }

            /// <summary>The dark green of the fixed green family (the same in every theme).</summary>
            public static Color GreenDark { get { return UIColors.GreenDark; } }
            /// <summary>The green of the fixed green family (the same in every theme).</summary>
            public static Color Green { get { return UIColors.Green; } }
            /// <summary>The light green of the fixed green family (the same in every theme).</summary>
            public static Color GreenLight { get { return UIColors.GreenLight; } }
            /// <summary>The lightest green of the fixed green family (the same in every theme).</summary>
            public static Color GreenLighter { get { return UIColors.GreenLighter; } }

            /// <summary>The dark yellow of the fixed yellow family (the same in every theme).</summary>
            public static Color YellowDark { get { return UIColors.YellowDark; } }
            /// <summary>The yellow of the fixed yellow family (the same in every theme).</summary>
            public static Color Yellow { get { return UIColors.Yellow; } }
            /// <summary>The light yellow of the fixed yellow family (the same in every theme).</summary>
            public static Color YellowLight { get { return UIColors.YellowLight; } }
            /// <summary>The lightest yellow of the fixed yellow family (the same in every theme).</summary>
            public static Color YellowLighter { get { return UIColors.YellowLighter; } }

            /// <summary>The dark red of the fixed red family (the same in every theme).</summary>
            public static Color RedDark { get { return UIColors.RedDark; } }
            /// <summary>The red of the fixed red family (the same in every theme).</summary>
            public static Color Red { get { return UIColors.Red; } }
            /// <summary>The light red of the fixed red family (the same in every theme).</summary>
            public static Color RedLight { get { return UIColors.RedLight; } }

            /// <summary>Pure white. Fixed: the same in every theme.</summary>
            public static Color White { get { return UIColors.White; } }
            /// <summary>The main text color of the theme.</summary>
            public static Color TextPrimary { get { return UIColors.TextPrimary; } }
            /// <summary>The main text color, a little dimmed.</summary>
            public static Color TextPrimaryDim { get { return UIColors.TextPrimaryDim; } }
            /// <summary>Secondary text: captions, hints.</summary>
            public static Color TextSecondary { get { return UIColors.TextSecondary; } }
            /// <summary>The quietest regular text.</summary>
            public static Color TextTertiary { get { return UIColors.TextTertiary; } }
            /// <summary>Text of a disabled control. (On the light theme it is faint: the controls work their own disabled text out with <c>GetDisabledText</c>.)</summary>
            public static Color TextDisabled { get { return UIColors.TextDisabled; } }
            /// <summary>Muted text: a count, a version.</summary>
            public static Color TextMuted { get { return UIColors.TextMuted; } }

            /// <summary>The darkest border or line of the theme.</summary>
            public static Color BorderDark { get { return UIColors.BorderDark; } }
            /// <summary>The usual border or line of the theme.</summary>
            public static Color BorderMedium { get { return UIColors.BorderMedium; } }
            /// <summary>The lightest border or line of the theme (a hovered border).</summary>
            public static Color BorderLight { get { return UIColors.BorderLight; } }
            /// <summary>The border in the color of the accent. Follows <see cref="SetAccent"/>.</summary>
            public static Color BorderPrimary { get { return UIColors.BorderPrimary; } }
            /// <summary>A red border (an entry that was not right). Fixed: the same in every theme.</summary>
            public static Color BorderRed { get { return UIColors.BorderRed; } }

            /// <summary>The text or icon color on an accent-colored surface: white, or dark on a very bright accent. Follows <see cref="SetAccent"/>.</summary>
            public static Color AccentForeColor { get { return UIColors.AccentForeColor; } }

            /// <summary>
            /// A color set made from any color: the color is what a control shows
            /// while hovered, the fill at rest is a darker shade and pressed a
            /// lighter one (a light color such as yellow keeps its look at rest and
            /// gets darker instead). Text and disabled colors are worked out so the
            /// text stays readable on every fill. Pass the result to a control that
            /// takes a <see cref="UIColorSet"/>.
            /// </summary>
            public static UIColorSet CreateSet(Color color)
            {
                return UIColors.CreateSet(color);
            }

            /// <summary>
            /// A color set from the three fills you want (at rest, hovered, pressed);
            /// the text and disabled colors are worked out from them.
            /// </summary>
            public static UIColorSet CreateSet(Color basic, Color hover, Color pressed)
            {
                return UIColors.CreateSet(basic, hover, pressed);
            }

            /// <summary>
            /// Like <see cref="CreateSet(Color, Color, Color)"/>, but the text has the one
            /// color you give in every state (muted when disabled) instead of the best one
            /// for each fill - for a look whose text color is fixed.
            /// </summary>
            public static UIColorSet CreateSet(Color basic, Color hover, Color pressed, Color text)
            {
                return UIColors.CreateSet(basic, hover, pressed, text);
            }

            /// <summary>
            /// Ready-made color sets for the common colors, for when a control should
            /// not use the standard or the primary look. Each one has a fill at rest,
            /// hovered, pressed and disabled plus matching text colors (see
            /// <see cref="UIColorSet"/>). All but <see cref="Primary"/> are the same in
            /// Dark and Light and ignore the accent. To tune one, replace it with your
            /// own <see cref="CreateSet(Color, Color, Color)"/>.
            /// </summary>
            public static class Sets
            {
                /// <summary>The accent color's own look; changes with <see cref="SetAccent"/>.</summary>
                public static UIColorSet Primary
                {
                    get { return UIColors.PrimarySet; }
                }

                /// <summary>A red look: dark red at rest, red when hovered, light red when pressed.</summary>
                public static UIColorSet Red { get; } = UIColors.CreateSet(UIColors.RedDark, UIColors.Red, UIColors.RedLight);
                /// <summary>An orange look: dark orange at rest, orange when hovered, light orange when pressed.</summary>
                public static UIColorSet Orange { get; } = UIColors.CreateSet(Color.FromArgb(154, 81, 15), Color.FromArgb(210, 110, 20), Color.FromArgb(236, 141, 55));
                // White text on every state, even on the bright fills.
                /// <summary>A yellow look with white text in every state, even on the bright fills.</summary>
                public static UIColorSet Yellow { get; } = UIColors.YellowSet;
                /// <summary>A green look: dark green at rest, green when hovered, light green when pressed.</summary>
                public static UIColorSet Green { get; } = UIColors.CreateSet(UIColors.GreenDark, UIColors.Green, UIColors.GreenLight);
                /// <summary>A teal look: dark teal at rest, teal when hovered, light teal when pressed.</summary>
                public static UIColorSet Teal { get; } = UIColors.CreateSet(Color.FromArgb(0, 69, 60), Color.FromArgb(0, 130, 114), Color.FromArgb(0, 191, 168));
                /// <summary>A blue look: the default blue of the library, fixed (it does not follow the accent, unlike <see cref="Primary"/>).</summary>
                public static UIColorSet Blue { get; } = UIColors.CreateSet(Color.FromArgb(0, 55, 97), Color.FromArgb(0, 90, 158), Color.FromArgb(0, 125, 219));
                /// <summary>A purple look: dark purple at rest, purple when hovered, light purple when pressed.</summary>
                public static UIColorSet Purple { get; } = UIColors.CreateSet(Color.FromArgb(81, 44, 125), Color.FromArgb(110, 60, 170), Color.FromArgb(141, 93, 198));
                /// <summary>A pink look: dark pink at rest, pink when hovered, light pink when pressed.</summary>
                public static UIColorSet Pink { get; } = UIColors.CreateSet(Color.FromArgb(155, 44, 83), Color.FromArgb(200, 60, 110), Color.FromArgb(213, 108, 145));
                /// <summary>A brown look: dark brown at rest, brown when hovered, light brown when pressed.</summary>
                public static UIColorSet Brown { get; } = UIColors.CreateSet(Color.FromArgb(75, 47, 28), Color.FromArgb(120, 75, 45), Color.FromArgb(165, 103, 62));
                /// <summary>
                /// The dark gray of <see cref="Buttons.CreateStandard"/> in the dark theme - but fixed:
                /// it stays dark in the light theme too and ignores the accent.
                /// </summary>
                public static UIColorSet Dark { get; } = UIColors.CreateSet(Color.FromArgb(35, 35, 35), Color.FromArgb(60, 60, 60), Color.FromArgb(90, 90, 90));

                /// <summary>A neutral gray look, the same in both themes.</summary>
                public static UIColorSet Gray { get; } = UIColors.CreateSet(Color.FromArgb(75, 75, 75), Color.FromArgb(90, 90, 90), Color.FromArgb(121, 121, 121));
            }

            /// <summary>The see-through tint laid over a surface while the mouse is over it.</summary>
            public static Color HoverOverlay { get { return UIColors.HoverOverlay; } }
            /// <summary>The see-through tint laid over a surface while it is pressed.</summary>
            public static Color ActiveOverlay { get { return UIColors.ActiveOverlay; } }
            /// <summary>The see-through tint of a selected row, in the color of the accent. Follows <see cref="SetAccent"/>.</summary>
            public static Color Selection { get { return UIColors.Selection; } }

            /// <summary>Fully transparent.</summary>
            public static Color Transparent { get { return UIColors.Transparent; } }
            /// <summary>A strong black veil (alpha 180), for dimming what lies behind a window.</summary>
            public static Color OverlayDark { get { return UIColors.OverlayDark; } }
            /// <summary>A medium black veil (alpha 120).</summary>
            public static Color OverlayMedium { get { return UIColors.OverlayMedium; } }
            /// <summary>A light black veil (alpha 60).</summary>
            public static Color OverlayLight { get { return UIColors.OverlayLight; } }

            /// <summary>
            /// Replaces the blue accent used throughout every built-in
            /// control (buttons, toggle switches, selection highlights,
            /// ...) with shades computed from a single color. May be called at
            /// any time: the controls that exist follow the change by
            /// themselves (see <see cref="ThemeChanged"/>).
            /// </summary>
            public static void SetAccent(Color accent)
            {
                UIColors.SetAccent(accent);
            }

            /// <summary>
            /// Switches the base theme (backgrounds/text/borders), e.g.
            /// UIThemes.Light to move off the dark default. May be called at
            /// any time: the controls that exist follow the change by
            /// themselves (a color set on a control by hand stays). Combine
            /// freely with SetAccent - the two are independent (e.g. a light
            /// theme with a purple accent).
            /// </summary>
            public static void ApplyTheme(UIColorTheme theme)
            {
                UIColors.ApplyTheme(theme);
            }

            /// <summary>
            /// Raised after the theme or the accent has changed. The controls of the library follow it by
            /// themselves (a color set on a control by hand stays), so an application only needs this for what it
            /// draws or colors itself.
            /// </summary>
            public static event EventHandler ThemeChanged
            {
                add { UIColors.ThemeChanged += value; }
                remove { UIColors.ThemeChanged -= value; }
            }
        }
    }
}
