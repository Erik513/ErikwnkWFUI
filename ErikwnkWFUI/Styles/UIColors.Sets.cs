using System;
using System.Drawing;

namespace ErikwnkWFUI.Styles
{
    // The color sets: one color in, every state out.
    public static partial class UIColors
    {
        /// <summary>
        /// The yellow look: white text that stays white in every state and in both
        /// themes. The disabled yellow is the resting one darkened as far as any
        /// dark fill gets.
        /// </summary>
        internal static readonly UIColorSet YellowSet = CreateYellowSet();

        private static UIColorSet CreateYellowSet()
        {
            Color disabled = DarkenByShare(Yellow, DisabledDarkenAmount);

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

        /// <summary>
        /// The look of the accent color: darker at rest, the accent hovered, lighter pressed. A bright, colored accent (yellow, amber) is not
        /// darkened by a third at rest - that would make it olive - but is the accent itself, darkened only as far as white text needs (2.4:1),
        /// and gets lighter when hovered and pressed: the look of <c>Sets.Yellow</c>.
        /// </summary>
        internal static UIColorSet PrimarySet
        {
            get
            {
                Color accent = Primary;

                if (IsBrightAccent(accent))
                {
                    Color rest = GetReadableFill(accent, LightForeColor, BrightAccentTextRatio);

                    return CreateSet(rest, ShiftLightness(rest, 0.07), ShiftLightness(rest, 0.14));
                }

                return CreateSet(PrimaryDark, Primary, PrimaryLight);
            }
        }

        private const double BrightAccentTextRatio = 2.4;

        private static bool IsBrightAccent(Color accent)
        {
            return !IsNeutral(accent) && IsLight(accent);
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

            return CreateSet(ShiftLightness(color, IsNeutral(color) ? -0.06 : -0.12), color, ShiftLightness(color, 0.12));
        }

        /// <summary>
        /// A color set from the three fills you want; the text colors (the
        /// ones that read best on each fill) and the disabled look are worked
        /// out from them.
        /// </summary>
        internal static UIColorSet CreateSet(Color basic, Color hover, Color pressed)
        {
            Color disabled = DarkenByShare(basic, IsLight(basic) ? LightDisabledDarkenAmount : DisabledDarkenAmount);
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
            Color disabled = DarkenByShare(basic, IsLight(basic) ? LightDisabledDarkenAmount : DisabledDarkenAmount);

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
    }
}
