using System.Threading;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // Shared by ReadOnlyDataGridView and ListView's constructors - both set
    // AllowDrop purely to support their own hand-rolled column-reorder drag,
    // not as an app-facing drop target, and only ever from here.
    internal static class DragDropSupport
    {
        // Registering a drop target needs an STA thread (true for any real
        // WinForms UI thread) - confirmed live on both controls that setting
        // AllowDrop on an MTA one (e.g. a test harness thread with no
        // message loop) doesn't throw, but can silently block for many
        // seconds while the underlying OLE registration retries. Skipped
        // entirely off STA, where the drag itself couldn't have worked
        // anyway - each control's own mouse-down handling checks AllowDrop
        // itself and never arms a drag if this never got set.
        public static void EnableDropIfSta(Control control)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                control.AllowDrop = true;
            }
        }
    }
}
