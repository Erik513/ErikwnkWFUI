using System;
using System.Linq;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // What counts as "somewhere else" for a list or grid that deselects when the user clicks elsewhere: a press in a window that is not the
    // owner itself or something that belongs to it (its header, the editor of a cell, its scroll bars), that is one of this application's own
    // windows (not another application's), and that is not a control that gives a command - a button, a tool bar, a menu bar or its menus -
    // because a command like "Delete" works on the selection and would find it gone.
    internal static class SelectionOutsideClick
    {
        // <paramref name="insideRoot"/>: the control that holds the owner and the parts that belong with it (the title of a titled list); the owner itself
        // when there is none.
        public static bool IsElsewhere(IntPtr window, Control owner, Control insideRoot = null, params IntPtr[] alsoInside)
        {
            if (window == owner.Handle || alsoInside.Contains(window))
                return false;

            Control clicked = Control.FromChildHandle(window);

            if (clicked == null)
            {
                // (A window of another application.)
                return false;
            }

            Control root = insideRoot ?? owner;

            if (clicked == root || root.Contains(clicked))
                return false;

            return !(clicked is ButtonBase || clicked is ToolStrip);
        }
    }
}