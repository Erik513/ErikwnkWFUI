using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// A settings window built around a <see cref="PropertyTable"/>: add rows to <see cref="PropertyTable"/> (and use <see cref="AppForm.VersionStrip"/> for
    /// an update button), then call <see cref="FitToContent"/> so the window sizes itself to what was added instead of guessing a size. It is a
    /// settings window of <see cref="UIStyles.Forms"/> (the version of the program in a strip at the bottom) with the buttons Save and
    /// Cancel, which close it with <c>DialogResult.OK</c> and <c>Cancel</c>; <see cref="ShowButtons"/> = false leaves them out for a window that
    /// applies every change at once. Make one with <c>UIStyles.Forms.CreateOptionsStandard</c> or <c>CreateOptionsPrimary</c>.
    /// </summary>
    public class OptionsForm : AppForm
    {
        private readonly PropertyTable _propertyTable;
        private readonly Panel _buttonBar;
        private readonly Button _saveButton;
        private readonly Button _cancelButton;
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
        /// closes it with the close button of the title bar or Escape, and there is no Enter and Escape handling of buttons.
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
        /// the dialog closes with DialogResult.OK - read whatever the option controls added to PropertyTable are currently set to here.
        /// </summary>
        public event EventHandler SaveClicked;

        /// <summary>Add rows to <see cref="PropertyTable"/> and call <see cref="FitToContent"/> after construction. The title is "Options" or "Optionen" when none is given.</summary>
        public OptionsForm(string title = null)
            : this(FormOptions.CreateSettings(title ?? UIStrings.Get("OptionsForm.Title")))
        {
        }

        /// <summary>The same with the options of the window (a color set or the accent for the title bar, a logo ...); start from <see cref="FormOptions.CreateSettings"/> or <see cref="FormOptions.CreateSettingsPrimary"/>.</summary>
        public OptionsForm(FormOptions options)
            : base(options ?? FormOptions.CreateSettings(UIStrings.Get("OptionsForm.Title")))
        {
            MinimumSize = new Size(340, 160);

            // Free space between the form's edges and the property
            // table/button bar - previously they were docked flush against
            // ContentPanel, so the table's own (darker) background filled
            // the whole window edge-to-edge instead of reading as a card.
            ContentPanel.Padding = new Padding(20);

            _propertyTable = UIStyles.PropertyTables.CreateStandard();
            _propertyTable.Dock = DockStyle.Top;
            // The table's default background matches ContentPanel's
            // BackgroundMedium, so its (transparent) label column blended
            // straight into the form - darker tone makes it read as its
            // own card.
            ThemeWatcher.FollowBackColor(_propertyTable, () => UIStyles.Colors.BackgroundDark);

            // Same BackgroundMedium as ContentPanel - so the form reads as
            // one consistent color, with only the property table set apart.
            _buttonBar = UIStyles.Panels.CreateMedium();
            _buttonBar.Dock = DockStyle.Bottom;
            _buttonBar.Height = 56;

            _cancelButton = UIStyles.Buttons.CreateStandard(UIStrings.Get("OptionsForm.Cancel"), size: new Size(100, 32));
            _cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _cancelButton.Location = new Point(_buttonBar.Width - 220, 12);
            _cancelButton.DialogResult = DialogResult.Cancel;
            _cancelButton.Click += (s, e) => Close();

            _saveButton = UIStyles.Buttons.CreatePrimary(UIStrings.Get("OptionsForm.Save"), size: new Size(100, 32));
            _saveButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _saveButton.Location = new Point(_buttonBar.Width - 112, 12);
            _saveButton.Click += (s, e) =>
            {
                // As the OK button of any dialog: the editors are validated first (their Validating events), and a value that is not
                // accepted keeps the window open.
                if (!ValidateChildren())
                    return;

                SaveClicked?.Invoke(this, EventArgs.Empty);
                DialogResult = DialogResult.OK;
                Close();
            };

            _buttonBar.Controls.Add(_cancelButton);
            _buttonBar.Controls.Add(_saveButton);

            // Fill (property table) added before the bottom-docked bar, so
            // the bar reliably keeps its band - see BuildCard in
            // DealOrNoDeal.cs for the same Dock-order reasoning.
            ContentPanel.Controls.Add(_propertyTable);
            ContentPanel.Controls.Add(_buttonBar);

            // Each button's own DialogResult only fires on an actual click -
            // it does nothing for the Enter/Escape keys on its own. Wiring
            // the form's AcceptButton/CancelButton is what makes Enter (in
            // any child control that doesn't itself consume it, e.g. a
            // single-line TextBox) submit and Escape cancel, matching every
            // other ErikwnkWFUI dialog convention.
            AcceptButton = _saveButton;
            CancelButton = _cancelButton;

            Resize += (s, e) => RepositionButtons();
            RepositionButtons();

            UIStrings.LanguageChanged += OnLanguageChanged;
        }

        // The buttons that have no text of their own follow the language.
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_saveText == null)
                _saveButton.Text = UIStrings.Get("OptionsForm.Save");

            if (_cancelText == null)
                _cancelButton.Text = UIStrings.Get("OptionsForm.Cancel");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                UIStrings.LanguageChanged -= OnLanguageChanged;

            base.Dispose(disposing);
        }

        private void RepositionButtons()
        {
            _cancelButton.Location = new Point(_buttonBar.Width - 220, 12);
            _saveButton.Location = new Point(_buttonBar.Width - 112, 12);
        }

        /// <summary>
        /// Sizes the window to exactly fit the rows currently in
        /// PropertyTable plus the button bar and the version strip - call once after populating
        /// PropertyTable, instead of guessing a fixed window size.
        /// </summary>
        public void FitToContent(int extraWidth = 16, int extraHeight = 16)
        {
            Size preferred = _propertyTable.PreferredSize;

            int width = Math.Max(MinimumSize.Width,
                preferred.Width + extraWidth + Padding.Horizontal + ContentPanel.Padding.Horizontal);
            int height = TitleBar.Height + preferred.Height + (_showButtons ? _buttonBar.Height : 0) + (VersionStrip != null ? VersionStrip.Height : 0)
                + extraHeight + Padding.Vertical + ContentPanel.Padding.Vertical;

            ClientSize = new Size(width, Math.Max(MinimumSize.Height, height));
        }
    }
}
