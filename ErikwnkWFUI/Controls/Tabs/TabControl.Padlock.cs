using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    // The padlock on locked tabs, and the tooltip over a tab.
    public partial class TabControl : ReadOnlyTabControl
    {
        private bool IsLocked(TabPage page)
        {
            return !IsTabRenamable(page) || !IsTabClosable(page);
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

        // The native tab leaves about 12 px of padding on each side of its
        // content. The padlock and the name together sit a few pixels left of
        // the center, which closes up the gap on the left.
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
            bool renameLocked = !IsTabRenamable(page);
            bool closeLocked = !IsTabClosable(page);

            if (renameLocked && closeLocked)
                return UIStrings.Get("TabControl.LockedRenameAndClose");

            if (renameLocked)
                return UIStrings.Get("TabControl.LockedRename");

            return closeLocked ? UIStrings.Get("TabControl.LockedClose") : "";
        }

        // The tooltip over a tab: what the base says (the full name of a cut-off
        // tab) and, below it, what is locked.
        protected override string GetTabToolTipText(int index)
        {
            string name = base.GetTabToolTipText(index);
            string locked = GetLockedToolTipText(index);

            if (name.Length > 0 && locked.Length > 0)
                return name + Environment.NewLine + locked;

            return name.Length > 0 ? name : locked;
        }
    }
}
