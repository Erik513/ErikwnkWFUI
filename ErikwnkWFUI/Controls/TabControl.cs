using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A <see cref="ReadOnlyTabControl"/> whose user can also add and close
    /// tabs through a right-click menu in the library's own design.
    /// </summary>
    /// <remarks>
    /// Named the same as its own base class' base, same as
    /// <see cref="DataGridView"/> next to <see cref="ReadOnlyDataGridView"/>.
    /// Right-click a tab: "Add tab" inserts a new page right after that tab
    /// (at the end when the click was not on a tab), "Close tab" closes it.
    /// A right-click also selects the tab (see
    /// <see cref="SelectTabOnRightClick"/>), so it is clear which one the menu
    /// acts on. Closing the selected tab selects the one before it (the next
    /// one when it was the first), and the last remaining tab can never be
    /// closed.
    /// Switch either action off with <see cref="AllowUserToAddTabs"/> /
    /// <see cref="AllowUserToCloseTabs"/>. Use
    /// <see cref="ReadOnlyTabControl"/> for tabs that cannot be edited at all.
    /// </remarks>
    public class TabControl : ReadOnlyTabControl
    {
        private readonly ThemeColor _contextMenuSelectionColor = new ThemeColor(() => UIColors.BorderLight);

        private bool _allowUserToAddTabs;
        private bool _allowUserToCloseTabs;
        private bool _selectTabOnRightClick = true;
        private ContextMenuStrip _builtInMenu;
        private ToolStripMenuItem _addTabItem;
        private ToolStripMenuItem _closeTabItem;

        // Tab under the last right-click, taken on mouse down - and the one
        // the open menu acts on, fixed when the menu opens. -1 when the
        // click was not on a tab (empty strip, a page's content, a key).
        private int _rightClickedTabIndex = -1;
        private int _menuTargetIndex = -1;

        /// <summary>Raised before a tab is added from the context menu. Cancel it, or replace <see cref="TabAddingEventArgs.TabPage"/> to add a different page.</summary>
        public event EventHandler<TabAddingEventArgs> TabAdding;

        /// <summary>Raised before a tab is closed from the context menu. Cancel it to keep the tab.</summary>
        public event EventHandler<TabClosingEventArgs> TabClosing;

        /// <summary>Lets the user add tabs from the right-click menu. On by default.</summary>
        public bool AllowUserToAddTabs
        {
            get => _allowUserToAddTabs;
            set
            {
                _allowUserToAddTabs = value;
                UpdateBuiltInMenu();
            }
        }

        /// <summary>Lets the user close tabs from the right-click menu - never the last one. On by default.</summary>
        public bool AllowUserToCloseTabs
        {
            get => _allowUserToCloseTabs;
            set
            {
                _allowUserToCloseTabs = value;
                UpdateBuiltInMenu();
            }
        }

        /// <summary>
        /// Whether a right-click on a tab selects it, so it is visible which
        /// tab the menu acts on. On by default; switch it off for the
        /// standard control's behavior, where only a left-click selects.
        /// </summary>
        public bool SelectTabOnRightClick
        {
            get => _selectTabOnRightClick;
            set => _selectTabOnRightClick = value;
        }

        /// <summary>
        /// Selection color of the built-in right-click menu. Defaults to a
        /// neutral gray (<see cref="UIColors.BorderLight"/>); the Primary
        /// factory sets it to the accent, same as on
        /// <see cref="ListView.ContextMenuSelectionColor"/>.
        /// </summary>
        public Color ContextMenuSelectionColor
        {
            get => _contextMenuSelectionColor.Value;
            set
            {
                _contextMenuSelectionColor.Set(value);

                if (_builtInMenu != null)
                {
                    _builtInMenu.SelectionBackColor = value;
                }
            }
        }

        public TabControl()
        {
            UIStrings.LanguageChanged += OnUIStringsLanguageChanged;

            AllowUserToAddTabs = true;
            AllowUserToCloseTabs = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIStrings.LanguageChanged -= OnUIStringsLanguageChanged;

                if (_builtInMenu != null)
                {
                    _builtInMenu.Dispose();
                    _builtInMenu = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _rightClickedTabIndex = GetTabIndexAt(e.Location);

                // Before the base raises TabRightClick, so a handler already
                // sees the tab selected.
                if (_selectTabOnRightClick && _rightClickedTabIndex >= 0)
                {
                    SelectedIndex = _rightClickedTabIndex;
                }
            }

            base.OnMouseDown(e);
        }

        // The library's own themed menu, put in place while either "allow"
        // switch is on - unless the application assigned a menu of its own,
        // which always wins.
        private void UpdateBuiltInMenu()
        {
            bool wanted = _allowUserToAddTabs || _allowUserToCloseTabs;

            if (wanted && ContextMenuStrip == null)
            {
                _builtInMenu = BuildContextMenu();
                ContextMenuStrip = _builtInMenu;
            }
            else if (!wanted && _builtInMenu != null)
            {
                if (ContextMenuStrip == _builtInMenu)
                {
                    ContextMenuStrip = null;
                }

                _builtInMenu.Dispose();
                _builtInMenu = null;
                _addTabItem = null;
                _closeTabItem = null;
            }
        }

        private ContextMenuStrip BuildContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                SelectionBackColor = ContextMenuSelectionColor
            };

            _addTabItem = new ToolStripMenuItem(UIStrings.Get("TabControl.AddTab"), null, (sender, e) => AddTabFromMenu());
            _closeTabItem = new ToolStripMenuItem(UIStrings.Get("TabControl.CloseTab"), null, (sender, e) => CloseTabFromMenu());

            menu.Items.Add(_addTabItem);
            menu.Items.Add(_closeTabItem);

            menu.Opening += (sender, e) =>
            {
                _menuTargetIndex = _rightClickedTabIndex;
                _rightClickedTabIndex = -1;

                _addTabItem.Visible = _allowUserToAddTabs;
                _closeTabItem.Visible = _allowUserToCloseTabs;
                _closeTabItem.Enabled = CanCloseTab(_menuTargetIndex);
            };

            return menu;
        }

        // A tab can be closed while it exists and is not the last one left.
        private bool CanCloseTab(int index)
        {
            return index >= 0 && index < TabCount && TabCount > 1;
        }

        // The new page goes right behind the tab that was right-clicked, at
        // the end when the click was not on a tab.
        private void AddTabFromMenu()
        {
            TabAddingEventArgs args = new TabAddingEventArgs(new TabPage(UIStrings.Get("TabControl.NewTabTitle")));
            TabAdding?.Invoke(this, args);

            if (args.Cancel || args.TabPage == null)
            {
                if (args.Cancel && args.TabPage != null)
                {
                    args.TabPage.Dispose();
                }

                return;
            }

            int insertAt = _menuTargetIndex >= 0 && _menuTargetIndex < TabCount
                ? _menuTargetIndex + 1
                : TabCount;

            TabPages.Insert(insertAt, args.TabPage);
            SelectedTab = args.TabPage;
        }

        // Closing the selected tab selects the one before it - or, for the
        // first tab, the one that takes its place. Closing any other tab
        // leaves the selection where it was.
        private void CloseTabFromMenu()
        {
            if (!CanCloseTab(_menuTargetIndex))
                return;

            int index = _menuTargetIndex;
            TabPage page = TabPages[index];
            TabClosingEventArgs args = new TabClosingEventArgs(page);
            TabClosing?.Invoke(this, args);

            if (args.Cancel)
                return;

            bool wasSelected = index == SelectedIndex;

            TabPages.RemoveAt(index);

            // Removing any other tab keeps the selected page on its own.
            if (wasSelected)
            {
                SelectedIndex = Math.Max(0, index - 1);
            }

            page.Dispose();
        }

        private void OnUIStringsLanguageChanged(object sender, EventArgs e)
        {
            if (_addTabItem == null)
                return;

            _addTabItem.Text = UIStrings.Get("TabControl.AddTab");
            _closeTabItem.Text = UIStrings.Get("TabControl.CloseTab");
        }
    }
}
