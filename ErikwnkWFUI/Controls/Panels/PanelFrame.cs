using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The color of the frame of a panel that has a border (<see cref="BorderStyle.FixedSingle"/>): the border is drawn by
    /// the system, in its own color, and is painted over in the library's (see <see cref="InputField"/>). Shared by the three
    /// themed panels.
    /// </summary>
    internal sealed class PanelFrame
    {
        private readonly Panel _panel;
        private readonly ThemeColor _color;

        public PanelFrame(Panel panel)
        {
            _panel = panel;

            // The theme's line color - or one that is seen on the color of this panel.
            _color = new ThemeColor(() => UIColors.GetLineColor(SurfaceColor.Of(panel)));
            InputField.AttachFrame(panel, () => panel.BorderStyle != BorderStyle.None, () => _color.Value);
        }

        /// <summary>Whether the panel has a frame: <see cref="BorderStyle.FixedSingle"/> (on) or none (off); any other style counts as on and is left as it is.</summary>
        public bool ShowBorder
        {
            get { return _panel.BorderStyle != BorderStyle.None; }
            set
            {
                if (value != ShowBorder)
                    _panel.BorderStyle = value ? BorderStyle.FixedSingle : BorderStyle.None;
            }
        }

        /// <summary>
        /// To be called after the window procedure of the panel: where both scroll bars meet, the system leaves a light square
        /// even with dark bars, which is painted over once the frame (the non-client area) is painted.
        /// </summary>
        public void AfterWindowMessage(ref Message m)
        {
            if (m.Msg == WM_NCPAINT)
                NativeControlTheme.PaintScrollBarCorner(_panel);
        }

        /// <summary>The color of the frame; follows the theme until it is set.</summary>
        public Color Color
        {
            get { return _color.Value; }
            set
            {
                if (value.IsEmpty)
                    _color.Reset();
                else
                    _color.Set(value);

                if (_panel.IsHandleCreated)
                    RedrawWindow(_panel.Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE);
            }
        }
    }
}
