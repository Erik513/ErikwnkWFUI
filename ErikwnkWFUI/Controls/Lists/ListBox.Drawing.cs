using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // The painting of the list box: the items, their texts and icons, the handle, the drop line, and which colors
    // an item has in which state. The settings, the mouse and the colors' properties are in ListBox.cs.
    public partial class ListBox
    {
        /// <inheritdoc/>
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            Rectangle rect = e.Bounds;
            object item = Items[e.Index];

            // A list that is disabled as a whole shows every item the way a disabled item looks.
            bool isSelected = (e.State & DrawItemState.Selected) != 0 && !_isDragging;
            bool isHovered = e.Index == _hoverIndex && !_isDragging && Enabled;
            bool isDisabled = !Enabled || _isItemDisabled?.Invoke(item) == true;

            Color backColor = GetBackColor(e.Index, isSelected, isHovered, isDisabled);
            Color textColor = GetTextColorOn(isSelected, isDisabled, backColor);

            FillItemBackground(e.Graphics, rect, backColor);

            Rectangle dragRect = Rectangle.Empty;

            if (CanReorder)
            {
                dragRect = GetDragHandleRectangle(rect);
                DrawDragHandle(e.Graphics, dragRect, GetDragHandleColor(textColor, backColor));
            }

            DrawItemContent(e.Graphics, rect, item, e.Index, textColor, dragRect);

            // In a multiple selection the item that has the focus can be one that is not selected: the keyboard
            // needs to see where it is (a single selection shows it by the selection itself).
            if (SelectionMode != SelectionMode.One && Focused && ShowFocusCues && (e.State & DrawItemState.Focus) != 0)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(rect, -1, -1), textColor, backColor);

            if (_isDragging && _dragInsertPosition != -1)
            {
                if (_dragInsertPosition == e.Index)
                {
                    DrawIndicatorLine(e.Graphics, rect.Top);
                }
                else if (_dragInsertPosition >= Items.Count && e.Index == Items.Count - 1)
                {
                    DrawIndicatorLine(e.Graphics, rect.Bottom);
                }
            }

            base.OnDrawItem(e);
        }

        private void FillItemBackground(Graphics graphics, Rectangle rect, Color backColor)
        {
            using (SolidBrush brush = new SolidBrush(backColor))
            {
                graphics.FillRectangle(brush, rect);
            }
        }

        private void DrawItemContent(
            Graphics graphics,
            Rectangle rect,
            object item,
            int index,
            Color textColor,
            Rectangle dragRect)
        {
            int textLeft = rect.X + Px(8);
            int textRightMargin = Px(12);
            int reservedDragWidth = CanReorder ? dragRect.Width : 0;

            if (_showEnumeration)
            {
                textLeft = DrawEnumeration(graphics, rect, index, textLeft, textColor);
            }

            Image icon = _iconProvider?.Invoke(item);

            if (icon != null)
            {
                textLeft = DrawIcon(graphics, rect, icon, textLeft);
            }

            Rectangle textRect = new Rectangle(
                textLeft,
                rect.Y,
                rect.Width - (textLeft - rect.X) - reservedDragWidth - textRightMargin,
                rect.Height);

            DrawText(graphics, textRect, GetDisplayText(item), textColor);
        }

        private int DrawEnumeration(Graphics graphics, Rectangle rect, int index, int textLeft, Color textColor)
        {
            string numberText = $"{index + 1}.";
            int numberWidth = TextRenderer.MeasureText(graphics, numberText, Font, Size.Empty, ItemTextFlags).Width;

            Rectangle numberRect = new Rectangle(textLeft, rect.Y, numberWidth + Px(4), rect.Height);

            TextRenderer.DrawText(graphics, numberText, Font, numberRect, textColor, ItemTextFlags);

            return textLeft + numberRect.Width + Px(4);
        }

        private int DrawIcon(Graphics graphics, Rectangle rect, Image icon, int textLeft)
        {
            Rectangle iconRect = new Rectangle(
                textLeft,
                rect.Y + (rect.Height - Px(ItemIconSize)) / 2,
                Px(ItemIconSize),
                Px(ItemIconSize));

            graphics.DrawImage(icon, iconRect);

            return textLeft + Px(ItemIconSize) + Px(ItemIconMargin);
        }

        private void DrawText(Graphics graphics, Rectangle textRect, string text, Color textColor)
        {
            TextRenderer.DrawText(graphics, text, Font, textRect, textColor, ItemTextFlags);
        }

        // The handle of an item: the color that was set, or the text color of the item blended toward its background
        // (quiet, like a hint) and kept at 3:1 against it - on the gray selection and on a green one alike.
        private Color GetDragHandleColor(Color textColor, Color backColor)
        {
            if (_dragHandleColor.HasValue)
                return _dragHandleColor.Value;

            return UIColors.EnsureContrast(UIColors.BlendToward(textColor, backColor, 0.4), backColor, 3.0);
        }

        private void DrawDragHandle(Graphics graphics, Rectangle dragRect, Color color)
        {
            using (Pen pen = new Pen(color, Px(3) / 2f))
            {
                int centerY = dragRect.Y + dragRect.Height / 2;
                int centerX = dragRect.X + dragRect.Width / 2;
                int startX = centerX - Px(6);

                int lineHeight = Px(3);
                int spacing = Px(1);

                for (int i = 0; i < 3; i++)
                {
                    int y = centerY - lineHeight - spacing + i * (lineHeight + spacing);
                    graphics.DrawLine(pen, startX, y, startX + Px(12), y);
                }
            }
        }

        private void DrawIndicatorLine(Graphics graphics, int y)
        {
            using (Pen pen = new Pen(DragIndicatorColor, Px(3)))
            {
                // ClientSize, not Width - Width is this control's FULL
                // outer bounds, which includes a native vertical
                // scrollbar's own strip when one is visible. A line drawn
                // all the way to Width would run underneath that
                // scrollbar instead of stopping at the actual content
                // area's own right edge.
                graphics.DrawLine(pen, 0, y, ClientSize.Width, y);
            }
        }

        private string GetDisplayText(object item)
        {
            if (_displayTextProvider != null)
                return _displayTextProvider(item) ?? "";

            // The text the standard list box makes of an item: DisplayMember, FormatString and the like are
            // followed, an item without any of them is its ToString().
            return item == null ? "" : GetItemText(item) ?? "";
        }

        private Color GetBackColor(int index, bool isSelected, bool isHovered, bool isDisabled)
        {
            // The selection of a disabled list: the selection color, dimmed toward the row it is on (the gray of a
            // disabled item is the same as the odd rows on the light theme, which would hide it).
            if (isSelected && !Enabled)
                return UIColors.BlendToward(GetSelectedBackColor(index), GetRowBackColor(index), 0.6);

            if (isSelected)
                return GetSelectedBackColor(index);

            if (isHovered)
                return _hoverBackColor ?? GetHoverBackColor(GetRowBackColor(index));

            if (isDisabled && Enabled)
                return DisabledBackColor;

            return GetRowBackColor(index);
        }

        private Color GetHoverBackColor(Color row)
        {
            return UIColors.BlendToward(row, ItemForeColor, 0.08);
        }

        // The row with the selection tint over it (what the eye sees: the one fill the item is painted with).
        private Color GetSelectedBackColor(int index)
        {
            Color row = GetRowBackColor(index);
            Color tint = _selectedBackColor.Value;
            bool isGray = UIColors.IsNeutral(tint);
            double alpha = (isGray ? SelectionAlphaGray : SelectionAlphaColor) / 255.0;

            return Color.FromArgb(
                (int)Math.Round(row.R + (tint.R - row.R) * alpha),
                (int)Math.Round(row.G + (tint.G - row.G) * alpha),
                (int)Math.Round(row.B + (tint.B - row.B) * alpha));
        }

        private Color GetRowBackColor(int index)
        {
            return index % 2 == 0
                ? ItemBackColor
                : UIColors.Darken(ItemBackColor, 5);
        }

        // The text color of an item in a state, on the first row (the painting asks GetTextColorOn with the background it
        // really has; the tests ask here).
        private Color GetTextColor(bool isSelected, bool isDisabled)
        {
            return GetTextColorOn(isSelected, isDisabled, isSelected ? GetSelectedBackColor(0) : ItemBackColor);
        }

        // The text on a background of the given color. A disabled one that was not given a color of its own is
        // worked out from the background (the theme's disabled gray is too faint on some of them).
        private Color GetTextColorOn(bool isSelected, bool isDisabled, Color backColor)
        {
            // The selection is only a tint over the row, so the text of the item reads on it as it does on the
            // row, unless a SelectedForeColor was set.
            if (!Enabled)
                return _disabledForeColor ?? UIColors.GetDisabledText(backColor);

            if (isSelected)
                return SelectedForeColor;

            if (isDisabled)
                return _disabledForeColor ?? UIColors.GetDisabledText(backColor);

            return ItemForeColor;
        }
    }
}
