using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;
using ProgressBar = System.Windows.Forms.ProgressBar;

namespace ErikwnkWFUI.Factories
{
    internal static class UIProgressBarFactory
    {
        // Named by color, matching UIButtonFactory's CreateStandard/
        // CreatePrimary/CreateGreen/CreateRed convention. The border is a
        // property of the bar (ProgressBar.BorderStyle), not a variant.
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

        // The fill follows Value: red at Minimum, yellow at the midpoint, green at Maximum, blended in between (a "status/health" bar,
        // where the color says good or bad). The bar works the color out at every paint (from Value when enabled, DisabledGray when
        // not), so there is no fixed fill color to save and restore as in SetEnabledStyle.
        public static ProgressBar CreateStatus()
        {
            return CreateStatusBar();
        }

        private static ProgressBar CreateStatusBar()
        {
            ProgressBar progressBar = new Controls.ProgressBar(useStatusGradient: true)
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
            ProgressBar progressBar = new Controls.ProgressBar()
            {
                Minimum = 0,
                Style = ProgressBarStyle.Continuous,
                // The track is BackgroundMedium (the default of ProgressBar.BackColor): a recessed surface that stays visible on the
                // usual panel shades, and the border covers a panel of the same shade. BorderStyle and BackColor change that; a
                // BackColor of Transparent takes the color of the panel.
                Margin = new Padding(0)
            };

            SetEnabledStyle(progressBar, foreColor);

            return progressBar;
        }

        // Like UIButtonFactory/UICheckBoxFactory's SetEnabledStyle: a disabled bar is filled in the one gray of the library
        // (DisabledGray), as every disabled control is, and the fill color that was live before comes back when it is enabled again,
        // so a color set by hand survives. The fill follows the theme as long as it is the one the factory gave (or, while disabled,
        // the one it would restore).
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
