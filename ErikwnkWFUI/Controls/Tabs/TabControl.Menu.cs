using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    // The right-click menu: add, rename and close a tab, which tab it acts on, and the color of its selection.
    public partial class TabControl : ReadOnlyTabControl
    {

        private readonly ThemeColor _contextMenuSelectionColor = new ThemeColor(() => UIColors.BorderLight);

        private ToolStripMenuItem _renameTabItem;

        private ContextMenuStrip _builtInMenu;
        private ToolStripMenuItem _addTabItem;
        private ToolStripMenuItem _closeTabItem;

        // Tab under the last right-click, taken on mouse down - and the one
        // the open menu acts on, fixed when the menu opens. -1 when the
        // click was not on a tab (empty strip, a page's content, a key).
        private int _rightClickedTabIndex = -1;
        private int _menuTargetIndex = -1;

        /// <summary>
        /// Selection color of the built-in right-click menu. Defaults to a
        /// neutral gray (<see cref="UIColors.BorderLight"/>); the Primary
        /// factory sets it to the accent, same as on
        /// <see cref="ListView.ContextMenuSelectionColor"/>.
        /// </summary>
        public Color ContextMenuSelectionColor
        {
            get => _contextMenuSelectionColor.Value;
            set { _contextMenuSelectionColor.Set(value); }
        }

        // The library's own menu, in place while any of the Allow... switches
        // is on - unless the application assigned a menu of its own, which
        // always wins.
        private void UpdateBuiltInMenu()
        {
            bool wanted = _allowUserToAddTabs || _allowUserToCloseTabs || _allowUserToRenameTabs;

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
                _renameTabItem = null;
                _closeTabItem = null;
            }
        }

        private ContextMenuStrip BuildContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip
            {
                ShowImageMargin = false
            };

            menu.FollowSelectionBackColor(() => ContextMenuSelectionColor);

            _addTabItem = new ToolStripMenuItem(UIStrings.Get("TabControl.AddTab"), null, (sender, e) => AddTabFromMenu());
            _renameTabItem = new ToolStripMenuItem(UIStrings.Get("TabControl.RenameTab"), null, (sender, e) => RenameTabFromMenu());
            _closeTabItem = new ToolStripMenuItem(UIStrings.Get("TabControl.CloseTab"), null, (sender, e) => CloseTabFromMenu());

            menu.Items.Add(_addTabItem);
            menu.Items.Add(_renameTabItem);
            menu.Items.Add(_closeTabItem);

            menu.Opening += (sender, e) =>
            {
                _menuTargetIndex = _rightClickedTabIndex;
                _rightClickedTabIndex = -1;

                _addTabItem.Visible = _allowUserToAddTabs;
                _renameTabItem.Visible = _allowUserToRenameTabs;
                _renameTabItem.Enabled = CanRenameTab(_menuTargetIndex);
                _closeTabItem.Visible = _allowUserToCloseTabs;
                _closeTabItem.Enabled = CanCloseTab(_menuTargetIndex);
            };

            return menu;
        }

        // A tab can be closed while it exists, is not the last one left and
        // was not locked.
        private bool CanCloseTab(int index)
        {
            return index >= 0 && index < TabCount && TabCount > 1 && IsTabClosable(TabPages[index]);
        }

        // A tab can be renamed while it exists, renaming is allowed for it,
        // and nobody vetoes it through TabRenameStarting.
        private bool CanRenameTab(int index)
        {
            if (index < 0 || index >= TabCount || !IsTabRenamable(TabPages[index]))
                return false;

            TabRenameStartingEventArgs args = new TabRenameStartingEventArgs(TabPages[index], index);
            TabRenameStarting?.Invoke(this, args);

            return !args.Cancel;
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

            if (_renameTabAfterAdding && _allowUserToRenameTabs)
            {
                // After the menu has closed - it hands the focus back to
                // whatever had it before, which would end the edit again.
                TabPage added = args.TabPage;
                BeginInvoke(new Action(() => BeginRenameTab(TabPages.IndexOf(added))));
            }
        }

        private void RenameTabFromMenu()
        {
            int index = _menuTargetIndex;

            // Same reason as above: the menu is still closing.
            BeginInvoke(new Action(() => BeginRenameTab(index)));
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
            _renameTabItem.Text = UIStrings.Get("TabControl.RenameTab");
            _closeTabItem.Text = UIStrings.Get("TabControl.CloseTab");
        }
    }
}
