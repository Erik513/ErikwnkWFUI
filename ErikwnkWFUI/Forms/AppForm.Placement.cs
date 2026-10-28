using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Forms
{
    // The window remembers where it was (and how big, and whether it was maximized) between runs.
    public partial class AppForm
    {
        /// <summary>
        /// A name under which the window remembers its position and size between runs (<see cref="FormOptions.RememberBoundsKey"/>): it is
        /// saved when the window is closed and restored when it opens, as long as it is still on a screen that is there. Use one name per
        /// window of the program. Null (the default) remembers nothing. A fixed-size window remembers only its position. Set it before the
        /// window is shown.
        /// </summary>
        public string RememberBoundsKey { get; set; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (string.IsNullOrEmpty(RememberBoundsKey))
                return;

            if (!WindowPlacementStore.TryLoad(RememberBoundsKey, WindowPlacementStore.GetWorkingAreas(), out WindowPlacementStore.Placement placement))
                return;

            Rectangle bounds = placement.Bounds;

            StartPosition = FormStartPosition.Manual;

            if (_isFixedSize)
                Location = bounds.Location;
            else
                Bounds = bounds;

            if (placement.Maximized && _canMaximize && !_isFixedSize)
                WindowState = FormWindowState.Maximized;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            if (string.IsNullOrEmpty(RememberBoundsKey))
                return;

            bool maximized = WindowState == FormWindowState.Maximized;
            Rectangle bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;

            WindowPlacementStore.Save(RememberBoundsKey, bounds, maximized);
        }
    }
}
