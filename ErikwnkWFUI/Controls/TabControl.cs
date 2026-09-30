using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
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
    /// closed. "Rename tab" (or a double-click on the tab) turns the tab's
    /// name into a text box to type the new name straight into: Enter or
    /// leaving it applies the name, Escape drops it.
    /// Switch an action off with <see cref="AllowUserToAddTabs"/> /
    /// <see cref="AllowUserToCloseTabs"/> / <see cref="AllowUserToRenameTabs"/>. Use
    /// <see cref="ReadOnlyTabControl"/> for tabs that cannot be edited at all.
    /// </remarks>
    public class TabControl : ReadOnlyTabControl
    {
        private readonly ThemeColor _contextMenuSelectionColor = new ThemeColor(() => UIColors.BorderLight);

        private bool _allowUserToAddTabs;
        private bool _allowUserToCloseTabs;
        private bool _selectTabOnRightClick = true;
        private bool _allowUserToRenameTabs;
        private bool _renameTabOnDoubleClick = true;
        private bool _renameTabAfterAdding;
        private ToolStripMenuItem _renameTabItem;

        // The text box that sits on a tab while its name is being edited.
        private TextBox _renameBox;
        private int _renameIndex = -1;
        private RenameClickFilter _renameClickFilter;

        private const int MinimumRenameWidth = 60;
        private const int DefaultMaxTabNameLength = 40;

        private int _maxTabNameLength = DefaultMaxTabNameLength;

        private const int LockSlotWidth = 7;
        private const int LockSlotHeight = 14;
        private const int LockContentShift = 6;
        private const int ImageTextGap = 4;

        private bool _showLockIcon = true;

        // Room for the lock in front of a locked tab's name. The width of a
        // tab comes from the native control, which only makes room for text
        // and an image - so a transparent image of the lock's size is what
        // reserves it; the lock is drawn where that image would be.
        private ImageList _lockSlots;
        private ToolTip _lockToolTip;
        private string _lockToolTipText = "";

        // Per-tab switches, on top of the control-wide ones: a page that was
        // never touched can be renamed and closed (as far as the control
        // allows it). Weak, so a removed page is not kept alive by it.
        private sealed class TabPermissions
        {
            public bool CanRename = true;
            public bool CanClose = true;
        }

        private readonly ConditionalWeakTable<TabPage, TabPermissions> _permissions =
            new ConditionalWeakTable<TabPage, TabPermissions>();
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

        /// <summary>Raised before a typed name is applied to a tab. Cancel it to keep the old name, or change <see cref="TabRenamingEventArgs.NewName"/>.</summary>
        public event EventHandler<TabRenamingEventArgs> TabRenaming;

        /// <summary>
        /// Raised when a rename is about to start - and also when the control
        /// only checks whether one would be allowed, to enable or disable the
        /// menu entry. Cancel it to keep that tab's name from being edited.
        /// </summary>
        public event EventHandler<TabRenameStartingEventArgs> TabRenameStarting;

        /// <summary>Raised after a tab got its new name.</summary>
        public event EventHandler<TabRenamedEventArgs> TabRenamed;

        /// <summary>Lets the user rename tabs - from the right-click menu and, see <see cref="RenameTabOnDoubleClick"/>, by double-clicking. On by default.</summary>
        public bool AllowUserToRenameTabs
        {
            get => _allowUserToRenameTabs;
            set
            {
                _allowUserToRenameTabs = value;
                UpdateBuiltInMenu();

                if (!value)
                {
                    EndRename(commit: false);
                }
            }
        }

        /// <summary>Whether a double-click on a tab starts renaming it (needs <see cref="AllowUserToRenameTabs"/>). On by default.</summary>
        public bool RenameTabOnDoubleClick
        {
            get => _renameTabOnDoubleClick;
            set => _renameTabOnDoubleClick = value;
        }

        /// <summary>Whether a tab added from the menu starts out in rename mode, so it can be named right away. Off by default.</summary>
        public bool RenameTabAfterAdding
        {
            get => _renameTabAfterAdding;
            set => _renameTabAfterAdding = value;
        }

        /// <summary>
        /// Allows or forbids the user to rename one tab, on top of
        /// <see cref="AllowUserToRenameTabs"/> (which forbids it for all).
        /// A tab is renamable until this says otherwise.
        /// </summary>
        public void SetTabRenameAllowed(TabPage tabPage, bool allowed)
        {
            if (tabPage == null)
                throw new ArgumentNullException(nameof(tabPage));

            _permissions.GetOrCreateValue(tabPage).CanRename = allowed;
            SyncLockSlots();
            Repaint();
        }

        /// <summary>Whether <see cref="SetTabRenameAllowed"/> allows renaming this tab. Says nothing about the control-wide switch or <see cref="TabRenameStarting"/>.</summary>
        public bool IsTabRenameAllowed(TabPage tabPage)
        {
            if (tabPage == null)
                throw new ArgumentNullException(nameof(tabPage));

            return !_permissions.TryGetValue(tabPage, out TabPermissions permissions) || permissions.CanRename;
        }

        /// <summary>
        /// Allows or forbids the user to close one tab, on top of
        /// <see cref="AllowUserToCloseTabs"/> (which forbids it for all).
        /// A tab is closable until this says otherwise - though never the
        /// last one left.
        /// </summary>
        public void SetTabCloseAllowed(TabPage tabPage, bool allowed)
        {
            if (tabPage == null)
                throw new ArgumentNullException(nameof(tabPage));

            _permissions.GetOrCreateValue(tabPage).CanClose = allowed;
            SyncLockSlots();
            Repaint();
        }

        /// <summary>Whether <see cref="SetTabCloseAllowed"/> allows closing this tab. Says nothing about the control-wide switch or the last-tab rule.</summary>
        public bool IsTabCloseAllowed(TabPage tabPage)
        {
            if (tabPage == null)
                throw new ArgumentNullException(nameof(tabPage));

            return !_permissions.TryGetValue(tabPage, out TabPermissions permissions) || permissions.CanClose;
        }

        /// <summary>
        /// The most characters a tab name can have when the user types it -
        /// the rename box stops accepting more, and a pasted text is cut to
        /// fit. 40 by default; must be at least 1. Names that are already
        /// longer are left as they are until they are edited.
        /// </summary>
        public int MaxTabNameLength
        {
            get => _maxTabNameLength;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "A tab name needs room for at least one character.");

                _maxTabNameLength = value;

                if (_renameBox != null)
                {
                    _renameBox.MaxLength = value;
                }
            }
        }

        /// <summary>
        /// Whether a tab that was locked against renaming or closing (with
        /// <see cref="SetTabRenameAllowed"/> / <see cref="SetTabCloseAllowed"/>)
        /// shows a small padlock in front of its name, and a tooltip saying
        /// what is locked. On by default. The room for the padlock is only
        /// reserved while the control's own <c>ImageList</c> is not in use;
        /// with one of yours, the padlock sits at the right end of the tab
        /// instead.
        /// </summary>
        public bool ShowLockIcon
        {
            get => _showLockIcon;
            set
            {
                _showLockIcon = value;
                SyncLockSlots();
                Repaint();
            }
        }

        /// <summary>Whether a tab's name is being edited right now.</summary>
        public bool IsRenamingTab => _renameBox != null;

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
            AllowUserToRenameTabs = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIStrings.LanguageChanged -= OnUIStringsLanguageChanged;
                EndRename(commit: false);

                if (_lockToolTip != null)
                {
                    _lockToolTip.Dispose();
                    _lockToolTip = null;
                }

                if (_lockSlots != null)
                {
                    if (ImageList == _lockSlots)
                    {
                        ImageList = null;
                    }

                    _lockSlots.Dispose();
                    _lockSlots = null;
                }

                if (_builtInMenu != null)
                {
                    _builtInMenu.Dispose();
                    _builtInMenu = null;
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);

            if (e.Control is TabPage)
            {
                SyncLockSlots();
            }
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);

            if (e.Control is TabPage removed)
            {
                SyncLockSlots(removed);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            UpdateLockToolTip(GetTabIndexAt(e.Location));
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            UpdateLockToolTip(-1);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (e.Button == MouseButtons.Left && _allowUserToRenameTabs && _renameTabOnDoubleClick)
            {
                int index = GetTabIndexAt(e.Location);

                if (index >= 0)
                {
                    BeginRenameTab(index);
                }
            }
        }

        // Anything that moves the tabs, or changes which one is selected,
        // ends the edit - the box would be left over a tab it no longer
        // belongs to.
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            EndRename(commit: true);
            base.OnSelectedIndexChanged(e);
        }

        protected override void OnResize(EventArgs e)
        {
            EndRename(commit: true);
            base.OnResize(e);
        }

        protected override void WndProc(ref Message m)
        {
            // The strip scrolling (WM_HSCROLL / WM_VSCROLL) moves every tab.
            if ((m.Msg == 0x0114 || m.Msg == 0x0115) && _renameBox != null)
            {
                EndRename(commit: true);
            }

            base.WndProc(ref m);
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
                ShowImageMargin = false,
                SelectionBackColor = ContextMenuSelectionColor
            };

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
            return index >= 0 && index < TabCount && TabCount > 1 && IsTabCloseAllowed(TabPages[index]);
        }

        // A tab can be renamed while it exists, renaming is allowed for it,
        // and nobody vetoes it through TabRenameStarting.
        private bool CanRenameTab(int index)
        {
            if (index < 0 || index >= TabCount || !IsTabRenameAllowed(TabPages[index]))
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

        // ---- the padlock on locked tabs ----

        private bool IsLocked(TabPage page)
        {
            return !IsTabRenameAllowed(page) || !IsTabCloseAllowed(page);
        }

        // pageToIgnore: a page that is just being removed and may still be
        // listed.
        private bool AnyTabIsLocked(TabPage pageToIgnore)
        {
            foreach (TabPage page in TabPages)
            {
                if (page != pageToIgnore && IsLocked(page))
                    return true;
            }

            return false;
        }

        // Keeps the transparent slot image in step with which tabs are
        // locked - unless the application put an image list of its own on
        // the control, which is then left alone.
        private void SyncLockSlots(TabPage pageToIgnore = null)
        {
            bool ownList = ImageList == null || ImageList == _lockSlots;

            if (!ownList)
                return;

            bool wanted = _showLockIcon && AnyTabIsLocked(pageToIgnore);

            if (wanted && ImageList == null)
            {
                if (_lockSlots == null)
                {
                    _lockSlots = new ImageList { ImageSize = new Size(LockSlotWidth, LockSlotHeight), ColorDepth = ColorDepth.Depth32Bit };

                    using (Bitmap slot = new Bitmap(LockSlotWidth, LockSlotHeight))
                    {
                        _lockSlots.Images.Add(slot);
                        _ = _lockSlots.Handle;
                    }
                }

                ImageList = _lockSlots;
            }

            // The pages' own index into the slot list, before the list goes.
            if (_lockSlots != null)
            {
                foreach (TabPage page in TabPages)
                {
                    bool hasOwnImage = (page.ImageIndex > 0) || !string.IsNullOrEmpty(page.ImageKey);
                    bool locked = _showLockIcon && page != pageToIgnore && IsLocked(page);

                    if (locked && !hasOwnImage && ImageList == _lockSlots)
                    {
                        page.ImageIndex = 0;
                    }
                    else if (!locked && page.ImageIndex == 0)
                    {
                        page.ImageIndex = -1;
                    }
                }
            }

            if (!wanted && _lockSlots != null && ImageList == _lockSlots)
            {
                ImageList = null;
            }
        }

        protected override void DrawTabImage(Graphics graphics, int index, Image image, Rectangle bounds, Color foreColor)
        {
            // The slot image is transparent - the padlock goes in its place.
            if (ImageList == _lockSlots && _lockSlots != null && IsSlotImage(index))
            {
                DrawPadlock(graphics, bounds, foreColor);
                return;
            }

            base.DrawTabImage(graphics, index, image, bounds, foreColor);
        }

        // The native tab leaves about as much room on each side of its content
        // as the padlock needs to look out of place - so the padlock and the
        // name together sit a few pixels left of the center, closing up the
        // gap on the left.
        protected override int GetTabContentOffset(int index)
        {
            return ImageList == _lockSlots && _lockSlots != null && IsSlotImage(index) ? -LockContentShift : 0;
        }

        private bool IsSlotImage(int index)
        {
            return index >= 0 && index < TabCount && IsLocked(TabPages[index]) && TabPages[index].ImageIndex == 0;
        }

        // With an image list of the application's own there is no slot, so
        // the padlock goes to the right end of the tab.
        protected override void DrawTabOverlay(Graphics graphics, int index, Rectangle bounds, Color foreColor)
        {
            if (!_showLockIcon || index < 0 || index >= TabCount || !IsLocked(TabPages[index]))
                return;

            if (ImageList == null || ImageList == _lockSlots)
                return;

            Rectangle slot = new Rectangle(bounds.Right - LockSlotWidth - 4, bounds.Top + (bounds.Height - LockSlotHeight) / 2, LockSlotWidth, LockSlotHeight);
            DrawPadlock(graphics, slot, foreColor);
        }

        // A small padlock: a body with a shackle on top, drawn in the tab's
        // own text color, a little dimmed so it stays a hint. It sits at the
        // left end of its slot (a pixel into the tab's padding) rather than
        // centered, which leaves a slightly wider gap to the name.
        private static void DrawPadlock(Graphics graphics, Rectangle slot, Color color)
        {
            Color dimmed = Color.FromArgb(190, color);
            System.Drawing.Drawing2D.SmoothingMode previous = graphics.SmoothingMode;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int bodyWidth = 7;
            int bodyHeight = 5;
            int left = slot.Left - 1;
            int bodyTop = slot.Top + slot.Height - bodyHeight - 3;

            using (SolidBrush brush = new SolidBrush(dimmed))
            {
                graphics.FillRectangle(brush, left, bodyTop, bodyWidth, bodyHeight);
            }

            using (Pen pen = new Pen(dimmed, 1.25f))
            {
                graphics.DrawArc(pen, left + 1.25f, bodyTop - 4.75f, bodyWidth - 2.5f, 6.5f, 180, 180);
                graphics.DrawLine(pen, left + 1.25f, bodyTop - 1.5f, left + 1.25f, bodyTop);
                graphics.DrawLine(pen, left + bodyWidth - 1.25f, bodyTop - 1.5f, left + bodyWidth - 1.25f, bodyTop);
            }

            graphics.SmoothingMode = previous;
        }

        // What the tooltip over a locked tab says - empty when there is
        // nothing to say (an unlocked tab, or the padlock switched off).
        internal string GetLockedToolTipText(int index)
        {
            if (!_showLockIcon || index < 0 || index >= TabCount)
                return "";

            TabPage page = TabPages[index];
            bool renameLocked = !IsTabRenameAllowed(page);
            bool closeLocked = !IsTabCloseAllowed(page);

            if (renameLocked && closeLocked)
                return UIStrings.Get("TabControl.LockedRenameAndClose");

            if (renameLocked)
                return UIStrings.Get("TabControl.LockedRename");

            return closeLocked ? UIStrings.Get("TabControl.LockedClose") : "";
        }

        private void UpdateLockToolTip(int index)
        {
            string text = GetLockedToolTipText(index);

            if (text == _lockToolTipText)
                return;

            _lockToolTipText = text;

            if (_lockToolTip == null)
            {
                _lockToolTip = new ToolTip { InitialDelay = 500, ReshowDelay = 100, AutoPopDelay = 5000 };
                _lockToolTip.ReviveOnFormActivate(this);
            }

            _lockToolTip.SetToolTip(this, text);
        }

        // ---- renaming in place ----

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

        /// <summary>
        /// Turns the name of a tab into a text box: type the new name, Enter
        /// or leaving the box applies it, Escape drops it. The tab is
        /// selected first. Returns whether the edit started - it does not
        /// without <see cref="AllowUserToRenameTabs"/>, for a tab that was
        /// locked with <see cref="SetTabRenameAllowed"/> or vetoed through
        /// <see cref="TabRenameStarting"/>.
        /// </summary>
        public bool BeginRenameTab(int index)
        {
            if (!_allowUserToRenameTabs || !IsHandleCreated || !CanRenameTab(index))
                return false;

            EndRename(commit: true);

            if (index != SelectedIndex)
            {
                SelectedIndex = index;
            }

            Rectangle tab = GetTabRect(index);
            int width = Math.Max(MinimumRenameWidth, tab.Width - 8);

            TextBox box = new TextBox
            {
                Text = TabPages[index].Text,
                BorderStyle = BorderStyle.None,
                Font = Font,
                BackColor = SelectedTabBackColor,
                ForeColor = SelectedTabForeColor,
                MaxLength = _maxTabNameLength
            };

            // A tab with an image (or the lock's slot) keeps it in front of
            // the name.
            Image tabImage = GetTabImage(TabPages[index]);
            int imageOffset = tabImage == null ? 0 : tabImage.Width + ImageTextGap;

            box.Location = new Point(tab.Left + 4 + imageOffset, tab.Top + Math.Max(0, (tab.Height - box.PreferredHeight) / 2));
            box.Width = Math.Max(MinimumRenameWidth, width - imageOffset);

            box.KeyDown += OnRenameBoxKeyDown;
            box.LostFocus += (sender, e) => EndRename(commit: true);

            _renameBox = box;
            _renameIndex = index;

            // Only a control that takes the focus makes the box lose it - a
            // click on a page's empty area or a label does not, so every
            // click outside the box has to end the edit by itself.
            _renameClickFilter = new RenameClickFilter(this, box);
            Application.AddMessageFilter(_renameClickFilter);

            // A tab control only accepts pages as children, so the box is
            // attached to its window directly.
            _ = box.Handle;
            SetParent(box.Handle, Handle);
            box.Visible = true;
            box.SelectAll();
            box.Focus();
            return true;
        }

        private void OnRenameBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                EndRename(commit: true);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                EndRename(commit: false);
            }
        }

        private void EndRename(bool commit)
        {
            TextBox box = _renameBox;

            if (box == null)
                return;

            // Cleared first: taking the box away moves the focus, which
            // would come back in here.
            int index = _renameIndex;
            _renameBox = null;
            _renameIndex = -1;

            if (_renameClickFilter != null)
            {
                Application.RemoveMessageFilter(_renameClickFilter);
                _renameClickFilter = null;
            }

            string name = box.Text.Trim();
            box.Dispose();

            if (commit && index >= 0 && index < TabCount && name.Length > 0)
            {
                ApplyName(TabPages[index], name);
            }

            if (IsHandleCreated && !Focused)
            {
                Focus();
            }
        }

        // Watches the mouse while a name is being edited: a button going down
        // anywhere but in the box itself ends the edit and applies the name.
        private sealed class RenameClickFilter : IMessageFilter
        {
            private const int WM_LBUTTONDOWN = 0x0201;
            private const int WM_RBUTTONDOWN = 0x0204;
            private const int WM_MBUTTONDOWN = 0x0207;
            private const int WM_XBUTTONDOWN = 0x020B;
            private const int WM_NCLBUTTONDOWN = 0x00A1;
            private const int WM_NCRBUTTONDOWN = 0x00A4;

            private readonly TabControl _owner;
            private readonly TextBox _box;

            public RenameClickFilter(TabControl owner, TextBox box)
            {
                _owner = owner;
                _box = box;
            }

            public bool PreFilterMessage(ref Message m)
            {
                bool isButtonDown =
                    m.Msg == WM_LBUTTONDOWN || m.Msg == WM_RBUTTONDOWN || m.Msg == WM_MBUTTONDOWN ||
                    m.Msg == WM_XBUTTONDOWN || m.Msg == WM_NCLBUTTONDOWN || m.Msg == WM_NCRBUTTONDOWN;

                if (isButtonDown && _box.IsHandleCreated && m.HWnd != _box.Handle)
                {
                    _owner.EndRename(commit: true);
                }

                // Never swallowed - the click still does what it was meant to.
                return false;
            }
        }

        private void ApplyName(TabPage page, string name)
        {
            string oldName = page.Text;

            if (name == oldName)
                return;

            TabRenamingEventArgs args = new TabRenamingEventArgs(page, oldName, name);
            TabRenaming?.Invoke(this, args);

            if (args.Cancel || string.IsNullOrWhiteSpace(args.NewName))
                return;

            page.Text = args.NewName.Trim();
            TabRenamed?.Invoke(this, new TabRenamedEventArgs(page, oldName));
        }
    }
}
