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
        // CreatePrimary/CreateGreen/CreateRed convention - "Transparent" is
        // a uniform suffix for the no-border/blend-in variant of each color.
        // CreateStandard is a neutral grey (adjust via ForeColor), the same
        // meaning "Standard" has for buttons and sliders; CreateGreen/
        // CreatePrimary just preset a specific fill instead.
        public static ProgressBar CreateStandard()
        {
            return Create(() => UIColors.DisabledGray, drawBorder: true);
        }

        // CreateStandard's grey fill with CreateGreenBorderless's
        // no-border/blend-in look.
        public static ProgressBar CreateStandardBorderless()
        {
            return Create(() => UIColors.DisabledGray, drawBorder: false);
        }

        public static ProgressBar CreateGreen()
        {
            return Create(() => UIColors.Green, drawBorder: true);
        }

        // No border, and BackColor deliberately left as whatever the caller
        // sets - ProgressBar.BackColor throws on Color.Transparent (there is
        // no real transparency for this control), so the only way to get the
        // "blends into its container" look some callers actually want is to
        // match BackColor to the surrounding panel by hand, e.g.
        // progressBar.BackColor = UIStyles.Colors.BackgroundLight to match a
        // BackgroundLight panel. CreateGreen's opinionated, always-visible
        // track/border stays the default; this is the explicit opt-out.
        public static ProgressBar CreateGreenBorderless()
        {
            return Create(() => UIColors.Green, drawBorder: false);
        }

        // Same as CreateGreen, but the fill follows the app-wide accent
        // (UIColors.Primary, the same color CreatePrimary buttons use)
        // instead of the fixed green - for progress bars that should read
        // as "this app's own accent", not "success/in-progress" specifically.
        public static ProgressBar CreatePrimary()
        {
            return Create(() => UIColors.Primary, drawBorder: true);
        }

        // CreatePrimary's accent-following fill, with CreateGreenBorderless's
        // no-border/blend-in look.
        public static ProgressBar CreatePrimaryBorderless()
        {
            return Create(() => UIColors.Primary, drawBorder: false);
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
            return CreateStatusBar(drawBorder: true);
        }

        // CreateStatus's Value-driven fill, with CreateGreenBorderless's
        // no-border/blend-in look.
        public static ProgressBar CreateStatusBorderless()
        {
            return CreateStatusBar(drawBorder: false);
        }

        private static ProgressBar CreateStatusBar(bool drawBorder)
        {
            ProgressBar progressBar = new ThemedProgressBar(drawBorder, useStatusGradient: true)
            {
                Minimum = 0,
                Maximum = 100,
                Style = ProgressBarStyle.Continuous,
                Margin = new Padding(0)
            };

            ThemeWatcher.FollowBackColor(progressBar, () => UIColors.BackgroundMedium);
            ThemeWatcher.Attach(progressBar, progressBar.Invalidate); // the border and the status color are read while painting

            return progressBar;
        }

        private static ProgressBar Create(Func<Color> foreColor, bool drawBorder)
        {
            ProgressBar progressBar = new ThemedProgressBar(drawBorder)
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
                // border (when drawBorder is true) covers the remaining
                // edge case where a panel is BackgroundMedium too. Callers
                // wanting the borderless/blend-in look reassign this
                // themselves - see CreateTransparent's own comment.
                Margin = new Padding(0)
            };

            ThemeWatcher.FollowBackColor(progressBar, () => UIColors.BackgroundMedium);
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
