using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// A settings window built around a <see cref="PropertyTable"/>: add your own rows to <see cref="PropertyTable"/>, then call
    /// <see cref="FitToContent"/> so the window sizes itself to what was added instead of guessing a size. It is a settings window of
    /// <see cref="UIStyles.Forms"/> (the version of the program in a strip at the bottom, <see cref="AppForm.VersionStrip"/>) with the buttons
    /// Save and Cancel, which close it with <c>DialogResult.OK</c> and <c>Cancel</c>; <see cref="ShowButtons"/> = false leaves them out for a
    /// window that applies every change at once. The table is a card in the middle of the window, and it scrolls when the rows are more than the
    /// screen has room for (the buttons and the version strip stay).
    /// It starts with the ready-made options of the library - theme (a button in the top right corner), accent color, language and always on top (<see cref="OptionsPreset"/>; more
    /// are there - start with Windows, reset to defaults - and any selection is added with <see cref="AddOption"/>). They are applied as they are
    /// chosen and taken back when the window is closed without Save. Saving also writes the appearance when the program called
    /// <see cref="UIStyles.RememberAppearance"/>, and tells the user by a toast (<see cref="ShowToasts"/>). Make one with
    /// <c>UIStyles.Forms.CreateOptionsStandard</c> or <c>CreateOptionsPrimary</c>.
    /// </summary>
    public partial class OptionsForm : AppForm
    {
        // The card in the middle of the window: this wide (96 dpi), with the same room at both sides.
        private const int TableWidth96 = 520;
        private const int WindowSideMargin96 = 40;

        // What is left free of the work area of the screen when the window is as high as it can be.
        private const int ScreenMargin96 = 40;

        private static readonly OptionsPreset[] DefaultPresets =
        {
            OptionsPreset.Theme, OptionsPreset.AccentColor, OptionsPreset.Language, OptionsPreset.AlwaysOnTop
        };

        private readonly PropertyTable _propertyTable;
        private readonly Panel _tableHolder;
        private readonly Panel _scrollHost;
        private readonly Panel _buttonBar;
        private readonly Button _saveButton;
        private readonly Button _cancelButton;
        private int _dpiOverride;
        private bool _showButtons = true;
        private string _saveText;
        private string _cancelText;

        /// <summary>Add your option rows here, then call <see cref="FitToContent"/>.</summary>
        public PropertyTable PropertyTable
        {
            get { return _propertyTable; }
        }

        /// <summary>
        /// The text of the Save button: "Save" or "Speichern", by the language of the library (it follows a change of the language while the
        /// window is open). A text set here is shown as it is; null goes back to the language of the library.
        /// </summary>
        public string SaveButtonText
        {
            get { return _saveButton.Text; }
            set
            {
                _saveText = value;
                _saveButton.Text = value ?? UIStrings.Get("OptionsForm.Save");
            }
        }

        /// <summary>The text of the Cancel button, as <see cref="SaveButtonText"/>.</summary>
        public string CancelButtonText
        {
            get { return _cancelButton.Text; }
            set
            {
                _cancelText = value;
                _cancelButton.Text = value ?? UIStrings.Get("OptionsForm.Cancel");
            }
        }

        /// <summary>
        /// Whether the window has the buttons Save and Cancel (the default). Without them it is for settings that are applied at once: the user
        /// closes it with the close button of the title bar or Escape, and there is no Enter and Escape handling of buttons. The options of the
        /// library that were chosen stay then.
        /// </summary>
        public bool ShowButtons
        {
            get { return _showButtons; }
            set
            {
                _showButtons = value;
                _buttonBar.Visible = value;
                AcceptButton = value ? _saveButton : null;
                CancelButton = value ? _cancelButton : null;
            }
        }

        /// <summary>
        /// Raised when the save button is clicked and every editor has accepted its value (<c>Validating</c> did not cancel), before
        /// the dialog closes with DialogResult.OK - read whatever the option controls added to PropertyTable are currently set to here, and the
        /// options of the library from <see cref="SelectedTheme"/>, <see cref="SelectedAccentColor"/>, <see cref="SelectedLanguage"/> ...
        /// (the start with Windows is written before, the remembered appearance after this event).
        /// </summary>
        public event EventHandler SaveClicked;

        /// <summary>Add rows to <see cref="PropertyTable"/> and call <see cref="FitToContent"/> after construction. The title is "Options" or "Optionen" when none is given. The table starts with the default options of the library.</summary>
        public OptionsForm(string title = null)
            : this(FormOptions.CreateSettings(title ?? UIStrings.Get("OptionsForm.Title")), null)
        {
        }

        /// <summary>The same with the options of the window (a color set or the accent for the title bar, a logo ...); start from <see cref="FormOptions.CreateSettings"/> or <see cref="FormOptions.CreateSettingsPrimary"/>.</summary>
        public OptionsForm(FormOptions options)
            : this(options, null)
        {
        }

        /// <summary>
        /// The same with the options of the library to start with: exactly <paramref name="presets"/> (none for an empty array), null is the default
        /// selection (theme, accent color, language, always on top). <see cref="AddOption"/> adds more later.
        /// </summary>
        public OptionsForm(FormOptions options, OptionsPreset[] presets)
            : base(options ?? FormOptions.CreateSettings(UIStrings.Get("OptionsForm.Title")))
        {
            // The sizes are given for 96 dpi and set by ApplyScale.
            _propertyTable = UIStyles.PropertyTables.CreateStandard();
            _propertyTable.Dock = DockStyle.Top;
            // The table's default background matches ContentPanel's BackgroundMedium, so its (transparent) label column blended straight into
            // the form - a darker tone makes it read as its own card.
            ThemeWatcher.FollowBackColor(_propertyTable, () => UIStyles.Colors.BackgroundDark);

            // The card is in the middle of the window: the holder puts the same room at both sides of it.
            _tableHolder = new Panel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0) };
            ThemeWatcher.FollowBackColor(_tableHolder, () => UIStyles.Colors.BackgroundMedium);
            _tableHolder.Controls.Add(_propertyTable);

            // It sits in a host that scrolls when the rows are more than the window has room for.
            _scrollHost = UIStyles.Panels.CreateMedium();
            _scrollHost.Dock = DockStyle.Fill;
            _scrollHost.AutoScroll = true;
            _scrollHost.Controls.Add(_tableHolder);
            _scrollHost.ClientSizeChanged += (s, e) => CenterTable();

            // Same BackgroundMedium as ContentPanel - so the form reads as one consistent color, with only the property table set apart.
            _buttonBar = UIStyles.Panels.CreateMedium();
            _buttonBar.Dock = DockStyle.Bottom;

            _cancelButton = UIStyles.Buttons.CreateStandard(UIStrings.Get("OptionsForm.Cancel"));
            _cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _cancelButton.DialogResult = DialogResult.Cancel;
            _cancelButton.Click += (s, e) => Close();

            _saveButton = UIStyles.Buttons.CreatePrimary(UIStrings.Get("OptionsForm.Save"));
            _saveButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _saveButton.Click += (s, e) =>
            {
                // As the OK button of any dialog: the editors are validated first (their Validating events), and a value that is not
                // accepted keeps the window open.
                if (!ValidateChildren())
                    return;

                ApplyStartWithWindows();
                SaveClicked?.Invoke(this, EventArgs.Empty);
                UIStyles.SaveAppearance();
                ShowSavedToast();
                DialogResult = DialogResult.OK;
                Close();
            };

            _buttonBar.Controls.Add(_cancelButton);
            _buttonBar.Controls.Add(_saveButton);

            // Fill (the scrolling table) is added before the bottom-docked bar: the docking runs from the last control to the first, so the
            // bar takes its band first and the table gets what is left.
            ContentPanel.Controls.Add(_scrollHost);
            ContentPanel.Controls.Add(_buttonBar);

            // Each button's own DialogResult only fires on an actual click - it does nothing for the Enter/Escape keys on its own. Wiring
            // the form's AcceptButton/CancelButton is what makes Enter (in any child control that doesn't itself consume it, e.g. a
            // single-line TextBox) submit and Escape cancel, matching every other ErikwnkWFUI dialog convention.
            AcceptButton = _saveButton;
            CancelButton = _cancelButton;

            Resize += (s, e) => RepositionButtons();
            ApplyScale();

            RememberOriginalSettings();
            AddOptions(presets ?? DefaultPresets);

            UIStrings.LanguageChanged += OnLanguageChanged;
        }

        // The buttons that have no text of their own follow the language, and so do the options.
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_saveText == null)
                _saveButton.Text = UIStrings.Get("OptionsForm.Save");

            if (_cancelText == null)
                _cancelButton.Text = UIStrings.Get("OptionsForm.Cancel");

            RefreshOptionTexts();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIStrings.LanguageChanged -= OnLanguageChanged;
                UIColors.ThemeChanged -= OnThemeChanged;
            }

            base.Dispose(disposing);
        }

        // ---- sizes

        // The sizes are given for 96 dpi and scaled to the screen; the test seam pretends another one.
        internal int DpiOverride
        {
            get { return _dpiOverride; }
            set
            {
                _dpiOverride = value;
                ApplyScale();
            }
        }

        private int Scale(int value96)
        {
            return Dpi.Scale(this, value96, _dpiOverride);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            ApplyScale();
        }

        private void ApplyScale()
        {
            MinimumSize = new Size(Scale(340), Scale(160));
            ContentPanel.Padding = new Padding(Scale(20));
            _buttonBar.Height = Scale(56);

            if (_themeStrip != null)
            {
                _themeStrip.Height = Scale(40);
                _themeButton.Size = new Size(Scale(32), Scale(32));
                PositionThemeButton();
            }

            Size buttonSize = new Size(Scale(100), Scale(32));
            _cancelButton.Size = buttonSize;
            _saveButton.Size = buttonSize;

            RepositionButtons();
            CenterTable();
        }

        private void RepositionButtons()
        {
            _cancelButton.Location = new Point(_buttonBar.Width - Scale(220), Scale(12));
            _saveButton.Location = new Point(_buttonBar.Width - Scale(112), Scale(12));
        }

        // The same room left and right of the card, whatever the width of the window.
        private void CenterTable()
        {
            int margin = Math.Max(0, (_scrollHost.ClientSize.Width - Scale(TableWidth96)) / 2);

            if (_tableHolder.Padding.Left != margin)
                _tableHolder.Padding = new Padding(margin, 0, margin, 0);
        }

        /// <summary>
        /// Sizes the window to exactly fit the rows currently in <see cref="PropertyTable"/> plus the button bar and the version strip - at most
        /// as high as the screen allows (then the table scrolls) - call it after the rows are added, instead of guessing a fixed window size.
        /// <paramref name="extraWidth"/> and <paramref name="extraHeight"/> are room (for 96 dpi) added to the width and the height.
        /// </summary>
        public void FitToContent(int extraWidth = 16, int extraHeight = 16)
        {
            Size preferred = _propertyTable.PreferredSize;

            int chrome = TitleBar.Height + (_showButtons ? _buttonBar.Height : 0) + (_themeStrip != null ? _themeStrip.Height : 0) + (VersionStrip != null ? VersionStrip.Height : 0)
                + Scale(extraHeight) + Padding.Vertical + ContentPanel.Padding.Vertical;
            int height = preferred.Height + chrome;

            // More rows than the screen has room for: the window is as high as the screen allows, and the table scrolls (with room for its bar).
            int room = Screen.FromControl(this).WorkingArea.Height - Scale(ScreenMargin96);
            int width = Math.Max(Scale(TableWidth96), preferred.Width) + 2 * Scale(WindowSideMargin96) + Scale(extraWidth)
                + Padding.Horizontal + ContentPanel.Padding.Horizontal;

            if (height > room)
            {
                height = room;
                width += SystemInformation.VerticalScrollBarWidth;
            }

            ClientSize = new Size(Math.Max(MinimumSize.Width, width), Math.Max(MinimumSize.Height, height));
        }
    }
}
