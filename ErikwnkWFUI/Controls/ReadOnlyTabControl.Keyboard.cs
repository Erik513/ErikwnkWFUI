using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // Selection and keys while disabled tabs may not be selected.
    public partial class ReadOnlyTabControl
    {
        protected override void OnSelecting(TabControlCancelEventArgs e)
        {
            // Before the event goes out, so a handler sees it already refused.
            if (!_allowSelectingDisabledTabs && e.TabPage != null && !e.TabPage.Enabled)
            {
                e.Cancel = true;
            }

            base.OnSelecting(e);
        }

        // Arrow keys along the strip. Natively they would step onto a disabled
        // tab, be refused and stay put, never getting past it - so they skip
        // disabled tabs here instead.
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!_allowSelectingDisabledTabs && !e.Control && !e.Alt)
            {
                bool vertical = IsVertical();
                Keys forward = vertical ? Keys.Down : Keys.Right;
                Keys backward = vertical ? Keys.Up : Keys.Left;

                if (e.KeyCode == forward || e.KeyCode == backward)
                {
                    SelectEnabledTab(e.KeyCode == forward ? 1 : -1, wrap: false);
                    e.Handled = true;
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        // Ctrl+Tab, Ctrl+Shift+Tab and Ctrl+PageDown / Ctrl+PageUp, which
        // the tab control turns into a selection change itself.
        protected override bool ProcessKeyPreview(ref Message m)
        {
            const int WM_KEYDOWN = 0x0100;

            if (!_allowSelectingDisabledTabs && m.Msg == WM_KEYDOWN && (ModifierKeys & Keys.Control) != 0)
            {
                Keys key = (Keys)(int)m.WParam;
                bool shift = (ModifierKeys & Keys.Shift) != 0;

                if (key == Keys.Tab || key == Keys.PageDown || key == Keys.PageUp)
                {
                    bool forward = key == Keys.PageDown || (key == Keys.Tab && !shift);
                    SelectEnabledTab(forward ? 1 : -1, wrap: true);
                    return true;
                }
            }

            return base.ProcessKeyPreview(ref m);
        }

        // Selects the next tab in a direction whose page is enabled; nothing
        // when there is none. Wraps around the ends where asked to.
        private void SelectEnabledTab(int direction, bool wrap)
        {
            int count = TabCount;

            if (count == 0)
                return;

            int index = SelectedIndex;

            for (int step = 0; step < count; step++)
            {
                index += direction;

                if (index < 0 || index >= count)
                {
                    if (!wrap)
                        return;

                    index = (index + count) % count;
                }

                if (index == SelectedIndex)
                    return;

                if (TabPages[index].Enabled)
                {
                    SelectedIndex = index;
                    return;
                }
            }
        }
    }
}
