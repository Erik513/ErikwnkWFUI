using System;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // Lets a control follow a change of the theme or the accent while it is shown: it listens to
    // UIColors.ThemeChanged from the moment it has a window until the window goes (so a control that is gone
    // is not kept alive by the static event), and does its update on its own thread.
    internal static class ThemeWatcher
    {
        public static void Attach(Control control, Action onThemeChanged)
        {
            EventHandler handler = null;
            bool listening = false;

            handler = (sender, e) =>
            {
                if (control.IsDisposed || !control.IsHandleCreated)
                    return;

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
