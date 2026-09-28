using System.Collections.Generic;
using System.Windows.Forms;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// Exposes a couple of protected members needed to drive DataGridView's
/// context-menu/keyboard behavior directly in a test, without an actual
/// mouse or a message loop - a subclass can call its own base class's
/// protected members freely, which is all RaiseCellMouseDown/PublicIsInputKey
/// below do.
/// </summary>
internal sealed class TestableDataGridView : WfuiDataGridView
{
    /// <summary>
    /// Runs the real OnCellMouseDown override - the same code path a mouse
    /// click drives - so tests can set up "row X was right-clicked" state
    /// (_contextMenuRowIndex, _contextMenuRowWasPlaceholder, selection)
    /// exactly the way the control itself does, instead of poking private
    /// fields directly and hoping that matches.
    /// </summary>
    public void RaiseCellMouseDown(int columnIndex, int rowIndex, MouseButtons button)
    {
        OnCellMouseDown(new DataGridViewCellMouseEventArgs(
            columnIndex, rowIndex, 0, 0, new MouseEventArgs(button, 1, 0, 0, 0)));
    }

    public bool PublicIsInputKey(Keys keyData)
    {
        return IsInputKey(keyData);
    }

    /// <summary>
    /// Every action-confirmation message that actually reached display,
    /// in call order - lets tests assert exactly what copy/cut/paste/clear/
    /// insert/delete would have shown the user, without a real Form for a
    /// ToastForm to attach to (see OnActionConfirmation's own base
    /// implementation, which no-ops when FindForm() returns null).
    /// </summary>
    public List<string> ActionConfirmationMessages { get; } = new List<string>();

    protected override void OnActionConfirmation(string message)
    {
        ActionConfirmationMessages.Add(message);
        base.OnActionConfirmation(message);
    }
}
