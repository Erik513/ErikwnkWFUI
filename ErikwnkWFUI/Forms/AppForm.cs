using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Helpers;
using FlowLayoutPanel = System.Windows.Forms.FlowLayoutPanel;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// A <see cref="Form"/> with ErikwnkWFUI's own title bar (<see cref="TitleBar"/>) instead of the native Windows one: no frame, resizable
    /// by its edges, with a system menu (right-click on the title bar, Alt+Space), a drop shadow, and the work area of the screen when it
    /// is maximized. This is the base class every ErikwnkWFUI window is built on - make one with <c>UIStyles.Forms.CreateStandard</c>,
    /// <c>CreateDialog</c> or <c>CreateSettings</c>, or derive from it and hand <see cref="FormOptions"/> to the constructor, the way
    /// <see cref="MessageBoxForm"/> and <see cref="UpdateAvailableForm"/> do. Add your own controls to <see cref="ContentPanel"/>, not
    /// directly to the form - the title bar occupies the top of the form's own <see cref="Control.Controls"/>. It opens in the middle of the
    /// screen, or over its owner when it has one (<c>StartPosition</c> can be changed). <see cref="FormTitle"/> sets
    /// the title of the window and of the bar at once; <see cref="RememberBoundsKey"/> makes the window come back where it was.
    /// </summary>
    public partial class AppForm : Form
    {
        private readonly TitleBarControl _titleBar;
        private readonly Panel _contentPanel;
        private readonly FlowLayoutPanel _versionStrip;
        private readonly Label _versionLabel;
        private readonly BorderlessResizeHandler _resizeHandler;
        private readonly bool _allowMinimizeFromTaskbar;
        private readonly bool _shadow;
        private readonly bool _closeOnEscape;
        private Color _borderColor;

        /// <summary>The panel below the title bar - add your own UI here.</summary>
        public Panel ContentPanel
        {
            get { return _contentPanel; }
        }

        /// <summary>
        /// The reserved bottom-right strip holding <see cref="VersionLabel"/>,
        /// or null when it wasn't reserved at all (see
        /// <see cref="FormOptions.VersionText"/>). Flows right-to-left,
        /// so <c>VersionStrip.Controls.Add(someControl)</c> places it
        /// directly to the left of the version text - e.g. an
        /// <see cref="AppUpdater.CreateUpdateAvailableButton(ErikwnkCore.Updater.UpdateCheckResult, System.Version, Form)"/> button.
        /// </summary>
        public FlowLayoutPanel VersionStrip
        {
            get { return _versionStrip; }
        }

        /// <summary>
        /// The reserved bottom-right version strip, or null when
        /// <see cref="FormOptions.VersionText"/> wasn't set - reach
        /// through here to restyle it (font, color) after construction.
        /// </summary>
        public Label VersionLabel
        {
            get { return _versionLabel; }
        }

        /// <summary>
        /// The title bar itself - reach through here for anything not
        /// already proxied by <see cref="FormTitle"/>/<see cref="FormIcon"/>,
        /// e.g. <c>TitleBar.TitleForeColor</c>.
        /// </summary>
        public TitleBarControl TitleBar
        {
            get { return _titleBar; }
        }

        /// <summary>
        /// The color of the thin edge around the window (a window with the native frame has none): black - the darkest background of the theme,
        /// which follows a change of the theme - for every window until a color is set; <see cref="Color.Empty"/> goes back to that. The title
        /// bar and the version strip have their own colors (<see cref="FormOptions.Colors"/>).
        /// </summary>
        public Color BorderColor
        {
            get { return ResolveBorderColor(); }
            set
            {
                _borderColor = value;
                BackColor = ResolveBorderColor();
            }
        }

        private Color ResolveBorderColor()
        {
            return _borderColor.IsEmpty ? UIColors.BackgroundBlack : _borderColor;
        }

        /// <summary>The title of the window: the <see cref="Form.Text"/> (taskbar, Alt-Tab) and the title bar show the same text, whichever of the two is set. <c>TitleBar.Title</c> changes the bar alone.</summary>
        public string FormTitle
        {
            get { return Text; }
            set { Text = value ?? ""; }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);

            if (_titleBar != null)
                _titleBar.Title = Text;
        }

        /// <summary>Shorthand for <c>TitleBar.IconImage</c> - the small logo shown at the top-left of the title bar, not the OS-level icon (see <see cref="FormOptions.WindowIcon"/> for that).</summary>
        public Image FormIcon
        {
            get { return _titleBar.IconImage; }
            set { _titleBar.IconImage = value; }
        }

        /// <summary>
        /// Builds the form from a full <see cref="FormOptions"/> - use
        /// <see cref="FormOptions.CreateStandard"/> or
        /// <see cref="FormOptions.CreateDialog"/> as a starting point.
        /// Passing null is the same as <c>FormOptions.CreateStandard()</c>.
        /// </summary>
        public AppForm(FormOptions options = null)
        {
            options = options ?? FormOptions.CreateStandard();

            Text = options.Title;

            if (options.WindowIcon != null)
            {
                Icon = options.WindowIcon;
                ShowIcon = true;
            }
            else
            {
                try
                {
                    Icon extractedIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

                    if (extractedIcon != null)
                    {
                        Icon = extractedIcon;
                        ShowIcon = true;
                    }
                }
                catch
                {
                    // No icon can be read from the executable: the window simply has none.
                }
            }

            _allowMinimizeFromTaskbar = options.Borderless && options.ShowMinimizeButton;
            _shadow = options.Borderless && options.Shadow;
            _closeOnEscape = options.CloseOnEscape;

            // Opens in the middle of the screen - or over its owner (see OnLoad); a window of its own can change StartPosition.
            StartPosition = FormStartPosition.CenterScreen;

            // A window that can be resized has a least size (the title bar with its buttons and a little content), as a normal form has.
            if (options.Resizable)
                MinimumSize = new Size(Dpi.Scale(options.MinimumSize.Width, DeviceDpi), Dpi.Scale(options.MinimumSize.Height, DeviceDpi));
            _canMinimize = options.ShowMinimizeButton;
            _canMaximize = options.ShowMaximizeButton;
            _isFixedSize = !options.Resizable;
            _menuColors = options.ColorSource;
            RememberBoundsKey = options.RememberBoundsKey;

            if (options.Borderless)
            {
                FormBorderStyle = FormBorderStyle.None;
            }
            else
            {
                // The native frame has the title bar and its buttons: they are the ones of the form.
                FormBorderStyle = options.Resizable ? FormBorderStyle.Sizable : FormBorderStyle.FixedDialog;
                MinimizeBox = options.ShowMinimizeButton;
                MaximizeBox = options.ShowMaximizeButton && options.Resizable;
                ControlBox = options.ShowCloseButton || MinimizeBox || MaximizeBox;
            }

            DoubleBuffered = true;
            // The edge around the window is black (the darkest background of the theme) unless a color was asked for; the title bar and the strip
            // are in the color of the set.
            _borderColor = options.BorderColor ?? Color.Empty;
            ThemeWatcher.FollowBackColor(this, ResolveBorderColor);
            Func<Color> bar = options.BarSource ?? (() => UIColors.BackgroundBlack);

            int borderSize = 0;
            if (options.Borderless)
                borderSize = options.Resizable ? 2 : 1;
            Padding = new Padding(borderSize);

            _titleBar = new TitleBarControl(
                options.Icon,
                options.Title,
                options.TitleTextAlign,
                options.ShowMinimizeButton,
                options.ShowMaximizeButton,
                options.ShowCloseButton,
                options.AllowWindowSnapAndMaximize,
                options.TitleBarBackColor,
                bar,
                options.ColorSource);

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            ThemeWatcher.FollowBackColor(_contentPanel, () => UIColors.BackgroundMedium);

            string versionText = options.VersionText;

            if (!string.IsNullOrEmpty(versionText))
            {
                _versionLabel = UILabelFactory.CreateMuted(versionText);
                _versionLabel.AutoSize = true;
                _versionLabel.TextAlign = ContentAlignment.MiddleRight;
                _versionLabel.Margin = new Padding(0, 4, 0, 0);

                _versionStrip = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 24,
                    FlowDirection = FlowDirection.RightToLeft,
                    WrapContents = false,
                    AutoSize = false,
                    Margin = new Padding(0),
                    Padding = new Padding(0, 0, 10, 0)
                    // Explicit, not left to the transparent background of the
                    // label - the strip's own edges (outside the label's now
                    // AutoSize width) would otherwise show through with the
                    // FlowLayoutPanel's default gray SystemColors.Control
                    // instead of matching the rest of the window.
                };
                // Not the color of the title bar: a darker gray than the content, the same in every window.
                ThemeWatcher.FollowBackColor(_versionStrip, () => UIColors.BackgroundDark);

                // Whatever is put into the strip (the update button, say) stands in the middle of its height, as the version text does.
                _versionStrip.Layout += (sender, e) => CenterVersionStripControls();
                _versionStrip.ControlAdded += (sender, e) => e.Control.SizeChanged += (changed, args) => QueueVersionStripLayout();
                _versionStrip.Controls.Add(_versionLabel);
            }

            Controls.Add(_contentPanel);

            // With the native frame there is only one title bar. The bar of the library is not in the window then (TitleBar, FormTitle and
            // FormIcon stay there to be used, but show nothing).
            if (options.Borderless)
            {
                Controls.Add(_titleBar);
                _titleBar.SystemMenuRequested += (sender, position) => ShowSystemMenu(position);
            }

            if (_versionStrip != null)
                Controls.Add(_versionStrip);

            if (options.Borderless && options.Resizable)
                _resizeHandler = new BorderlessResizeHandler(this);
        }

        private const int WmGetMinMaxInfo = 0x24;
        private const int WsMinimizeBox = 0x00020000;
        private const int ClassStyleDropShadow = 0x00020000;

        // A window without a frame has no minimize box for the system: a click on its button in the taskbar would not minimize it. The window
        // that has a minimize button of its own gets the style (nothing is drawn for it).
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;

                if (_allowMinimizeFromTaskbar)
                    parameters.Style |= WsMinimizeBox;

                if (_shadow)
                    parameters.ClassStyle |= ClassStyleDropShadow;

                return parameters;
            }
        }

        // Escape closes a dialog that has no cancel button of its own (a text box that is being edited, or a list that is open, takes the key first).
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape && _closeOnEscape && CancelButton == null)
            {
                Close();

                return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        protected override void Dispose(bool disposing)
        {
            // The bar that is not in the window (native frame) is not disposed with the controls.
            if (disposing && _titleBar != null && _titleBar.Parent == null)
                _titleBar.Dispose();

            if (disposing && _openMenu != null)
                _openMenu.Dispose();

            base.Dispose(disposing);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MinMaxInfo
        {
            public NativePoint Reserved;
            public NativePoint MaxSize;
            public NativePoint MaxPosition;
            public NativePoint MinTrackSize;
            public NativePoint MaxTrackSize;
        }

        // A control that changes its size while the strip is laid out (the text getting its font) asks for the layout again afterwards: a margin that
        // changes in the middle of a layout does not start one.
        private bool _versionStripLayoutQueued;

        private void QueueVersionStripLayout()
        {
            if (_versionStripLayoutQueued || !IsHandleCreated || IsDisposed)
                return;

            _versionStripLayoutQueued = true;

            BeginInvoke(new Action(() =>
            {
                _versionStripLayoutQueued = false;

                if (!IsDisposed)
                    _versionStrip.PerformLayout();
            }));
        }

        // A flow panel puts its controls at the top: the margins are worked out so that each one is centered in the strip (the layout is
        // started again only when one of them changes).
        private void CenterVersionStripControls()
        {
            foreach (Control control in _versionStrip.Controls)
            {
                int top = Math.Max(0, (_versionStrip.ClientSize.Height - control.Height) / 2);

                if (control.Margin.Top != top || control.Margin.Bottom != 0)
                    control.Margin = new Padding(control.Margin.Left, top, control.Margin.Right, 0);
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WmGetMinMaxInfo && FormBorderStyle == FormBorderStyle.None && MaximumSize.IsEmpty)
                FitMaximizedToWorkArea(m.LParam);

            if (_resizeHandler != null)
                _resizeHandler.TryHandleMessage(ref m);
        }

        // A form without a frame is as big as the whole screen when it is maximized - over the taskbar. It is given the work area of its
        // screen instead (a MaximumSize of the form stays what it was: the system takes that into account itself).
        private void FitMaximizedToWorkArea(IntPtr minMaxInfo)
        {
            Screen screen = IsHandleCreated ? Screen.FromControl(this) : Screen.PrimaryScreen;
            Rectangle position;

            GetMaximizedPlacement(screen.WorkingArea, screen.Bounds, out position);

            MinMaxInfo info = (MinMaxInfo)Marshal.PtrToStructure(minMaxInfo, typeof(MinMaxInfo));
            info.MaxPosition = new NativePoint { X = position.X, Y = position.Y };
            info.MaxSize = new NativePoint { X = position.Width, Y = position.Height };
            Marshal.StructureToPtr(info, minMaxInfo, false);
        }

        /// <summary>Where a maximized frameless window goes: the work area of the screen, with the position relative to the top left corner of the screen (as the system asks for it).</summary>
        internal static void GetMaximizedPlacement(Rectangle workingArea, Rectangle screenBounds, out Rectangle placement)
        {
            placement = new Rectangle(workingArea.X - screenBounds.X, workingArea.Y - screenBounds.Y, workingArea.Width, workingArea.Height);
        }
    }
}
