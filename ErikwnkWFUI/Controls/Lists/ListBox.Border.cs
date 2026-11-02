using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // The frame of the list: a thin line around the list, so that it is seen against the window also where its rows have the color of the
    // window (the light theme). It is the border of the window of the list (BorderStyle.FixedSingle, the default), painted over after the system
    // has painted its own; BorderStyle.None leaves the list without a frame (the list inside a TitledListBox, which frames the title too).
    public partial class ListBox
    {
        // Follows the theme until it is set: the neutral border color, in every variant (a frame in the color of a set or of the accent would be
        // very striking around a whole list).
        private readonly ThemeColor _borderColor = new ThemeColor(() => UIColors.BorderMedium);

        /// <summary>
        /// The color of the frame (<see cref="System.Windows.Forms.ListBox.BorderStyle"/> <c>FixedSingle</c>, the default). Left alone it is the
        /// neutral gray border color of the theme in every variant, also for a list made with a color set or <c>CreatePrimary</c>; set it for a frame in
        /// another color (<c>UIStyles.Colors.Sets.Green.Hover</c>, say).
        /// </summary>
        public Color BorderColor
        {
            get { return _borderColor.Value; }
            set
            {
                _borderColor.Set(value);
                RedrawFrame();
            }
        }

        private bool ShouldSerializeBorderColor()
        {
            return _borderColor.IsSet;
        }

        private void ResetBorderColor()
        {
            _borderColor.Reset();
            RedrawFrame();
        }

        internal void RedrawFrame()
        {
            if (IsHandleCreated)
                RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE);
        }

        // After the system has painted the frame (on the screen: WM_NCPAINT; into a picture, a print or a screenshot of the window: WM_PRINT with the
        // non-client part): the line in the color of the list.
        private void PaintBorder()
        {
            if (BorderStyle != BorderStyle.FixedSingle || !IsHandleCreated)
                return;

            IntPtr hdc = GetWindowDC(Handle);

            if (hdc == IntPtr.Zero)
                return;

            try
            {
                PaintBorder(hdc);
            }
            finally
            {
                ReleaseDC(Handle, hdc);
            }
        }

        private void PaintBorder(IntPtr hdc)
        {
            using (Graphics graphics = Graphics.FromHdc(hdc))
            using (Pen pen = new Pen(BorderColor))
            {
                graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void PaintBorderOfPrint(Message m)
        {
            if (BorderStyle == BorderStyle.FixedSingle && ((long)m.LParam & PRF_NONCLIENT) != 0 && m.WParam != IntPtr.Zero)
                PaintBorder(m.WParam);
        }
    }
}