using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Factories
{
    internal static class UIProgressBarFactory
    {
        // Named by color, matching UIButtonFactory's CreateStandard/
        // CreatePrimary/CreateGreen/CreateRed convention. The border is a
        // property of the bar (ThemedProgressBar.ShowBorder), not a variant.
        // CreateStandard is a neutral gray (adjust via ForeColor), the same
        // meaning "Standard" has for buttons and sliders; with a color set the
        // bar is filled in the main color of the set (see FillOf). CreateGreen/
        // CreatePrimary are CreateStandard with the green and the accent set.
        public static ProgressBar CreateStandard(UIColorSet colors = null)
        {
            return Create(FillOf(ColorSetSource.Of(colors)));
        }

        // The fill of a bar for a color set: its Hover color. The sets of the library keep their main color there (Green is
        // UIColors.Green, Primary is the accent, a set made of a color has that color) - Basic is the darker shade made for
        // the text on a button, and would nearly vanish on the dark track. No set (null) is the neutral gray.
        // The set is asked for again each time: the primary set follows the accent.
        internal static Func<Color> FillOf(Func<UIColorSet> colors)
        {
            if (colors == null)
                return () => UIColors.DisabledGray;

            return () => colors().Hover;
        }

        public static ProgressBar CreateGreen()
        {
            return CreateStandard(UIStyles.Colors.Sets.Green);
        }

        // Same as CreateGreen, but the fill follows the app-wide accent
        // (UIColors.Primary, the same color CreatePrimary buttons use)
        // instead of the fixed green - for progress bars that should read
        // as "this app's own accent", not "success/in-progress" specifically.
        public static ProgressBar CreatePrimary()
        {
            return Create(FillOf(() => UIColors.PrimarySet));
        }

        // Fill color follows Value instead of being fixed: red at Minimum,
        // yellow at the midpoint, green at Maximum, blending smoothly
        // between them rather than snapping - for a "status/health" bar
        // where the color itself communicates good/bad, not just the fill
        // amount. Doesn't take SetEnabledStyle's restore-color path since
        // the color is recomputed fresh on every paint anyway (from Value
        // when enabled, DisabledGray when not) - there's no fixed color to
        // save and restore.
        public static ProgressBar CreateStatus()
        {
            return CreateStatusBar();
        }

        private static ProgressBar CreateStatusBar()
        {
            ProgressBar progressBar = new ThemedProgressBar(useStatusGradient: true)
            {
                Minimum = 0,
                Maximum = 100,
                Style = ProgressBarStyle.Continuous,
                Margin = new Padding(0)
            };

            ThemeWatcher.Attach(progressBar, progressBar.Invalidate); // the border and the status color are read while painting

            return progressBar;
        }

        private static ProgressBar Create(Func<Color> foreColor)
        {
            ProgressBar progressBar = new ThemedProgressBar()
            {
                Minimum = 0,
                Style = ProgressBarStyle.Continuous,
                // BackgroundMedium rather than BackgroundLight - a progress
                // bar's track needs to read as a recessed surface, not
                // whatever shade the caller's panel happens to use (several
                // consumers set their panel BackColor to BackgroundLight too,
                // which made the track invisible - the fill looked like it
                // was floating on transparent background). BackgroundDark
                // fixed that but read as near-black, harsher than the rest
                // of the library's dark grays; BackgroundMedium is still
                // reliably a shade below the common panel roles
                // (Light/Lighter/Elevated) without going that dark, and the
                // border (while ShowBorder is on) covers the remaining
                // edge case where a panel is BackgroundMedium too. Callers
                // wanting the borderless/blend-in look turn ShowBorder off and
                // set BackColor to Color.Transparent: the track then takes the
                // color of the panel (ThemedProgressBar does that; a native
                // ProgressBar cannot be transparent).
                Margin = new Padding(0)
            };

            SetEnabledStyle(progressBar, foreColor);

            return progressBar;
        }

        // Like UIButtonFactory/UICheckBoxFactory's SetEnabledStyle - restores
        // whatever ForeColor was live right before Enabled went false, so a
        // caller's manual fill-color override survives the round-trip.
        // Previously ProgressBar had no disabled-state handling at all
        // (confirmed identical pixels enabled vs. disabled via screenshot).
        //
        // Disabled fill uses UIColors.DisabledGray - a plain darkened
        // version of whatever the current fill color happened to be
        // (accent green, or a caller's own override) still read as "that
        // same color, just dimmer", not "disabled/inactive", inconsistent
        // with every other disabled control in the library (buttons,
        // CheckBox, ToggleSwitch, Labels all resolve to this same fixed
        // gray). Disabled progress bars are a rare case in practice, but
        // when one does show up it should read as gray like everything
        // else, not a dark shade of its own accent.
        //
        // The fill color follows the theme as long as it is still the one the factory gave (or, while disabled,
        // the one it would restore): a color set by hand stays.
        private static void SetEnabledStyle(ProgressBar progressBar, Func<Color> foreColor)
        {
            Color given = foreColor();
            Color restoreForeColor = given;
            progressBar.ForeColor = given;

            progressBar.EnabledChanged += delegate
            {
                if (progressBar.Enabled)
                {
                    progressBar.ForeColor = restoreForeColor;
                }
                else
                {
                    restoreForeColor = progressBar.ForeColor;
                    progressBar.ForeColor = UIColors.DisabledGray;
                }
            };

            ThemeWatcher.Attach(progressBar, () =>
            {
                if (progressBar.Enabled)
                {
                    if (progressBar.ForeColor == given)
                        progressBar.ForeColor = foreColor();
                }
                else
                {
                    if (restoreForeColor == given)
                        restoreForeColor = foreColor();

                    progressBar.ForeColor = UIColors.DisabledGray;
                }

                given = foreColor();
                progressBar.Invalidate();
            });
        }
    }
}
