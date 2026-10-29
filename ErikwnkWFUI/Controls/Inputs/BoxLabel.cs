using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// What the check box and the radio button draw the same way besides their box: the page behind them, the label and the frame of the
    /// keyboard focus. (They cannot share a base class - each is a different standard control - so the shared parts live here.)
    /// </summary>
    internal static class BoxLabel
    {
        /// <summary>The color of the page behind the box: the first opaque background up the parents.</summary>
        public static Color GetParentBackColor(Control box)
        {
            for (Control parent = box.Parent; parent != null; parent = parent.Parent)
            {
                if (parent.BackColor.A == 255)
                    return parent.BackColor;
            }

            return UIColors.BackgroundMedium;
        }

        // A mnemonic ("&Save") shows its underline only once the Alt key was used, as on any box; without mnemonics the "&" is just a character.
        private static TextFormatFlags GetTextFlags(ButtonBase box, bool showKeyboardCues)
        {
            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter;

            if (!box.UseMnemonic)
                flags |= TextFormatFlags.NoPrefix;
            else if (!showKeyboardCues)
                flags |= TextFormatFlags.HidePrefix;

            return flags;
        }

        /// <summary>
        /// The label, next to the box: after it with the box on the left, from the left edge with the box on the right. Disabled text is the
        /// contrasting color blended toward the page, as on buttons (the fixed gray of a disabled box is too faint for text on the light theme).
        /// </summary>
        public static void PaintText(Graphics g, ButtonBase box, Color parentBackColor, bool boxOnRight, bool showKeyboardCues)
        {
            if (string.IsNullOrEmpty(box.Text))
                return;

            CheckBox.Metrics metrics = new CheckBox.Metrics(box.DeviceDpi);
            int glyphColumn = metrics.GlyphColumnWidth;
            Rectangle textArea = boxOnRight
                ? new Rectangle(metrics.BoxLeftMargin, 0, box.Width - glyphColumn, box.Height)
                : new Rectangle(glyphColumn, 0, box.Width - glyphColumn, box.Height);

            Color textColor = box.Enabled
                ? UIColors.TextPrimary
                : UIColors.BlendToward(UIColors.GetContrastingForeColor(parentBackColor), parentBackColor, 0.35);

            TextRenderer.DrawText(g, box.Text, box.Font, textArea, textColor, GetTextFlags(box, showKeyboardCues));
        }

        /// <summary>
        /// The dotted frame of the keyboard focus around the label and the box (the box alone without a label; the whole width with the box on
        /// the right) - only once the keyboard has been used.
        /// </summary>
        public static void PaintFocus(Graphics g, ButtonBase box, Color parentBackColor, bool boxOnRight, bool showFocusCues)
        {
            if (!box.Focused || !showFocusCues)
                return;

            CheckBox.Metrics metrics = new CheckBox.Metrics(box.DeviceDpi);
            Rectangle area;

            if (string.IsNullOrEmpty(box.Text))
            {
                int around = metrics.Scale(3);
                area = new Rectangle(
                    (box.Width - metrics.BoxSize) / 2 - around,
                    (box.Height - metrics.BoxSize) / 2 - around,
                    metrics.BoxSize + around * 2,
                    metrics.BoxSize + around * 2);
            }
            else
            {
                int textWidth = TextRenderer.MeasureText(box.Text, box.Font).Width;
                int width = boxOnRight
                    ? box.Width - 2
                    : Math.Min(box.Width - 2, metrics.GlyphColumnWidth + textWidth + 2);
                area = new Rectangle(1, 1, width, box.Height - 2);
            }

            area.Intersect(new Rectangle(0, 0, box.Width, box.Height));
            ControlPaint.DrawFocusRectangle(g, area, UIColors.TextPrimary, parentBackColor);
        }
    }
}
