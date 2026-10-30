using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using LibraryMenu = ErikwnkWFUI.Controls.ContextMenuStrip;

namespace ErikwnkWFUI.Forms
{
    // The system menu of a window that has the title bar of the library (the native one has no frame to hang it on).
    public partial class AppForm
    {
        private bool _canMinimize;
        private bool _canMaximize;
        private bool _isFixedSize;
        private Func<UIColorSet> _menuColors;
        private LibraryMenu _openMenu;

        // Right-click on the title bar, or Alt+Space (the keys of the native one).
        /// <inheritdoc/>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Alt | Keys.Space) && TitleBar != null && TitleBar.Parent == this)
            {
                ShowSystemMenu(PointToScreen(new Point(0, TitleBar.Height)));

                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Shows the system menu (Restore, Minimize, Maximize, Close) at a place on the screen - what a right-click on the title bar and
        /// Alt+Space do. The entries the window cannot do are left out.
        /// </summary>
        public void ShowSystemMenu(Point screenPosition)
        {
            _openMenu?.Dispose();

            LibraryMenu menu = CreateSystemMenu();
            _openMenu = menu;

            // Gone when it closes - but not while its own click handler still runs, and not through a window that is gone (an entry that
            // closes the window disposes the menu with it).
            menu.Closed += (sender, e) =>
            {
                if (IsDisposed || !IsHandleCreated)
                    menu.Dispose();
                else
                    BeginInvoke(new Action(menu.Dispose));
            };
            menu.Show(screenPosition);
        }

        // The entries, as the window can do them now.
        internal LibraryMenu CreateSystemMenu()
        {
            LibraryMenu menu = UIStyles.ContextMenuStrips.CreateStandard();

            if (_menuColors != null)
                menu.FollowColors(_menuColors);

            bool maximized = WindowState == FormWindowState.Maximized;
            bool canResize = !_isFixedSize;

            if (maximized)
                menu.Items.Add(CreateItem(UIStrings.Get("TitleBar.Restore"), null, () => WindowState = FormWindowState.Normal));

            if (_canMinimize)
                menu.Items.Add(CreateItem(UIStrings.Get("TitleBar.Minimize"), null, () => WindowState = FormWindowState.Minimized));

            if (_canMaximize && canResize && !maximized)
                menu.Items.Add(CreateItem(UIStrings.Get("TitleBar.Maximize"), null, () => WindowState = FormWindowState.Maximized));

            if (menu.Items.Count > 0)
                menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem close = CreateItem(UIStrings.Get("TitleBar.Close"), "Alt+F4", Close);
            close.Enabled = _closeEnabled;
            menu.Items.Add(close);

            return menu;
        }

        private static ToolStripMenuItem CreateItem(string text, string shortcut, Action action)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);

            if (shortcut != null)
                item.ShortcutKeyDisplayString = shortcut;

            item.Click += (sender, e) => action();

            return item;
        }
    }
}
