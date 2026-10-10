using System;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIToolTipFactory
    {
        public static ToolTip CreateToolTip(
            string text = "")
        {
            ToolTip toolTip = new ToolTip
            {
                ToolTipTitle = text ?? "",
                BackColor = UIColors.BackgroundMedium,
                ForeColor = UIColors.TextPrimary,
                AutoPopDelay = 10000,
                InitialDelay = 1000,
                ReshowDelay = 300,
                UseAnimation = true,
                UseFading = true
            };

            FollowTheme(toolTip);

            return toolTip;
        }

        // A tooltip is not a control with a window of its own, so it listens as long as it exists. Its colors follow
        // the theme while they still are the ones the factory gave; a color set by hand stays.
        private static void FollowTheme(ToolTip toolTip)
        {
            System.Drawing.Color back = toolTip.BackColor;
            System.Drawing.Color fore = toolTip.ForeColor;
            System.Threading.SynchronizationContext context = System.Threading.SynchronizationContext.Current;
            EventHandler handler = null;

            handler = (sender, e) =>
            {
                System.Threading.SendOrPostCallback update = state =>
                {
                    if (toolTip.BackColor == back)
                        toolTip.BackColor = UIColors.BackgroundMedium;

                    if (toolTip.ForeColor == fore)
                        toolTip.ForeColor = UIColors.TextPrimary;

                    back = UIColors.BackgroundMedium;
                    fore = UIColors.TextPrimary;
                };

                if (context != null && System.Threading.SynchronizationContext.Current != context)
                    context.Post(update, null);
                else
                    update(null);
            };

            UIColors.ThemeChanged += handler;
            toolTip.Disposed += delegate { UIColors.ThemeChanged -= handler; };
        }

        // The plain tooltip a control shows for itself on hover: standard
        // look, short delay, and kept working after the window loses and
        // regains focus.
        public static ToolTip CreateHoverToolTip(Control owner)
        {
            ToolTip toolTip = new ToolTip
            {
                InitialDelay = 500,
                ReshowDelay = 100,
                AutoPopDelay = 5000
            };

            toolTip.ReviveOnFormActivate(owner);
            return toolTip;
        }
    }
}