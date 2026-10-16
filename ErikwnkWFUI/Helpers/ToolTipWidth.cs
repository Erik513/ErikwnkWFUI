using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// The widest line of a <see cref="ToolTip"/>: a longer text is wrapped into lines of that width (a standard tooltip shows one
    /// line as wide as the screen). The window of a tooltip is made anew whenever it is switched off and on (which
    /// <see cref="ToolTipActivationFix"/> does when a form is activated), and the size of a tooltip is worked out before it tells
    /// anybody that it is about to be shown, so the width is set when the limit is given, again after each re-arming, and once more
    /// before each tooltip is shown.
    /// </summary>
    internal static class ToolTipWidth
    {
        /// <summary>The widest a line of a tooltip of the library is, in pixels at 96 dpi; a longer text is wrapped.</summary>
        internal const int MaxWidthAt96Dpi = 400;

        private sealed class WidthLimit
        {
            public int Width;
        }

        // The window of a ToolTip is not public: the property is found once, by name (it exists in .NET Framework and in .NET).
        private static readonly PropertyInfo HandleProperty = typeof(ToolTip).GetProperty("Handle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly ConditionalWeakTable<ToolTip, WidthLimit> Limits = new ConditionalWeakTable<ToolTip, WidthLimit>();

        /// <summary>Limits the lines of the tooltip to this many pixels; another call changes the limit.</summary>
        public static void Set(ToolTip toolTip, int width)
        {
            if (Limits.TryGetValue(toolTip, out WidthLimit limit))
            {
                limit.Width = width;
            }
            else
            {
                Limits.Add(toolTip, new WidthLimit { Width = width });

                // Before each tooltip is shown (the first one may have been sized already: see Apply, called at once).
                toolTip.Popup += (sender, e) => Apply(toolTip);
            }

            Apply(toolTip);
        }

        /// <summary>Limits the lines of the tooltip to <see cref="MaxWidthAt96Dpi"/> pixels, scaled to the screen of its owner.</summary>
        public static void Limit(ToolTip toolTip, Control owner)
        {
            Set(toolTip, (int)Math.Round(MaxWidthAt96Dpi * owner.DeviceDpi / 96.0));
        }

        /// <summary>Sets the limit in the window of the tooltip, if it has a limit; makes the window if it does not exist yet.</summary>
        public static void Apply(ToolTip toolTip)
        {
            if (!Limits.TryGetValue(toolTip, out WidthLimit limit))
                return;

            if (!(HandleProperty?.GetValue(toolTip) is IntPtr window) || window == IntPtr.Zero)
                return;

            SendMessage(window, TTM_SETMAXTIPWIDTH, 0, limit.Width);
        }
    }
}
