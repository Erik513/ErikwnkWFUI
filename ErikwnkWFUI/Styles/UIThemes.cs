using System.Drawing;

namespace ErikwnkWFUI.Styles
{
    /// <summary>
    /// Ready-to-use base themes for <see cref="UIColors.ApplyTheme"/>. Dark
    /// matches ErikwnkWFUI's original, always-has-been-the-default palette
    /// exactly (applying it is a no-op unless something else already changed
    /// the theme). Light is a from-scratch light palette, not a per-channel
    /// inversion of Dark - see the note on <see cref="UIColorTheme"/> about
    /// why the same role can hold a lighter or darker RGB value depending on
    /// which theme is active.
    /// </summary>
    public static class UIThemes
    {
        public static readonly UIColorTheme Dark = new UIColorTheme();

        public static readonly UIColorTheme Light = new UIColorTheme
        {
            // A genuine, monotonically increasing staircase toward white,
            // mirroring Dark's own strictly-increasing 10/20/25/35/40/50/60
            // ladder - just inverted, since a light theme's "base" surface
            // is a visible off-white/gray and its most "elevated" surface
            // is the one that reaches actual white, not the other way
            // around. The previous values here weren't a staircase at all
            // (BackgroundDark and BackgroundMedium were both pure white,
            // BackgroundDarkElevated and BackgroundLight were identical) -
            // confirmed live as controls sitting directly on the app's own
            // background (also near-white) becoming nearly invisible with
            // no border to fall back on, e.g. ListBoxControl's header
            // panel (BackgroundDark) against the Showcase's own
            // BackgroundBlack. Every role here is now a distinct value,
            // and only the very last one (BackgroundLighter) is actual
            // pure white - nothing else has nowhere left to go.
            BackgroundBlack = Color.FromArgb(225, 225, 225),
            BackgroundDark = Color.FromArgb(236, 236, 236),
            BackgroundDarkElevated = Color.FromArgb(243, 243, 243),
            BackgroundMedium = Color.FromArgb(248, 248, 248),
            BackgroundMediumElevated = Color.FromArgb(251, 251, 251),
            BackgroundLight = Color.FromArgb(253, 253, 253),
            BackgroundLighter = Color.FromArgb(255, 255, 255),

            TextPrimary = Color.FromArgb(20, 20, 20),
            TextPrimaryDim = Color.FromArgb(45, 45, 45),
            TextSecondary = Color.FromArgb(90, 90, 90),
            TextTertiary = Color.FromArgb(120, 120, 120),
            TextDisabled = Color.FromArgb(170, 170, 170),
            TextMuted = Color.FromArgb(140, 140, 140),

            BorderDark = Color.FromArgb(220, 220, 220),
            BorderMedium = Color.FromArgb(200, 200, 200),
            BorderLight = Color.FromArgb(180, 180, 180),

            // A translucent dark tint reads fine as a hover/press cue on a
            // light surface too, so these stay the same as Dark's.
            HoverOverlay = Color.FromArgb(30, 30, 30, 80),
            ActiveOverlay = Color.FromArgb(40, 40, 40, 120)
        };
    }
}
