using System.ComponentModel;
using System;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    // A click somewhere else deselects, as in the ListView: the filter watches the whole application, so it runs only while the grid has a
    // window (OnHandleCreated, OnHandleDestroyed). SelectionOutsideClick says what counts as somewhere else.
    public partial class ReadOnlyDataGridView
    {
        private OutsideClickFilter _outsideClickDeselectFilter;

        /// <summary>Whether a click with the left mouse button somewhere else in the window (not on a button, a tool bar or a menu) takes the selection away. On by default; a grid whose cell is being edited keeps its selection.</summary>
        [DefaultValue(true)]
        public bool DeselectOnOutsideClick { get; set; } = true;

        /// <inheritdoc/>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (_outsideClickDeselectFilter == null)
                _outsideClickDeselectFilter = new OutsideClickFilter(IsOutsideClickThatDeselects, DeselectAll, leftButtonOnly: true);

            _outsideClickDeselectFilter.Start();
        }

        /// <inheritdoc/>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_outsideClickDeselectFilter != null)
                _outsideClickDeselectFilter.Stop();

            base.OnHandleDestroyed(e);
        }

        // A grid whose cell is being edited keeps its selection: the click ends the edit (the focus leaves the editor), and a selection
        // taken away at the same moment would take the cell the edit belongs to with it.
        private bool IsOutsideClickThatDeselects(IntPtr window)
        {
            if (!DeselectOnOutsideClick || IsDisposed || !IsHandleCreated || SelectedCells.Count == 0 || IsCurrentCellInEditMode)
                return false;

            return SelectionOutsideClick.IsElsewhere(window, this);
        }

        private void DeselectAll()
        {
            ClearSelection();
            CurrentCell = null;
        }
    }
}