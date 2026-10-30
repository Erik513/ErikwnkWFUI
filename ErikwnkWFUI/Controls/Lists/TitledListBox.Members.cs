using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // The members of the list box, passed on to the list inside (items, selection, colors, finding, updating), so that a
    // TitledListBox is used like a ListBox. The title bar, the events and the layout are in TitledListBox.cs.
    public partial class TitledListBox : UserControl
    {
        // The designer asks the list inside whether a value was set: the members below that pass a color or a height on are written only then.
        private bool InnerShouldSerialize(string name)
        {
            return TypeDescriptor.GetProperties(_listBox)[name].ShouldSerializeValue(_listBox);
        }

        private void InnerReset(string name)
        {
            TypeDescriptor.GetProperties(_listBox)[name].ResetValue(_listBox);
        }

        private bool ShouldSerializeDragIndicatorColor()
        {
            return InnerShouldSerialize("DragIndicatorColor");
        }

        private void ResetDragIndicatorColor()
        {
            InnerReset("DragIndicatorColor");
        }

        private bool ShouldSerializeSelectedForeColor()
        {
            return InnerShouldSerialize("SelectedForeColor");
        }

        private void ResetSelectedForeColor()
        {
            InnerReset("SelectedForeColor");
        }

        private bool ShouldSerializeSelectedBackColor()
        {
            return InnerShouldSerialize("SelectedBackColor");
        }

        private void ResetSelectedBackColor()
        {
            InnerReset("SelectedBackColor");
        }

        private bool ShouldSerializeItemBackColor()
        {
            return InnerShouldSerialize("ItemBackColor");
        }

        private void ResetItemBackColor()
        {
            InnerReset("ItemBackColor");
        }

        private bool ShouldSerializeItemForeColor()
        {
            return InnerShouldSerialize("ItemForeColor");
        }

        private void ResetItemForeColor()
        {
            InnerReset("ItemForeColor");
        }

        private bool ShouldSerializeHoverBackColor()
        {
            return InnerShouldSerialize("HoverBackColor");
        }

        private void ResetHoverBackColor()
        {
            InnerReset("HoverBackColor");
        }

        private bool ShouldSerializeDisabledForeColor()
        {
            return InnerShouldSerialize("DisabledForeColor");
        }

        private void ResetDisabledForeColor()
        {
            InnerReset("DisabledForeColor");
        }

        private bool ShouldSerializeDisabledBackColor()
        {
            return InnerShouldSerialize("DisabledBackColor");
        }

        private void ResetDisabledBackColor()
        {
            InnerReset("DisabledBackColor");
        }

        private bool ShouldSerializeDragHandleColor()
        {
            return InnerShouldSerialize("DragHandleColor");
        }

        private void ResetDragHandleColor()
        {
            InnerReset("DragHandleColor");
        }

        private bool ShouldSerializeItemHeight()
        {
            return InnerShouldSerialize("ItemHeight");
        }

        private void ResetItemHeight()
        {
            InnerReset("ItemHeight");
        }

        private bool ShouldSerializeForeColor()
        {
            return InnerShouldSerialize("ForeColor");
        }

        private void ResetForeColor()
        {
            InnerReset("ForeColor");
        }

        /// <inheritdoc cref="ListBox.IsItemDisabled"/>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<object, bool> IsItemDisabled
        {
            get { return _listBox.IsItemDisabled; }
            set { _listBox.IsItemDisabled = value; }
        }

        /// <inheritdoc cref="ListBox.IconProvider"/>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<object, Image> IconProvider
        {
            get { return _listBox.IconProvider; }
            set { _listBox.IconProvider = value; }
        }

        /// <inheritdoc cref="ListBox.DisplayTextProvider"/>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<object, string> DisplayTextProvider
        {
            get { return _listBox.DisplayTextProvider; }
            set { _listBox.DisplayTextProvider = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.Items"/>
        public ListBox.ObjectCollection Items
        {
            get { return _listBox.Items; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.SelectedItem"/>
        public object SelectedItem
        {
            get { return _listBox.SelectedItem; }
            set { _listBox.SelectedItem = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.SelectedIndex"/>
        public int SelectedIndex
        {
            get { return _listBox.SelectedIndex; }
            set { _listBox.SelectedIndex = value; }
        }

        /// <inheritdoc cref="ListBox.ItemHeight"/>
        public int ItemHeight
        {
            get { return _listBox.ItemHeight; }
            set { _listBox.ItemHeight = value; }
        }

        /// <inheritdoc cref="ListBox.DragIndicatorColor"/>
        public Color DragIndicatorColor
        {
            get { return _listBox.DragIndicatorColor; }
            set { _listBox.DragIndicatorColor = value; }
        }

        /// <inheritdoc cref="ListBox.SelectedForeColor"/>
        public Color SelectedForeColor
        {
            get { return _listBox.SelectedForeColor; }
            set { _listBox.SelectedForeColor = value; }
        }

        /// <inheritdoc cref="ListBox.SelectedBackColor"/>
        public Color SelectedBackColor
        {
            get { return _listBox.SelectedBackColor; }
            set { _listBox.SelectedBackColor = value; }
        }

        /// <inheritdoc cref="ListBox.ShowEnumeration"/>
        [DefaultValue(false)]
        public bool ShowEnumeration
        {
            get { return _listBox.ShowEnumeration; }
            set { _listBox.ShowEnumeration = value; }
        }

        /// <inheritdoc cref="ListBox.AllowReorder"/>
        [DefaultValue(false)]
        public bool AllowReorder
        {
            get { return _listBox.AllowReorder; }
            set { _listBox.AllowReorder = value; }
        }

        /// <inheritdoc cref="ListBox.ItemBackColor"/>
        public Color ItemBackColor
        {
            get { return _listBox.ItemBackColor; }
            set { _listBox.ItemBackColor = value; }
        }

        /// <inheritdoc cref="ListBox.ItemForeColor"/>
        public Color ItemForeColor
        {
            get { return _listBox.ItemForeColor; }
            set { _listBox.ItemForeColor = value; }
        }

        /// <inheritdoc cref="ListBox.HoverBackColor"/>
        public Color HoverBackColor
        {
            get { return _listBox.HoverBackColor; }
            set { _listBox.HoverBackColor = value; }
        }

        /// <inheritdoc cref="ListBox.DisabledForeColor"/>
        public Color DisabledForeColor
        {
            get { return _listBox.DisabledForeColor; }
            set { _listBox.DisabledForeColor = value; }
        }

        /// <inheritdoc cref="ListBox.DisabledBackColor"/>
        public Color DisabledBackColor
        {
            get { return _listBox.DisabledBackColor; }
            set { _listBox.DisabledBackColor = value; }
        }

        /// <inheritdoc cref="ListBox.DragHandleColor"/>
        public Color DragHandleColor
        {
            get { return _listBox.DragHandleColor; }
            set { _listBox.DragHandleColor = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.SelectionMode"/>
        public SelectionMode SelectionMode
        {
            get { return _listBox.SelectionMode; }
            set { _listBox.SelectionMode = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.SelectedItems"/>
        public ListBox.SelectedObjectCollection SelectedItems
        {
            get { return _listBox.SelectedItems; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.SelectedIndices"/>
        public ListBox.SelectedIndexCollection SelectedIndices
        {
            get { return _listBox.SelectedIndices; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListControl.DataSource"/>
        public object DataSource
        {
            get { return _listBox.DataSource; }
            set { _listBox.DataSource = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListControl.DisplayMember"/>
        public string DisplayMember
        {
            get { return _listBox.DisplayMember; }
            set { _listBox.DisplayMember = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListControl.ValueMember"/>
        public string ValueMember
        {
            get { return _listBox.ValueMember; }
            set { _listBox.ValueMember = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListControl.SelectedValue"/>
        public object SelectedValue
        {
            get { return _listBox.SelectedValue; }
            set { _listBox.SelectedValue = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.TopIndex"/>
        public int TopIndex
        {
            get { return _listBox.TopIndex; }
            set { _listBox.TopIndex = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.Sorted"/>
        public bool Sorted
        {
            get { return _listBox.Sorted; }
            set { _listBox.Sorted = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.ScrollAlwaysVisible"/>
        public bool ScrollAlwaysVisible
        {
            get { return _listBox.ScrollAlwaysVisible; }
            set { _listBox.ScrollAlwaysVisible = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.MultiColumn"/>
        public bool MultiColumn
        {
            get { return _listBox.MultiColumn; }
            set { _listBox.MultiColumn = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.ColumnWidth"/>
        public int ColumnWidth
        {
            get { return _listBox.ColumnWidth; }
            set { _listBox.ColumnWidth = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.IntegralHeight"/>
        public bool IntegralHeight
        {
            get { return _listBox.IntegralHeight; }
            set { _listBox.IntegralHeight = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListControl.FormatString"/>
        public string FormatString
        {
            get { return _listBox.FormatString; }
            set { _listBox.FormatString = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListControl.FormattingEnabled"/>
        public bool FormattingEnabled
        {
            get { return _listBox.FormattingEnabled; }
            set { _listBox.FormattingEnabled = value; }
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.FindString(string)"/>
        public int FindString(string text)
        {
            return _listBox.FindString(text);
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.FindString(string, int)"/>
        public int FindString(string text, int startIndex)
        {
            return _listBox.FindString(text, startIndex);
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.FindStringExact(string)"/>
        public int FindStringExact(string text)
        {
            return _listBox.FindStringExact(text);
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.GetSelected(int)"/>
        public bool GetSelected(int index)
        {
            return _listBox.GetSelected(index);
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.SetSelected(int, bool)"/>
        public void SetSelected(int index, bool value)
        {
            _listBox.SetSelected(index, value);
        }

        /// <inheritdoc cref="System.Windows.Forms.ListControl.GetItemText(object)"/>
        public string GetItemText(object item)
        {
            return _listBox.GetItemText(item);
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.GetItemRectangle(int)"/>
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

        /// <inheritdoc cref="System.Windows.Forms.ListBox.IndexFromPoint(Point)"/>
        public int IndexFromPoint(Point point)
        {
            return _listBox.IndexFromPoint(point);
        }

        /// <summary>Whether a click with the left mouse button somewhere else in the window (not on a button, a tool bar or a menu) takes the selection away. On by default.</summary>
        public bool DeselectOnOutsideClick
        {
            get { return _listBox.DeselectOnOutsideClick; }
            set { _listBox.DeselectOnOutsideClick = value; }
        }

        /// <inheritdoc cref="ListBox.ClearSelected()"/>
        public void ClearSelected()
        {
            _listBox.ClearSelected();
        }

        /// <inheritdoc cref="ListBox.MoveItem(int, int)"/>
        public void MoveItem(int fromIndex, int toIndex)
        {
            _listBox.MoveItem(fromIndex, toIndex);
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.BeginUpdate()"/>
        public void BeginUpdate()
        {
            if (_listBox == null)
                return;

            _listBox.BeginUpdate();
        }

        /// <inheritdoc cref="System.Windows.Forms.ListBox.EndUpdate()"/>
        public void EndUpdate()
        {
            if (_listBox == null)
                return;

            _listBox.EndUpdate();
        }
    }
}
