using System;
using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    // A click somewhere else deselects, as in the ListView and the grid: the filter watches the whole application, so it runs only while the
    // list has a window. The title of a TitledListBox belongs to the list (it is inside the same control).
    public partial class ListBox
    {
        private OutsideClickFilter _outsideClickDeselectFilter;

        /// <summary>Whether a click with the left mouse button somewhere else in the window (not on a button, a tool bar or a menu) takes the selection away. On by default.</summary>
        [DefaultValue(true)]
        public bool DeselectOnOutsideClick { get; set; } = true;

        private void StartWatchingOutsideClicks()
        {
            if (_outsideClickDeselectFilter == null)
                _outsideClickDeselectFilter = new OutsideClickFilter(IsOutsideClickThatDeselects, ClearSelected, leftButtonOnly: true);

            _outsideClickDeselectFilter.Start();
        }

        /// <inheritdoc/>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_outsideClickDeselectFilter != null)
                _outsideClickDeselectFilter.Stop();

            base.OnHandleDestroyed(e);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _outsideClickDeselectFilter != null)
                _outsideClickDeselectFilter.Stop();

            base.Dispose(disposing);
        }

        private bool IsOutsideClickThatDeselects(IntPtr window)
        {
            if (!DeselectOnOutsideClick || IsDisposed || !IsHandleCreated || SelectedIndices.Count == 0)
                return false;

            Control root = Parent is TitledListBox ? Parent : this;

            return SelectionOutsideClick.IsElsewhere(window, this, root);
        }
    }
}