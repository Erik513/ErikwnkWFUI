using System;
using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // The members of the list box, passed on to the list inside (items, selection, colors, finding, updating), so that a
    // TitledListBox is used like a ListBox. The title bar, the events and the layout are in TitledListBox.cs.
    public partial class TitledListBox : UserControl
    {
        public Func<object, bool> IsItemDisabled
        {
            get { return _listBox.IsItemDisabled; }
            set { _listBox.IsItemDisabled = value; }
        }

        public Func<object, Image> IconProvider
        {
            get { return _listBox.IconProvider; }
            set { _listBox.IconProvider = value; }
        }

        public Func<object, string> DisplayTextProvider
        {
            get { return _listBox.DisplayTextProvider; }
            set { _listBox.DisplayTextProvider = value; }
        }

        public ListBox.ObjectCollection Items
        {
            get { return _listBox.Items; }
        }

        public object SelectedItem
        {
            get { return _listBox.SelectedItem; }
            set { _listBox.SelectedItem = value; }
        }

        public int SelectedIndex
        {
            get { return _listBox.SelectedIndex; }
            set { _listBox.SelectedIndex = value; }
        }

        public int ItemHeight
        {
            get { return _listBox.ItemHeight; }
            set { _listBox.ItemHeight = value; }
        }

        public Color DragIndicatorColor
        {
            get { return _listBox.DragIndicatorColor; }
            set { _listBox.DragIndicatorColor = value; }
        }

        public Color SelectedForeColor
        {
            get { return _listBox.SelectedForeColor; }
            set { _listBox.SelectedForeColor = value; }
        }

        public Color SelectedBackColor
        {
            get { return _listBox.SelectedBackColor; }
            set { _listBox.SelectedBackColor = value; }
        }

        public bool ShowEnumeration
        {
            get { return _listBox.ShowEnumeration; }
            set { _listBox.ShowEnumeration = value; }
        }

        public bool AllowReorder
        {
            get { return _listBox.AllowReorder; }
            set { _listBox.AllowReorder = value; }
        }

        public Color ItemBackColor
        {
            get { return _listBox.ItemBackColor; }
            set { _listBox.ItemBackColor = value; }
        }

        public Color ItemForeColor
        {
            get { return _listBox.ItemForeColor; }
            set { _listBox.ItemForeColor = value; }
        }

        public Color HoverBackColor
        {
            get { return _listBox.HoverBackColor; }
            set { _listBox.HoverBackColor = value; }
        }

        public Color DisabledForeColor
        {
            get { return _listBox.DisabledForeColor; }
            set { _listBox.DisabledForeColor = value; }
        }

        public Color DisabledBackColor
        {
            get { return _listBox.DisabledBackColor; }
            set { _listBox.DisabledBackColor = value; }
        }

        public Color DragHandleColor
        {
            get { return _listBox.DragHandleColor; }
            set { _listBox.DragHandleColor = value; }
        }

        public SelectionMode SelectionMode
        {
            get { return _listBox.SelectionMode; }
            set { _listBox.SelectionMode = value; }
        }

        public ListBox.SelectedObjectCollection SelectedItems
        {
            get { return _listBox.SelectedItems; }
        }

        public ListBox.SelectedIndexCollection SelectedIndices
        {
            get { return _listBox.SelectedIndices; }
        }

        public object DataSource
        {
            get { return _listBox.DataSource; }
            set { _listBox.DataSource = value; }
        }

        public string DisplayMember
        {
            get { return _listBox.DisplayMember; }
            set { _listBox.DisplayMember = value; }
        }

        public string ValueMember
        {
            get { return _listBox.ValueMember; }
            set { _listBox.ValueMember = value; }
        }

        public object SelectedValue
        {
            get { return _listBox.SelectedValue; }
            set { _listBox.SelectedValue = value; }
        }

        public int TopIndex
        {
            get { return _listBox.TopIndex; }
            set { _listBox.TopIndex = value; }
        }

        public bool Sorted
        {
            get { return _listBox.Sorted; }
            set { _listBox.Sorted = value; }
        }

        public bool ScrollAlwaysVisible
        {
            get { return _listBox.ScrollAlwaysVisible; }
            set { _listBox.ScrollAlwaysVisible = value; }
        }

        public bool MultiColumn
        {
            get { return _listBox.MultiColumn; }
            set { _listBox.MultiColumn = value; }
        }

        public int ColumnWidth
        {
            get { return _listBox.ColumnWidth; }
            set { _listBox.ColumnWidth = value; }
        }

        public bool IntegralHeight
        {
            get { return _listBox.IntegralHeight; }
            set { _listBox.IntegralHeight = value; }
        }

        public string FormatString
        {
            get { return _listBox.FormatString; }
            set { _listBox.FormatString = value; }
        }

        public bool FormattingEnabled
        {
            get { return _listBox.FormattingEnabled; }
            set { _listBox.FormattingEnabled = value; }
        }

        public int FindString(string text)
        {
            return _listBox.FindString(text);
        }

        public int FindString(string text, int startIndex)
        {
            return _listBox.FindString(text, startIndex);
        }

        public int FindStringExact(string text)
        {
            return _listBox.FindStringExact(text);
        }

        public bool GetSelected(int index)
        {
            return _listBox.GetSelected(index);
        }

        public void SetSelected(int index, bool value)
        {
            _listBox.SetSelected(index, value);
        }

        public string GetItemText(object item)
        {
            return _listBox.GetItemText(item);
        }

        public Rectangle GetItemRectangle(int index)
        {
            return _listBox.GetItemRectangle(index);
        }

        /// <summary>The font of the items (as on the list box; the title has <see cref="HeaderFont"/>).</summary>
        public override Font Font
        {
            get { return _listBox != null ? _listBox.Font : base.Font; }
            set
            {
                if (_listBox != null)
                    _listBox.Font = value;
                else
                    base.Font = value;
            }
        }

        /// <summary>The text color of the items (as on the list box; the title has <see cref="HeaderForeColor"/>).</summary>
        public override Color ForeColor
        {
            get { return _listBox != null ? _listBox.ForeColor : base.ForeColor; }
            set
            {
                if (_listBox != null)
                    _listBox.ForeColor = value;
                else
                    base.ForeColor = value;
            }
        }

        /// <summary>The context menu of the list (a right click on an item or on the room below the items).</summary>
        public override System.Windows.Forms.ContextMenuStrip ContextMenuStrip
        {
            get { return base.ContextMenuStrip; }
            set
            {
                base.ContextMenuStrip = value;

                if (_listBox != null)
                    _listBox.ContextMenuStrip = value;
            }
        }

        /// <summary>Gives the focus to the list.</summary>
        public new bool Focus()
        {
            return _listBox.Focus();
        }

        /// <summary>Selects the list (it is the one that takes the keys).</summary>
        public new void Select()
        {
            _listBox.Select();
        }

        public int IndexFromPoint(Point point)
        {
            return _listBox.IndexFromPoint(point);
        }

        public void ClearSelected()
        {
            _listBox.ClearSelected();
        }

        public void MoveItem(int fromIndex, int toIndex)
        {
            _listBox.MoveItem(fromIndex, toIndex);
        }

        public void BeginUpdate()
        {
            if (_listBox == null)
                return;

            _listBox.BeginUpdate();
        }

        public void EndUpdate()
        {
            if (_listBox == null)
                return;

            _listBox.EndUpdate();
        }
    }
}
