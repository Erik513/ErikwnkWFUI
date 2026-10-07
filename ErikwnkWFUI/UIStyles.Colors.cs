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
            public static Color Black { get { return UIColors.Black; } }

            public static Color BackgroundBlack { get { return UIColors.BackgroundBlack; } }
            public static Color BackgroundDark { get { return UIColors.BackgroundDark; } }
            public static Color BackgroundDarkElevated { get { return UIColors.BackgroundDarkElevated; } }
            public static Color BackgroundMedium { get { return UIColors.BackgroundMedium; } }
            public static Color BackgroundMediumElevated { get { return UIColors.BackgroundMediumElevated; } }
            public static Color BackgroundLight { get { return UIColors.BackgroundLight; } }
            public static Color BackgroundLighter { get { return UIColors.BackgroundLighter; } }

            public static Color PrimaryDarkDark { get { return UIColors.PrimaryDarkDark; } }
            public static Color PrimaryDark { get { return UIColors.PrimaryDark; } }
            public static Color Primary { get { return UIColors.Primary; } }
            public static Color PrimaryLight { get { return UIColors.PrimaryLight; } }

            public static Color SecondaryDark { get { return UIColors.SecondaryDark; } }
            public static Color Secondary { get { return UIColors.Secondary; } }
            public static Color SecondaryLight { get { return UIColors.SecondaryLight; } }

            public static Color GreenDark { get { return UIColors.GreenDark; } }
            public static Color Green { get { return UIColors.Green; } }
            public static Color GreenLight { get { return UIColors.GreenLight; } }
            public static Color GreenLighter { get { return UIColors.GreenLighter; } }

            public static Color YellowDark { get { return UIColors.YellowDark; } }
            public static Color Yellow { get { return UIColors.Yellow; } }
            public static Color YellowLight { get { return UIColors.YellowLight; } }
            public static Color YellowLighter { get { return UIColors.YellowLighter; } }

            public static Color RedDark { get { return UIColors.RedDark; } }
            public static Color Red { get { return UIColors.Red; } }
            public static Color RedLight { get { return UIColors.RedLight; } }

            public static Color White { get { return UIColors.White; } }
            public static Color TextPrimary { get { return UIColors.TextPrimary; } }
            public static Color TextPrimaryDim { get { return UIColors.TextPrimaryDim; } }
            public static Color TextSecondary { get { return UIColors.TextSecondary; } }
            public static Color TextTertiary { get { return UIColors.TextTertiary; } }
            public static Color TextDisabled { get { return UIColors.TextDisabled; } }
            public static Color TextMuted { get { return UIColors.TextMuted; } }

            public static Color BorderDark { get { return UIColors.BorderDark; } }
            public static Color BorderMedium { get { return UIColors.BorderMedium; } }
            public static Color BorderLight { get { return UIColors.BorderLight; } }
            public static Color BorderPrimary { get { return UIColors.BorderPrimary; } }
            public static Color BorderRed { get { return UIColors.BorderRed; } }

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

                public static UIColorSet Red { get; } = UIColors.CreateSet(UIColors.RedDark, UIColors.Red, UIColors.RedLight);
                public static UIColorSet Orange { get; } = UIColors.CreateSet(Color.FromArgb(154, 81, 15), Color.FromArgb(210, 110, 20), Color.FromArgb(236, 141, 55));
                // White text on every state, even on the bright fills.
                public static UIColorSet Yellow { get; } = UIColors.YellowSet;
                public static UIColorSet Green { get; } = UIColors.CreateSet(UIColors.GreenDark, UIColors.Green, UIColors.GreenLight);
                public static UIColorSet Teal { get; } = UIColors.CreateSet(Color.FromArgb(0, 69, 60), Color.FromArgb(0, 130, 114), Color.FromArgb(0, 191, 168));
                public static UIColorSet Blue { get; } = UIColors.CreateSet(Color.FromArgb(0, 55, 97), Color.FromArgb(0, 90, 158), Color.FromArgb(0, 125, 219));
                public static UIColorSet Purple { get; } = UIColors.CreateSet(Color.FromArgb(81, 44, 125), Color.FromArgb(110, 60, 170), Color.FromArgb(141, 93, 198));
                public static UIColorSet Pink { get; } = UIColors.CreateSet(Color.FromArgb(155, 44, 83), Color.FromArgb(200, 60, 110), Color.FromArgb(213, 108, 145));
                public static UIColorSet Brown { get; } = UIColors.CreateSet(Color.FromArgb(75, 47, 28), Color.FromArgb(120, 75, 45), Color.FromArgb(165, 103, 62));
                /// <summary>
                /// The dark gray of <see cref="Buttons.CreateStandard"/> in the dark theme - but fixed:
                /// it stays dark in the light theme too and ignores the accent.
                /// </summary>
                public static UIColorSet Dark { get; } = UIColors.CreateSet(Color.FromArgb(35, 35, 35), Color.FromArgb(60, 60, 60), Color.FromArgb(90, 90, 90));

                public static UIColorSet Gray { get; } = UIColors.CreateSet(Color.FromArgb(75, 75, 75), Color.FromArgb(90, 90, 90), Color.FromArgb(121, 121, 121));
            }

            public static Color HoverOverlay { get { return UIColors.HoverOverlay; } }
            public static Color ActiveOverlay { get { return UIColors.ActiveOverlay; } }
            public static Color Selection { get { return UIColors.Selection; } }

            public static Color Transparent { get { return UIColors.Transparent; } }
            public static Color OverlayDark { get { return UIColors.OverlayDark; } }
            public static Color OverlayMedium { get { return UIColors.OverlayMedium; } }
            public static Color OverlayLight { get { return UIColors.OverlayLight; } }

            /// <summary>
            /// Replaces the blue accent used throughout every built-in
            /// control (buttons, toggle switches, selection highlights,
            /// ...) with shades computed from a single color. Set once, as
            /// early as possible - before building any UI - same as
            /// <see cref="Language"/>; a couple of controls only read their
            /// accent color once at construction time, so they won't
            /// retroactively pick up a change made after they're built.
            /// </summary>
            public static void SetAccent(Color accent)
            {
                UIColors.SetAccent(accent);
            }

            /// <summary>
            /// Switches the base theme (backgrounds/text/borders), e.g.
            /// UIThemes.Light to move off the dark default. Same "call once,
            /// before building any UI" caveat as <see cref="SetAccent"/>. Combine
            /// freely with SetAccent - the two are independent (e.g. a light
            /// theme with a purple accent).
            /// </summary>
            public static void ApplyTheme(UIColorTheme theme)
            {
                UIColors.ApplyTheme(theme);
            }
        }
    }
}
