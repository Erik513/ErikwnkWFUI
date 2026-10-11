using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // Lets a control follow a change of the theme or the accent while it is shown: it listens to
    // UIColors.ThemeChanged from the moment it has a window until the window goes (so a control that is gone
    // is not kept alive by the static event), and does its update on its own thread.
    internal static class ThemeWatcher
    {
        // For a plain control that a factory colors (a panel): its color follows the theme as long as it still is the
        // color that was given to it - a color set by hand afterwards stays.
        public static void FollowBackColor(Control control, Func<Color> color)
        {
            Follow(control, color, () => control.BackColor, value => control.BackColor = value);
        }

        public static void FollowForeColor(Control control, Func<Color> color)
        {
            Follow(control, color, () => control.ForeColor, value => control.ForeColor = value);
        }

        private static void Follow(Control control, Func<Color> color, Func<Color> get, Action<Color> set)
        {
            Color given = color();
            set(given);

            Attach(control, () =>
            {
                if (get() != given)
                    return;

                given = color();
                set(given);
            });
        }

        public static void Attach(Control control, Action onThemeChanged)
        {
            EventHandler handler = null;
            bool listening = false;
            int seen = UIColors.ThemeVersion;

            handler = (sender, e) =>
            {
                if (control.IsDisposed || !control.IsHandleCreated)
                    return;

                seen = UIColors.ThemeVersion;

                if (control.InvokeRequired)
                    control.BeginInvoke(onThemeChanged);
                else
                    onThemeChanged();
            };

            Action listen = () =>
            {
                if (listening)
                    return;

                listening = true;
                UIColors.ThemeChanged += handler;

                // A control that had no window while the theme changed (never shown, or hidden) catches up now.
                if (seen != UIColors.ThemeVersion)
                {
                    seen = UIColors.ThemeVersion;
                    onThemeChanged();
                }
            };

            Action stop = () =>
            {
                if (!listening)
                    return;

                listening = false;
                UIColors.ThemeChanged -= handler;
            };

            control.HandleCreated += (sender, e) => listen();
            control.HandleDestroyed += (sender, e) => stop();
            control.Disposed += (sender, e) => stop();

            if (control.IsHandleCreated)
                listen();
        }
    }
}
