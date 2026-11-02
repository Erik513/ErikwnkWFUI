using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Native;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// What <see cref="InfoPopupForm"/> and <see cref="ToastForm"/> have in common: a borderless
    /// window that is never in the taskbar, never takes the activation, has smoothly rounded corners
    /// and a thin line around it (and, as Windows 11 would give a window it rounds itself, no shadow),
    /// is see-through by a <see cref="PopupTransparency"/> and moves with the window it belongs to.
    /// </summary>
    /// <remarks>
    /// The window is a layered window with an alpha value for every pixel: what the form and its controls draw is
    /// taken into a bitmap, cut to the rounded shape with smoothed edges, and handed to Windows as the picture of the
    /// window (<c>UpdateLayeredWindow</c>). It is made again whenever the window is painted (that is, whenever something
    /// in it is invalidated). The standard <see cref="Form.Opacity"/> is not used, since it cannot be combined with that.
    /// </remarks>
    public abstract class PopupFormBase : Form
    {
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private PopupTransparency _transparency = PopupTransparency.Medium;
        private double _opacity = 1.0;

        internal PopupFormBase()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            _opacity = _transparency.ToOpacity();
        }

        /// <summary>Moves the popup with the window it is shown for.</summary>
        internal OwnerFollower Follower { get; } = new OwnerFollower();

        /// <summary>How see-through the popup is: not at all, slightly, clearly (the default) or strongly.</summary>
        [DefaultValue(PopupTransparency.Medium)]
        public PopupTransparency Transparency
        {
            get { return _transparency; }
            set
            {
                _transparency = value;

                if (IsDisposed)
                    return;

                Opacity = value.ToOpacity();
                OnTransparencyChanged();
            }
        }

        /// <summary>
        /// How opaque the whole popup is, from 0 (not there) to 1 (not see-through). Hides the opacity of the form, which
        /// is not used here (see the remarks of the class).
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new double Opacity
        {
            get { return _opacity; }
            set
            {
                _opacity = Math.Max(0.0, Math.Min(1.0, value));

                if (IsHandleCreated)
                    UpdateLayer();
            }
        }

        /// <summary>Called when the opacity has changed: the colors that depend on it are to be set again.</summary>
        protected abstract void OnTransparencyChanged();

        /// <summary>The radius of the rounded corners at 96 dpi (scaled with the screen).</summary>
        protected virtual int CornerRadius96
        {
            get { return 12; }
        }

        /// <summary>The thin line around the popup: the text color of its background, faded most of the way into it.</summary>
        protected virtual Color BorderColor
        {
            get { return UIColors.BlendToward(UIColors.GetBestTextColor(BackColor), BackColor, BorderFade); }
        }

        // How far the line around the popup is faded into the background (it is a quiet outline, not a frame).
        private const double BorderFade = 0.7;

        private int GetCornerRadius()
        {
            return Math.Min(Dpi.Scale(this, CornerRadius96), Math.Min(Width, Height) / 2);
        }

        // A popup must never take the activation away from the window it is shown for (the window
        // of a slider that is being dragged, say).
        /// <inheritdoc/>
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        // The same for a click on the popup: it does not activate either. And the picture of the window comes from us.
        /// <inheritdoc/>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WS_EX_NOACTIVATE | WS_EX_LAYERED;

                return parameters;
            }
        }

        // Windows 11 rounds a window itself (smoothed) and gives it a shadow, which follows a popup that moves with the mouse
        // late and at the wrong place - so its own rounding is switched off, the corners are cut by RenderLayer.
        private void SwitchOffSystemRounding()
        {
            int preference = DWMWCP_DONOTROUND;

            try
            {
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            }
            catch (EntryPointNotFoundException)
            {
                // before Windows 11 there is nothing to switch off
            }
            catch (DllNotFoundException)
            {
                // no desktop window manager
            }
        }

        /// <summary>
        /// The picture of the window: what the form and its controls draw, cut to the rounded shape with smoothed edges
        /// and with the thin line around it. 32 bits with an alpha value per pixel (premultiplied); not yet made see-through
        /// by <see cref="Opacity"/>.
        /// </summary>
        internal Bitmap RenderLayer()
        {
            int width = Math.Max(1, Width);
            int height = Math.Max(1, Height);

            // Drawn into a bitmap without alpha: what the controls draw with GDI (text) leaves the alpha of a 32-bit bitmap at 0.
            using (Bitmap content = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            {
                DrawToBitmap(content, new Rectangle(0, 0, width, height));

                Bitmap layer = new Bitmap(width, height, PixelFormat.Format32bppPArgb);

                using (Graphics graphics = Graphics.FromImage(layer))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.PixelOffsetMode = PixelOffsetMode.Half; // (a pixel covers from its edge to the next one, not from the middle of it)

                    float radius = GetCornerRadius();
                    float thickness = Math.Max(1, Dpi.Scale(this, 1));

                    using (GraphicsPath shape = RoundedRectangle.CreatePath(new RectangleF(0, 0, width, height), radius))
                    using (TextureBrush picture = new TextureBrush(content))
                    {
                        graphics.FillPath(picture, shape);
                    }

                    // A line along the middle of the outermost pixels.
                    RectangleF line = new RectangleF(thickness / 2, thickness / 2, width - thickness, height - thickness);

                    using (GraphicsPath outline = RoundedRectangle.CreatePath(line, Math.Max(0, radius - thickness / 2)))
                    using (Pen pen = new Pen(BorderColor, thickness))
                    {
                        graphics.DrawPath(pen, outline);
                    }
                }

                return layer;
            }
        }

        // Hands the picture to Windows. Not while the window is hidden (it is made when it is shown), being made, or empty.
        private bool _updatingLayer;

        private void UpdateLayer()
        {
            if (!IsHandleCreated || IsDisposed || !Visible || _updatingLayer || Width <= 0 || Height <= 0)
                return;

            _updatingLayer = true;

            try
            {
                using (Bitmap layer = RenderLayer())
                {
                    LayeredWindow.Update(Handle, layer, (byte)Math.Round(_opacity * 255));
                }
            }
            finally
            {
                _updatingLayer = false;
            }
        }

        /// <inheritdoc/>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SwitchOffSystemRounding();
            UpdateLayer();
        }

        /// <inheritdoc/>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            UpdateLayer();
        }

        // A layered window shows the picture Windows was given, not what it paints itself: whenever something in the
        // window (or the window itself) is invalidated - a text, a color, the size - the picture is made again. (A paint
        // message would be the natural moment, but it does not come for a window that was hidden or shown meanwhile.)
        /// <inheritdoc/>
        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            base.OnInvalidated(e);
            UpdateLayer();
        }

        /// <inheritdoc/>
        protected override void WndProc(ref Message m)
        {
            // What the window paints itself is not shown: the message is answered, nothing more.
            if (m.Msg == WM_PAINT && IsHandleCreated)
            {
                NativePaintStruct paint;
                BeginPaint(Handle, out paint);
                EndPaint(Handle, ref paint);

                m.Result = IntPtr.Zero;
                return;
            }

            base.WndProc(ref m);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Follower.Detach();

            base.Dispose(disposing);
        }
    }
}
