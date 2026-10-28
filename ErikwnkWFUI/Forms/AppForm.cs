using System.Drawing;
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
    /// A <see cref="Form"/> with ErikwnkWFUI's own custom-drawn title bar
    /// (<see cref="TitleBar"/>) instead of the native Windows one, and
    /// optional borderless resizing. This is the base class every ErikwnkWFUI
    /// window is built on - either use it directly (<c>UIStyles.Forms.CreateStandard(title)</c>
    /// for a quick window, or the <see cref="FormOptions"/> constructor
    /// for full control), or derive from it the way
    /// <see cref="MessageBoxForm"/> and <see cref="UpdateAvailableForm"/>
    /// do. Add your own controls to <see cref="ContentPanel"/>, not directly
    /// to the form - the title bar already occupies the top of the form's
    /// own <see cref="Control.Controls"/> collection.
    /// </summary>
    public class AppForm : Form
    {
        private readonly TitleBarControl _titleBar;
        private readonly Panel _contentPanel;
        private readonly FlowLayoutPanel _versionStrip;
        private readonly Label _versionLabel;
        private readonly BorderlessResizeHandler _resizeHandler;

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

        /// <summary>Shorthand for <c>TitleBar.Title</c>.</summary>
        public string FormTitle
        {
            get { return _titleBar.Title; }
            set { _titleBar.Title = value ?? ""; }
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

            if (options.Borderless)
                FormBorderStyle = FormBorderStyle.None;

            DoubleBuffered = true;
            ThemeWatcher.FollowBackColor(this, () => UIColors.BackgroundBlack);

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
                options.TitleBarBackColor);

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
                ThemeWatcher.FollowBackColor(_versionStrip, () => UIColors.BackgroundBlack);
                _versionStrip.Controls.Add(_versionLabel);
            }

            Controls.Add(_contentPanel);
            Controls.Add(_titleBar);

            if (_versionStrip != null)
                Controls.Add(_versionStrip);

            if (options.Borderless && options.Resizable)
                _resizeHandler = new BorderlessResizeHandler(this);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (_resizeHandler != null)
                _resizeHandler.TryHandleMessage(ref m);
        }
    }
}
