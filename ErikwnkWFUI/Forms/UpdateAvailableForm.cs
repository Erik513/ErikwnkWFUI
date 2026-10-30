using System.ComponentModel;
using System;
using ErikwnkWFUI.Controls;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;
using FlowLayoutPanel = System.Windows.Forms.FlowLayoutPanel;
using Panel = System.Windows.Forms.Panel;
using TableLayoutPanel = System.Windows.Forms.TableLayoutPanel;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// The dialog behind <c>UpdatePrompt.ShowUpdateAvailableAsync</c> - prefer calling that over constructing this directly: it shows this dialog
    /// and then, when the user chose to update, the progress of the download. After <c>ShowDialog</c> <see cref="UpdateChosen"/> says whether
    /// "Update now" was clicked and <see cref="SkipChosen"/> whether "Skip this version" was. <see cref="ReleaseNotes"/> adds a "What's new" button
    /// that opens the text of the release under the versions.
    /// </summary>
    public class UpdateAvailableForm : AppForm
    {
        private readonly FormSizeOption _size;
        private readonly string _currentVersion;
        private readonly string _latestVersion;

        private Control _promptContent;
        private Control _buttonPanel;
        private TableLayoutPanel _mainLayout;
        private TextBox _notesBox;
        private Button _notesButton;
        private Button _skipButton;
        private Button _updateButton;
        private Button _laterButton;
        private Label _headline;
        private TableLayoutPanel _buttonTable;
        private readonly bool _defaultTitle;
        private Panel _notesHost;
        private Panel _notesFrame;
        private bool _notesFocused;
        private int _columnsWidth;
        private string _releaseNotes = "";
        private bool _notesExpanded;
        private bool _allowSkip;

        /// <summary>Whether the user clicked "Update now" (the dialog is closed with <c>DialogResult.Yes</c> then); false for Later, Skip and for closing it.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool UpdateChosen { get; private set; }

        /// <summary>Whether the user clicked "Skip this version" (the dialog is closed with <c>DialogResult.Ignore</c> then). The button is there only with <see cref="AllowSkip"/>.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool SkipChosen { get; private set; }

        /// <param name="currentVersion">The version that runs now, as it is shown ("1.2.0").</param>
        /// <param name="latestVersion">The new version.</param>
        /// <param name="title">The title of the window ("Update available" or "Update verfügbar" when none is given).</param>
        /// <param name="size">One of the three sizes (<see cref="FormSize.Medium"/> by default) or an own <see cref="Size"/>; in pixels as they are.</param>
        public UpdateAvailableForm(
            string currentVersion,
            string latestVersion,
            string title = null,
            FormSizeOption? size = null)
            : this(
                FormOptions.CreateDialog(
                    title: title ?? UIStrings.Get("Update.AvailableTitle"),
                    titleTextAlign: ContentAlignment.MiddleLeft,
                    icon: MessageBoxForm.GetTitleBarIcon(MessageBoxIcon.Info)),
                currentVersion,
                latestVersion,
                size)
        {
        }

        // The window with the options it is given (a dialog made by FormOptions, with the color set or the accent of the title bar): what
        // UIStyles.Forms.CreateUpdateAvailableStandard / CreateUpdateAvailablePrimary make.
        internal UpdateAvailableForm(FormOptions options, string currentVersion, string latestVersion, FormSizeOption? size)
            : base(options)
        {
            // The title of the library follows the language while the window is open; a title that was given stays.
            _defaultTitle = options == null || options.Title == UIStrings.Get("Update.AvailableTitle");

            _currentVersion = WithoutV(currentVersion);
            _latestVersion = WithoutV(latestVersion);
            _size = size ?? FormSize.Medium;

            ShowInTaskbar = false;
            BuildLayout();
            LayoutContent();
            UIStrings.LanguageChanged += OnLanguageChanged;
        }

        /// <summary>The text of the release ("What's new"): with it the window has a "What's new" button that opens the text under the versions. Empty (the default) has no button. Plain text; the Markdown of a GitHub release is shown as it is written.</summary>
        [DefaultValue("")]
        public string ReleaseNotes
        {
            get { return _releaseNotes; }
            set
            {
                _releaseNotes = value == null ? "" : value.Trim();
                _notesBox.Text = _releaseNotes.Replace("\r\n", "\n").Replace("\n", "\r\n");
                _notesButton.Visible = _releaseNotes.Length > 0;

                if (_releaseNotes.Length == 0)
                    _notesExpanded = false;

                UpdateNotesButton();
                LayoutContent();
            }
        }

        /// <summary>Whether the text of <see cref="ReleaseNotes"/> is open (the window is higher then). Closed by default.</summary>
        [DefaultValue(false)]
        public bool NotesExpanded
        {
            get { return _notesExpanded; }
            set
            {
                bool expanded = value && _releaseNotes.Length > 0;

                if (expanded == _notesExpanded)
                    return;

                _notesExpanded = expanded;
                UpdateNotesButton();
                LayoutContent();
            }
        }

        /// <summary>Whether the window has a "Skip this version" button (the program then does not ask again for this version). Off by default.</summary>
        [DefaultValue(false)]
        public bool AllowSkip
        {
            get { return _allowSkip; }
            set
            {
                _allowSkip = value;
                _skipButton.Visible = value;
                LayoutContent();
            }
        }

        private void BuildLayout()
        {
            Panel rootPanel = UIStyles.Panels.CreateMedium();

            // Rows: the versions (the rest of the room), the text of the release (closed: none) and the buttons.
            _mainLayout = UITableLayoutPanelFactory.CreateStandard(1, 3);
            _mainLayout.Dock = DockStyle.Fill;
            ThemeWatcher.FollowBackColor(_mainLayout, () => UIColors.BackgroundMedium);
            _mainLayout.Padding = new Padding(0);
            _mainLayout.Margin = new Padding(0);
            _mainLayout.RowStyles.Clear();
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, ButtonRowHeight));

            Panel contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            _promptContent = CreatePromptContent();
            contentHost.Controls.Add(_promptContent);

            _notesBox = UIStyles.TextBoxes.CreateStandard("");
            _notesBox.Multiline = true;
            _notesBox.ReadOnly = true;
            _notesBox.Dock = DockStyle.Fill;

            // The frame is a panel around the box (the box has none): a frame that the system paints as part of the box is overpainted
            // by its scroll bar, and the bar is shown only when the text does not fit (see UpdateNotesScrollBars).
            _notesBox.BorderStyle = BorderStyle.None;
            _notesBox.ScrollBars = ScrollBars.None;
            _notesBox.Enter += (sender, e) => SetNotesFocused(true);
            _notesBox.Leave += (sender, e) => SetNotesFocused(false);
            _notesFrame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(1) };
            _notesFrame.Controls.Add(_notesBox);
            ThemeWatcher.Attach(_notesFrame, UpdateNotesFrame);

            // (While the notes are closed the frame has no window and hears nothing of a change of the theme: it is worked out again when it is shown.)
            _notesFrame.VisibleChanged += (sender, e) => UpdateNotesFrame();
            UpdateNotesFrame();

            // Shown only while the notes are open: closed, the row has no height, and a box that is only clipped away would still be
            // a stop of the Tab key.
            _notesHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(24, 0, 24, 8),
                Visible = false
            };
            _notesHost.Controls.Add(_notesFrame);

            _buttonPanel = CreateButtonPanel();

            _mainLayout.Controls.Add(contentHost, 0, 0);
            _mainLayout.Controls.Add(_notesHost, 0, 1);
            _mainLayout.Controls.Add(_buttonPanel, 0, 2);

            rootPanel.Controls.Add(_mainLayout);

            ContentPanel.Controls.Clear();
            ContentPanel.Controls.Add(rootPanel);

            UpdateNotesButton();

            // The keyboard starts on "Update now" (not on the box of the notes, which would show its focus frame at once).
            ActiveControl = _updateButton;
        }

        private const int ButtonRowHeight = 60;

        // The window writes the "v" itself: a version that comes with one (a tag such as "v1.3.0") is not shown with two.
        private static string WithoutV(string version)
        {
            return (version ?? "").Trim().TrimStart('v', 'V');
        }

        // The size of the window: the size that was asked for, higher by the room of the release notes when they are open, and wider when the
        // buttons of the left (What's new, Skip) and the two of the right do not fit side by side (an own size is kept).
        private void LayoutContent()
        {
            if (_buttonPanel == null)
                return;

            Size baseSize = _size.Resolve(FormKind.Update);
            int notesHeight = _notesExpanded ? GetNotesHeight() : 0;
            int width = baseSize.Width;

            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;

            if (!_size.Custom.HasValue)
            {
                // The frame is measured with the window at the width of its size (the sizes of the parts are only then up to date).
                Size = new Size(width, Height);

                // (Their own state, not Visible: a control of a window that is not shown yet is never Visible.)
                int left = (_releaseNotes.Length > 0 ? _notesButton.Width : 0) + (_allowSkip ? _skipButton.Width : 0);
                int frame = Width - ContentPanel.ClientSize.Width;
                width = Math.Max(width, frame + _buttonPanel.Padding.Horizontal + left + _columnsWidth + 8);
            }

            _mainLayout.RowStyles[1].Height = notesHeight;
            _notesHost.Visible = _notesExpanded;
            UpdateNotesScrollBars();
            Size = new Size(width, baseSize.Height + notesHeight);
            MinimumSize = Size;
            MaximumSize = Size;

            KeepOnScreen();
        }

        // The quiet frame of a field; with the keyboard focus in the box the stronger one (as every text box of the library).
        private void SetNotesFocused(bool focused)
        {
            _notesFocused = focused;
            UpdateNotesFrame();
        }

        private void UpdateNotesFrame()
        {
            // (The page is the one behind the frame: the box stands on the frame itself, whose color is what is being worked out.)
            _notesFrame.BackColor = _notesFocused
                ? InputField.GetFrameColor(_notesBox, null, true)
                : UIColors.GetOutlineColor(InputField.GetPageColor(_notesFrame));
        }

        // The scroll bar is there only when the text does not fit: it takes room from the text, and a bar that is always there has nothing to
        // scroll. (Changing ScrollBars makes a new window, so it is done only when it must change; with the bar the lines are wrapped narrower, so
        // a text that does not fit with it does not fit without it, and one that fits with it fits without.)
        private void UpdateNotesScrollBars()
        {
            if (_notesBox == null || !_notesBox.IsHandleCreated || !_notesExpanded)
                return;

            _mainLayout.PerformLayout();

            int lines = _notesBox.GetLineFromCharIndex(_notesBox.TextLength) + 1;
            bool doesNotFit = lines * _notesBox.Font.Height > _notesBox.ClientSize.Height;
            ScrollBars wanted = doesNotFit ? ScrollBars.Vertical : ScrollBars.None;

            if (_notesBox.ScrollBars != wanted)
                _notesBox.ScrollBars = wanted;
        }

        /// <inheritdoc/>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateNotesScrollBars();
        }

        private int GetNotesHeight()
        {
            if (_size.Custom.HasValue)
                return 110;

            return _size.Preset == FormSize.Small ? 80 : _size.Preset == FormSize.Large ? 130 : 100;
        }

        // A window that has become higher (the notes were opened) does not reach below the screen.
        private void KeepOnScreen()
        {
            if (!IsHandleCreated || WindowState != FormWindowState.Normal)
                return;

            Rectangle area = Screen.FromControl(this).WorkingArea;

            if (Bottom > area.Bottom)
                Top = Math.Max(area.Top, area.Bottom - Height);
        }

        // The panel and the top-down layout both of the window's contents are built from.
        private static Panel CreateContentPanel(out FlowLayoutPanel layout)
        {
            Panel panel = UIPanelFactory.CreateMedium();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(24, 18, 24, 8);

            layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            return panel;
        }

        private Control CreatePromptContent()
        {
            FlowLayoutPanel layout;
            Panel panel = CreateContentPanel(out layout);

            Label headline = UILabelFactory.CreateStandard(UIStrings.Get("Update.AvailableMessage"));
            _headline = headline;
            headline.AutoSize = true;
            headline.Font = UIFonts.Title;
            ThemeWatcher.FollowForeColor(headline, () => UIColors.TextPrimary);
            headline.Margin = new Padding(0, 0, 0, 10);

            Label versionLine = UILabelFactory.CreateStandard(
                "v" + _currentVersion + "  →  v" + _latestVersion);
            versionLine.AutoSize = true;
            versionLine.Font = UIFonts.Normal;
            ThemeWatcher.FollowForeColor(versionLine, () => UIColors.TextSecondary);

            layout.Controls.Add(headline);
            layout.Controls.Add(versionLine);
            panel.Controls.Add(layout);

            return panel;
        }

        private Control CreateButtonPanel()
        {
            TableLayoutPanel buttonPanel = UITableLayoutPanelFactory.CreateStandard(3, 1);
            buttonPanel.Dock = DockStyle.Fill;
            ThemeWatcher.FollowBackColor(buttonPanel, () => UIColors.BackgroundMedium);
            buttonPanel.Padding = new Padding(12, 12, 24, 18);
            buttonPanel.Margin = new Padding(0);

            // Button widths grow to fit whatever UIStrings resolves to for the
            // active language (e.g. German "Jetzt aktualisieren" is wider than
            // English "Update now") - the 80/100 minimums keep English pixel-
            // identical to before.
            string laterText = UIStrings.Get("Update.Later");
            string updateText = UIStrings.Get("Update.UpdateNow");
            int laterButtonWidth;
            int updateButtonWidth;
            MeasureColumns(out laterButtonWidth, out updateButtonWidth);
            _buttonTable = buttonPanel;

            buttonPanel.ColumnStyles.Clear();
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, laterButtonWidth));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, updateButtonWidth));

            buttonPanel.RowStyles.Clear();
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // At the left: the buttons that are there only when they are wanted (the release notes, Skip). Docked from the last one added: the
            // button of the notes stands at the very left.
            Panel left = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0) };

            _skipButton = UIButtonFactory.CreateStandard(UIStrings.Get("Update.Skip"), "", new Size(100, 30));
            _skipButton.Dock = DockStyle.Left;
            _skipButton.Margin = new Padding(0);
            _skipButton.Width = FitWidth(_skipButton);
            _skipButton.Visible = false;
            _skipButton.Click += OnSkipButtonClick;

            _notesButton = UIButtonFactory.CreateStandard("", "", new Size(100, 30));
            _notesButton.Dock = DockStyle.Left;
            _notesButton.Margin = new Padding(0, 0, 6, 0);
            _notesButton.Visible = false;
            _notesButton.Click += (sender, e) => NotesExpanded = !NotesExpanded;

            left.Controls.Add(_skipButton);
            left.Controls.Add(_notesButton);

            Button laterButton = UIButtonFactory.CreateStandard(
                laterText, "", new Size(laterButtonWidth, 30));
            laterButton.Dock = DockStyle.Fill;
            laterButton.Margin = new Padding(0, 0, 6, 0);
            laterButton.Click += (sender, e) => Close();
            CancelButton = laterButton;
            _laterButton = laterButton;

            Button updateButton = UIButtonFactory.CreateGreen(
                updateText, "", new Size(updateButtonWidth, 30));
            updateButton.Dock = DockStyle.Fill;
            updateButton.Margin = new Padding(6, 0, 0, 0);
            updateButton.Click += OnUpdateButtonClick;
            AcceptButton = updateButton;
            _updateButton = updateButton;

            buttonPanel.Controls.Add(left, 0, 0);
            buttonPanel.Controls.Add(laterButton, 1, 0);
            buttonPanel.Controls.Add(updateButton, 2, 0);

            return buttonPanel;
        }

        // The widths of the two columns of the right: as wide as the texts of the language need (the minimums are what English always had).
        private void MeasureColumns(out int laterWidth, out int updateWidth)
        {
            laterWidth = Math.Max(80, TextRenderer.MeasureText(UIStrings.Get("Update.Later"), UIFonts.Normal).Width + 50);
            updateWidth = Math.Max(100, TextRenderer.MeasureText(UIStrings.Get("Update.UpdateNow"), UIFonts.Normal).Width + 50);
            _columnsWidth = laterWidth + updateWidth;
        }

        // The texts follow a change of the language while the window is open: the title (when it is the standard one), the headline and the
        // buttons. The columns are measured again (a word of the other language may be longer) and the window is laid out anew.
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            if (_defaultTitle)
                FormTitle = UIStrings.Get("Update.AvailableTitle");

            _headline.Text = UIStrings.Get("Update.AvailableMessage");
            _laterButton.Text = UIStrings.Get("Update.Later");
            _updateButton.Text = UIStrings.Get("Update.UpdateNow");
            _skipButton.Text = UIStrings.Get("Update.Skip");
            _skipButton.Width = FitWidth(_skipButton);

            int laterWidth;
            int updateWidth;
            MeasureColumns(out laterWidth, out updateWidth);
            _buttonTable.ColumnStyles[1].Width = laterWidth;
            _buttonTable.ColumnStyles[2].Width = updateWidth;

            UpdateNotesButton();
            LayoutContent();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                UIStrings.LanguageChanged -= OnLanguageChanged;

            base.Dispose(disposing);
        }

        // A button as wide as its text needs.
        private static int FitWidth(Button button)
        {
            return Math.Max(70, TextRenderer.MeasureText(button.Text, button.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width + 28);
        }

        private void UpdateNotesButton()
        {
            if (_notesButton == null)
                return;

            _notesButton.Text = UIStrings.Get("Update.WhatsNew") + (_notesExpanded ? " ▴" : " ▾");
            _notesButton.Width = FitWidth(_notesButton) + 6;
        }

        private void OnUpdateButtonClick(object sender, EventArgs e)
        {
            // The dialog ends; what comes next (the download) is shown by a ProgressForm.
            UpdateChosen = true;
            DialogResult = DialogResult.Yes;
            Close();
        }

        private void OnSkipButtonClick(object sender, EventArgs e)
        {
            SkipChosen = true;
            DialogResult = DialogResult.Ignore;
            Close();
        }
    }
}