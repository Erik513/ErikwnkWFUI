using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // Renaming a tab in place with a text box.
    public partial class TabControl : ReadOnlyTabControl
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

        /// <summary>
        /// Turns the name of a tab into a text box: type the new name, Enter
        /// or leaving the box applies it, Escape drops it. The tab is
        /// selected first. Returns whether the edit started - it does not
        /// without <see cref="AllowUserToRenameTabs"/>, for a tab that was
        /// locked with <see cref="SetTabRenamable"/> or vetoed through
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
