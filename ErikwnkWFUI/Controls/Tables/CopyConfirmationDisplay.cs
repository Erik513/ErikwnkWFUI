using System.Windows.Forms;
using ErikwnkWFUI.Forms;

namespace ErikwnkWFUI.Controls
{
    // Shared "how a copy gets visually confirmed" logic for ListView and
    // ReadOnlyDataGridView/DataGridView - both offer the exact same
    // three-way choice (Toast/ToolTip/None, see CopyConfirmationStyle) for
    // their own CopyConfirmation property, so there's no reason for each
    // control to carry its own copy of this same switch.
    internal static class CopyConfirmationDisplay
    {
        public static void Show(CopyConfirmationStyle style, string message, Control owner, ToolTip toolTip)
        {
            switch (style)
            {
                case CopyConfirmationStyle.None:
                    return;

                case CopyConfirmationStyle.ToolTip:
                    toolTip.Show(message, owner, 12, 12, 2000);
                    return;

                default:
                    Form form = owner.FindForm();
                    if (form != null)
                    {
                        ToastForm.ShowToast(message, form);
                    }
                    return;
            }
        }
    }
}
