using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A custom-drawn window title bar: logo, title text, minimize/maximize/
    /// close buttons, and window dragging (including drag-to-maximize and
    /// double-click-to-maximize) - all reimplemented in managed code since it
    /// replaces the native one. <see cref="ErikwnkWFUI.Forms.AppForm"/> already creates and
    /// docks one of these for you; construct it directly only if you're
    /// building a custom top-level window from scratch instead of deriving
    /// from AppForm.
    /// </summary>
    public class TitleBarControl : System.Windows.Forms.Panel
    {
        private const int TitleBarHeight = 30;
        private const double TitleTextContrast = 4.5;
        private const int MinimumCenteredTitleWidth = 60;
        private const int IconSize = TitleBarHeight;
        private const int IconLeftMargin = 0;
        private const int TitleHorizontalPadding = 10;

        private static readonly Size ButtonSize = new Size(42, 30);

        private readonly bool _allowWindowSnapAndMaximize;

        private Label _titleLabel;
        private PictureBox _iconPictureBox;
        private Button _minimizeButton;
        private Button _maximizeButton;
        private Button _closeButton;
        private bool _isMaximized;

        private FormDragHandle _titleBarDragHandle;
        private FormDragHandle _titleLabelDragHandle;
        private FormDragHandle _iconDragHandle;

        private Form _parentForm;

        /// <summary>Raised with the position on the screen when the bar (its title, its logo) is right-clicked: the window shows its system menu.</summary>
        internal event EventHandler<Point> SystemMenuRequested;

        private void RaiseSystemMenuOnRightClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                SystemMenuRequested?.Invoke(this, Cursor.Position);
        }

        /// <summary>The window title text.</summary>
        public string Title
        {
            get { return _titleLabel.Text; }
            set { _titleLabel.Text = value ?? ""; }
        }

        /// <summary>The small logo at the top-left, or null to hide it entirely (default). See ErikwnkWFUI/README.md for how to load this from your own embedded resources.</summary>
        public Image IconImage
        {
            get { return _iconPictureBox.Image; }
            set
            {
                _iconPictureBox.Image = value;
                _iconPictureBox.Visible = value != null;
                UpdateLayout();
            }
        }

        /// <summary>The title text's color.</summary>
        public Color TitleForeColor
        {
            get { return _titleLabel.ForeColor; }
            set
            {
                _titleForeColorWasSet = true;
                _titleLabel.ForeColor = value;
            }
        }

        public ContentAlignment TitleTextAlign
        {
            get { return _titleLabel.TextAlign; }
            set
            {
                _titleLabel.TextAlign = value;
                UpdateLayout();
            }
        }

        public bool ShowMinimizeButton
        {
            get { return _minimizeButton.Visible; }
            set
            {
                _minimizeButton.Visible = value;
                UpdateLayout();
            }
        }

        public bool ShowMaximizeButton
        {
            get { return _maximizeButton.Visible; }
            set
            {
                _maximizeButton.Visible = value;
                UpdateLayout();
            }
        }

        public bool ShowCloseButton
        {
            get { return _closeButton.Visible; }
            set
            {
                _closeButton.Visible = value;
                UpdateLayout();
            }
        }

        /// <summary>allowWindowSnapAndMaximize controls both double-click-to-maximize and drag-to-top-of-screen-to-maximize.</summary>
        public TitleBarControl(
            Image icon = null,
            string title = "",
            ContentAlignment titleTextAlign = ContentAlignment.MiddleCenter,
            bool showMinimizeButton = true,
            bool showMaximizeButton = true,
            bool showCloseButton = true,
            bool allowWindowSnapAndMaximize = true,
            Color? backColor = null)
            : this(icon, title, titleTextAlign, showMinimizeButton, showMaximizeButton, showCloseButton, allowWindowSnapAndMaximize, backColor, null, null)
        {
        }

        // The form hands over what its color set (or the primary look) makes of the bar: the background (asked again each time, so a change of the
        // theme or of the accent is followed - unless a color was given by hand), and the set the title and the buttons take their colors from.
        internal TitleBarControl(
            Image icon,
            string title,
            ContentAlignment titleTextAlign,
            bool showMinimizeButton,
            bool showMaximizeButton,
            bool showCloseButton,
            bool allowWindowSnapAndMaximize,
            Color? backColor,
            Func<Color> backSource,
            Func<UIColorSet> colors)
        {
            Color resolvedBackColor = backColor ?? (backSource != null ? backSource() : UIColors.BackgroundBlack);
            _backColorWasGiven = backColor.HasValue || backSource != null;
            _allowWindowSnapAndMaximize = allowWindowSnapAndMaximize;

            ConfigureControl(resolvedBackColor);

            // What the buttons are read against: the bar's color, worked out apart from the bar so that a change of the theme cannot show them
            // the old one.
            Func<Color> barColor = backColor.HasValue
                ? (Func<Color>)(() => backColor.Value)
                : (backSource ?? (() => UIColors.BackgroundBlack));

            _titleLabel = CreateTitleLabel(
                title,
                titleTextAlign,
                resolvedBackColor);
            _iconPictureBox = CreateIconPictureBox(icon);
            _minimizeButton = CreateTitleBarButton(UIStyles.Symbols.Minimize, UIStrings.Get("TitleBar.Minimize"), showMinimizeButton, barColor, colors, false);
            _maximizeButton = CreateTitleBarButton(UIStyles.Symbols.Maximize, UIStrings.Get("TitleBar.Maximize"), showMaximizeButton, barColor, colors, false);
            _closeButton = CreateTitleBarButton(UIStyles.Symbols.Close, UIStrings.Get("TitleBar.Close"), showCloseButton, barColor, colors, true);

            if (backSource != null && !backColor.HasValue)
            {
                ThemeWatcher.FollowBackColor(this, backSource);
                ThemeWatcher.FollowBackColor(_titleLabel, backSource);
            }

            // The text of the set, brought to where it reads on the bar (the bar's color is looked at each time).
            if (colors != null)
            {
                _titleForeColorWasSet = true;
                ThemeWatcher.FollowForeColor(_titleLabel, () => UIColors.EnsureContrast(colors().BasicText, BackColor, TitleTextContrast));
            }

            AddControls();
            CreateDragHandles();
            WireControlEvents();

            UpdateLayout();
            RunWhenHandleReady(UpdateLayout);

            ThemeWatcher.Attach(this, OnThemeChanged);
        }

        private bool _backColorWasGiven;
        private bool _titleForeColorWasSet;

        // The theme changed while the bar is shown: the colors that were not given by hand follow (the buttons
        // follow by themselves, they are made by the button factory).
        private void OnThemeChanged()
        {
            if (!_backColorWasGiven)
            {
                BackColor = UIColors.BackgroundBlack;
                _titleLabel.BackColor = UIColors.BackgroundBlack;
            }

            if (!_titleForeColorWasSet)
                _titleLabel.ForeColor = UIColors.TextPrimary;

            Invalidate(true);
        }

        // While maximized the button restores the window, and says so.
        public void UpdateMaximizeButton(bool isMaximized)
        {
            if (_maximizeButton == null)
                return;

            _isMaximized = isMaximized;
            _maximizeButton.Text = isMaximized ? UIStyles.Symbols.Restore : UIStyles.Symbols.Maximize;
            UIButtonFactory.UpdateTooltip(_maximizeButton, GetMaximizeTooltip());
        }

        private string GetMaximizeTooltip()
        {
            return UIStrings.Get(_isMaximized ? "TitleBar.Restore" : "TitleBar.Maximize");
        }

        public void DisposeDragHandle()
        {
            if (_titleBarDragHandle != null)
            {
                _titleBarDragHandle.Dispose();
                _titleBarDragHandle = null;
            }

            if (_titleLabelDragHandle != null)
            {
                _titleLabelDragHandle.Dispose();
                _titleLabelDragHandle = null;
            }

            if (_iconDragHandle != null)
            {
                _iconDragHandle.Dispose();
                _iconDragHandle = null;
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible)
                return;

            RunWhenHandleReady(UpdateLayout);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnwireControlEvents();
                UnwireParentFormEvents();
                DisposeDragHandle();
            }

            base.Dispose(disposing);
        }

        private void ConfigureControl(Color backColor)
        {
            Height = TitleBarHeight;
            Dock = DockStyle.Top;
            BackColor = backColor;
            Visible = true;
            Margin = new Padding(0);
            Padding = new Padding(0);

            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.ResizeRedraw,
                true);

            UpdateStyles();
        }

        private Label CreateTitleLabel(
            string title,
            ContentAlignment textAlign,
            Color backColor)
        {
            return new Label
            {
                Text = title ?? "",
                AutoSize = false,
                BackColor = backColor,
                ForeColor = UIColors.TextPrimary,
                Font = UIFonts.Title,
                TextAlign = textAlign,
                Padding = new Padding(TitleHorizontalPadding, 0, TitleHorizontalPadding, 0),
                AutoEllipsis = true,
                UseMnemonic = false
            };
        }

        private PictureBox CreateIconPictureBox(Image icon)
        {
            return new PictureBox
            {
                Image = icon,
                Size = new Size(IconSize, IconSize),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Visible = icon != null
            };
        }

        private Button CreateTitleBarButton(string text, string tooltip, bool visible, Func<Color> barColor, Func<UIColorSet> colors, bool isClose)
        {
            // Flat and in the color of the bar; the same symbols in every window.
            Button button = UIButtonFactory.CreateCaptionButton(text, tooltip, ButtonSize, barColor, colors, isClose);
            button.Visible = visible;

            // Shows only a symbol: named by its tooltip for a screen reader.
            button.AccessibleName = tooltip;

            return button;
        }

        private void AddControls()
        {
            Controls.Add(_titleLabel);
            Controls.Add(_iconPictureBox);
            Controls.Add(_minimizeButton);
            Controls.Add(_maximizeButton);
            Controls.Add(_closeButton);

            _titleLabel.SendToBack();
            _iconPictureBox.BringToFront();
            _minimizeButton.BringToFront();
            _maximizeButton.BringToFront();
            _closeButton.BringToFront();
        }

        private void WireControlEvents()
        {
            Resize += OnTitleBarResize;
            ParentChanged += OnTitleBarParentChanged;
            HandleCreated += OnTitleBarHandleCreated;

            _minimizeButton.Click += OnMinimizeButtonClick;
            _maximizeButton.Click += OnMaximizeButtonClick;
            _closeButton.Click += OnCloseButtonClick;

            UIStrings.LanguageChanged += OnUIStringsLanguageChanged;
        }

        private void UnwireControlEvents()
        {
            Resize -= OnTitleBarResize;
            ParentChanged -= OnTitleBarParentChanged;
            HandleCreated -= OnTitleBarHandleCreated;

            if (_minimizeButton != null)
                _minimizeButton.Click -= OnMinimizeButtonClick;

            if (_maximizeButton != null)
                _maximizeButton.Click -= OnMaximizeButtonClick;

            if (_closeButton != null)
                _closeButton.Click -= OnCloseButtonClick;

            UIStrings.LanguageChanged -= OnUIStringsLanguageChanged;
        }

        private void OnUIStringsLanguageChanged(object sender, EventArgs e)
        {
            UIButtonFactory.UpdateTooltip(_minimizeButton, UIStrings.Get("TitleBar.Minimize"));
            UIButtonFactory.UpdateTooltip(_maximizeButton, GetMaximizeTooltip());
            UIButtonFactory.UpdateTooltip(_closeButton, UIStrings.Get("TitleBar.Close"));
        }

        private void CreateDragHandles()
        {
            DisposeDragHandle();

            MouseUp -= RaiseSystemMenuOnRightClick;
            MouseUp += RaiseSystemMenuOnRightClick;
            _titleLabel.MouseUp -= RaiseSystemMenuOnRightClick;
            _titleLabel.MouseUp += RaiseSystemMenuOnRightClick;
            _iconPictureBox.MouseUp -= RaiseSystemMenuOnRightClick;
            _iconPictureBox.MouseUp += RaiseSystemMenuOnRightClick;

            _titleBarDragHandle = new FormDragHandle(this, _allowWindowSnapAndMaximize);
            _titleLabelDragHandle = new FormDragHandle(_titleLabel, _allowWindowSnapAndMaximize);

            if (_iconPictureBox != null)
                _iconDragHandle = new FormDragHandle(_iconPictureBox, _allowWindowSnapAndMaximize);
        }

        private void UpdateLayout()
        {
            if (_closeButton == null || _maximizeButton == null || _minimizeButton == null || _iconPictureBox == null || _titleLabel == null)
                return;

            int right = Width;

            right = PositionButtonFromRight(_closeButton, right);
            right = PositionButtonFromRight(_maximizeButton, right);
            right = PositionButtonFromRight(_minimizeButton, right);

            int left = 0;

            if (_iconPictureBox.Visible)
            {
                _iconPictureBox.Location = new Point(IconLeftMargin, 0);
                _iconPictureBox.BringToFront();

                left = IconLeftMargin + IconSize;
            }

            UpdateTitleLabelBounds(left, right);

            _titleLabel.SendToBack();
        }

        private void UpdateTitleLabelBounds(int leftLimit, int rightLimit)
        {
            int labelLeft;
            int labelWidth;

            if (IsTitleAlignedLeft(_titleLabel.TextAlign))
            {
                labelLeft = leftLimit;
                labelWidth = Math.Max(0, rightLimit - leftLimit);
            }
            else if (IsTitleAlignedRight(_titleLabel.TextAlign))
            {
                labelLeft = 0;
                labelWidth = Math.Max(0, rightLimit);
            }
            else
            {
                // Centred in the room the buttons and the logo leave, as far as the bar allows: the same margin on both sides, the larger of
                // the two, so that the title stays in the middle of the bar - and never goes under a button when the window is narrow.
                int margin = Math.Max(leftLimit, Width - rightLimit);

                if (Width - 2 * margin >= MinimumCenteredTitleWidth)
                {
                    labelLeft = margin;
                    labelWidth = Width - 2 * margin;
                }
                else
                {
                    // Too narrow for that: the title takes whatever room the logo and the buttons leave (it is still centred in it).
                    labelLeft = leftLimit;
                    labelWidth = Math.Max(0, rightLimit - leftLimit);
                }
            }

            _titleLabel.Bounds = new Rectangle(
                labelLeft,
                0,
                labelWidth,
                Height);
        }

        private bool IsTitleAlignedLeft(ContentAlignment alignment)
        {
            return alignment == ContentAlignment.TopLeft ||
                   alignment == ContentAlignment.MiddleLeft ||
                   alignment == ContentAlignment.BottomLeft;
        }

        private bool IsTitleAlignedRight(ContentAlignment alignment)
        {
            return alignment == ContentAlignment.TopRight ||
                   alignment == ContentAlignment.MiddleRight ||
                   alignment == ContentAlignment.BottomRight;
        }

        private int PositionButtonFromRight(Button button, int right)
        {
            if (!button.Visible)
                return right;

            button.Location = new Point(right - ButtonSize.Width, 0);
            button.BringToFront();

            return right - ButtonSize.Width;
        }

        private void AttachToParentFormWhenReady()
        {
            Form form = FindForm();

            if (form == null)
            {
                RunWhenHandleReady(AttachToParentFormWhenReady);
                return;
            }

            if (_parentForm == form)
            {
                UpdateMaximizeButton(form.WindowState == FormWindowState.Maximized);
                return;
            }

            UnwireParentFormEvents();

            _parentForm = form;
            _parentForm.Resize += OnParentFormResize;

            UpdateMaximizeButton(_parentForm.WindowState == FormWindowState.Maximized);
        }

        private void UnwireParentFormEvents()
        {
            if (_parentForm == null)
                return;

            _parentForm.Resize -= OnParentFormResize;
            _parentForm = null;
        }

        private void RunWhenHandleReady(Action action)
        {
            if (action == null)
                return;

            if (IsDisposed)
                return;

            if (IsHandleCreated)
            {
                BeginInvoke(action);
                return;
            }

            EventHandler handler = null;

            handler = delegate
            {
                HandleCreated -= handler;

                if (!IsDisposed && IsHandleCreated)
                    BeginInvoke(action);
            };

            HandleCreated += handler;
        }

        private void ToggleMaximize()
        {
            if (_parentForm == null || !_allowWindowSnapAndMaximize)
                return;

            _parentForm.WindowState = _parentForm.WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
        }

        private void OnTitleBarResize(object sender, EventArgs e)
        {
            UpdateLayout();
        }

        private void OnTitleBarParentChanged(object sender, EventArgs e)
        {
            AttachToParentFormWhenReady();
        }

        private void OnTitleBarHandleCreated(object sender, EventArgs e)
        {
            AttachToParentFormWhenReady();
        }

        private void OnParentFormResize(object sender, EventArgs e)
        {
            Form form = _parentForm ?? FindForm();

            if (form == null)
                return;

            _parentForm = form;
            UpdateMaximizeButton(form.WindowState == FormWindowState.Maximized);
        }

        private void OnMinimizeButtonClick(object sender, EventArgs e)
        {
            if (_parentForm != null)
                _parentForm.WindowState = FormWindowState.Minimized;
        }

        private void OnMaximizeButtonClick(object sender, EventArgs e)
        {
            ToggleMaximize();
        }

        private void OnCloseButtonClick(object sender, EventArgs e)
        {
            if (_parentForm != null)
                _parentForm.Close();
        }
    }
}
