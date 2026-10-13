using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // What the library draws: the rows of the list and the closed box (border, text, arrow).
    public partial class ComboBox
    {
        // ---- the rows of the list ----

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            // On a focus change the system repaints the selection field of the closed box
            // itself, asking for it as an item (state ComboBoxEdit) and not through WM_PAINT.
            // Drawn as a row it came out a few pixels off, and the text of a box that just lost
            // the focus jumped for a moment. The closed box is drawn the one way instead.
            if (OwnsClosedPainting && (e.State & DrawItemState.ComboBoxEdit) != 0)
            {
                PaintBuffered(e.Graphics);

                return;
            }

            if (e.Index >= 0)
                DrawRow(e);

            base.OnDrawItem(e);
        }

        private void DrawRow(DrawItemEventArgs e)
        {
            // ComboBoxEdit marks the box's own display area (redrawn whenever the control
            // has the focus) as opposed to a row inside the open list - without excluding it
            // the closed box would stay highlighted the whole time it has the focus.
            bool isDisplayArea = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool isSelected = !isDisplayArea && (e.State & DrawItemState.Selected) != 0;

            // The selected (or hovered) row: the set's color with the set's text, or the
            // neutral gray of the other controls' selections without a set.
            Color backColor = !isSelected
                ? (isDisplayArea ? BackColor : InputField.Background)
                : ResolvedColors != null ? UIColors.GetReadableFill(ResolvedColors.Hover, ResolvedColors.HoverText) : UIColors.BorderLight;

            // A disabled box draws its text muted.
            Color textColor = !Enabled
                ? UIColors.GetDisabledText(backColor)
                : isSelected
                    ? (ResolvedColors != null ? ResolvedColors.HoverText : UIColors.GetContrastingForeColor(backColor))
                    : UIColors.TextPrimary;

            using (SolidBrush backBrush = new SolidBrush(backColor))
                e.Graphics.FillRectangle(backBrush, e.Bounds);

            // GetItemText, not ToString: it respects DisplayMember like the native box does.
            object item = e.Index < Items.Count ? Items[e.Index] : null;

            TextRenderer.DrawText(
                e.Graphics,
                GetItemText(item),
                Font,
                e.Bounds,
                textColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }

        // Drawn into an off-screen buffer and put on the screen in one go: drawing the fill and
        // then the text straight onto the window showed as flicker whenever the box repainted
        // (while items were loaded, say). The buffer is a compatible bitmap, so the text keeps
        // its ClearType, and every pixel of it is painted before it is shown.
        private void PaintBuffered(IntPtr hdc)
        {
            using (Graphics target = Graphics.FromHdc(hdc))
                PaintBuffered(target);
        }

        private void PaintBuffered(Graphics target)
        {
            bool isEditable = OwnsEditableFramePainting;

            using (BufferedGraphics buffer = _bufferContext.Allocate(target, ClientRectangle))
            {
                PaintClosed(buffer.Graphics, !isEditable);

                // The text field is a window of its own on top: never painted over.
                if (isEditable)
                    target.SetClip(GetTextFieldBounds(), CombineMode.Exclude);

                buffer.Render(target);
            }
        }

        // The border in the current state: the 3:1 outline at rest; hovered the set's color,
        // dropped down the set's stronger one - each brought to its contrast against the box,
        // so the state is seen as a stronger border in both themes. The focus does not change
        // it: after a pick with the mouse the box keeps the focus and must look like any other.
        internal Color BorderColor
        {
            get
            {
                if (!Enabled)
                    return UIColors.DisabledGray;

                UIColorSet colors = GetColors();

                if (DroppedDown)
                    return ListBorderColor;

                if (_isHot)
                    return UIColors.EnsureContrast(colors.Hover, BackColor, 4.5);

                return UIColors.GetOutlineColor(BackColor);
            }
        }

        private void PaintClosed(Graphics g, bool withText = true)
        {
            Rectangle bounds = ClientRectangle;

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            using (SolidBrush back = new SolidBrush(BackColor))
                g.FillRectangle(back, bounds);

            bool isActive = Enabled && (DroppedDown || _isHot);
            Color border = BorderColor;
            int borderWidth = isActive ? Scale(2) : 1;

            using (Pen pen = new Pen(border, borderWidth))
            {
                float half = borderWidth / 2f;
                g.DrawRectangle(pen, half, half, bounds.Width - borderWidth, bounds.Height - borderWidth);
            }

            // The layout uses the widest border, whatever the state: the text and the arrow must
            // not move when the border gets thicker on hover.
            int inset = Scale(2);
            int arrowWidth = Scale(ArrowWidth96);
            Rectangle arrowArea = new Rectangle(bounds.Right - arrowWidth - inset, bounds.Top, arrowWidth, bounds.Height);
            Color textColor = Enabled ? ForeColor : UIColors.GetDisabledText(BackColor);
            Color arrowColor = !Enabled ? UIColors.DisabledGray : isActive ? border : textColor;

            DrawArrow(g, arrowArea, arrowColor);

            int padding = Scale(TextPadding96);
            Rectangle textArea = new Rectangle(
                bounds.Left + inset + padding,
                bounds.Top + inset,
                Math.Max(0, arrowArea.Left - bounds.Left - inset - padding),
                bounds.Height - inset * 2);

            if (!withText)
                return;

            string text = SelectedIndex >= 0 ? GetItemText(SelectedItem) : "";

            if (!string.IsNullOrEmpty(text))
            {
                TextRenderer.DrawText(
                    g,
                    text,
                    Font,
                    textArea,
                    textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }

            // The dotted frame of the keyboard focus, around the text - only while the keyboard is
            // what is being used: a box that keeps the focus after a pick with the mouse does not
            // show it.
            if (Focused && ShowFocusCues && !_lastInputWasMouse)
            {
                int textWidth = string.IsNullOrEmpty(text) ? 0 : Math.Min(textArea.Width, TextRenderer.MeasureText(text, Font).Width);
                Rectangle frame = new Rectangle(textArea.Left - 2, textArea.Top + 2, textWidth + 4, textArea.Height - 4);
                ControlPaint.DrawFocusRectangle(g, frame, textColor, BackColor);
            }
        }

        // A small chevron pointing down, in the middle of the arrow area.
        private void DrawArrow(Graphics g, Rectangle area, Color color)
        {
            float width = Scale(8);
            float height = Scale(4);
            float cx = area.Left + area.Width / 2f;
            float cy = area.Top + area.Height / 2f;

            SmoothingMode previous = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (Pen pen = new Pen(color, Scale(2)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                g.DrawLines(pen, new[]
                {
                    new PointF(cx - width / 2f, cy - height / 2f),
                    new PointF(cx, cy + height / 2f),
                    new PointF(cx + width / 2f, cy - height / 2f)
                });
            }

            g.SmoothingMode = previous;
        }
    }
}
