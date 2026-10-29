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
    /// <summary>The dialog behind <see cref="UpdatePrompt.ShowUpdateAvailableAsync"/> - prefer calling that over constructing this directly: it shows this dialog and then, when the user chose to update, the progress of the download. After <c>ShowDialog</c> <see cref="UpdateChosen"/> says whether "Update now" was clicked.</summary>
    public class UpdateAvailableForm : AppForm
    {
        private static readonly Size DialogSize = new Size(420, 190);

        private readonly string _currentVersion;
        private readonly string _latestVersion;

        private Control _promptContent;
        private Control _buttonPanel;

        /// <summary>Whether the user clicked "Update now" (the dialog is closed with <c>DialogResult.Yes</c> then); false for Later and for closing it.</summary>
        public bool UpdateChosen { get; private set; }

        public UpdateAvailableForm(
            string currentVersion,
            string latestVersion,
            string title = null)
            : base(FormOptions.CreateDialog(
                title: title ?? UIStrings.Get("Update.AvailableTitle"),
                titleTextAlign: ContentAlignment.MiddleLeft,
                icon: SystemIcons.Information.ToBitmap()))
        {
            _currentVersion = currentVersion ?? "";
            _latestVersion = latestVersion ?? "";

            ConfigureForm();
            BuildLayout();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }

        private void ConfigureForm()
        {
            ShowInTaskbar = false;

            Size = DialogSize;
            MinimumSize = DialogSize;
            MaximumSize = DialogSize;
        }

        private void BuildLayout()
        {
            Panel rootPanel = UIStyles.Panels.CreateMedium();

            TableLayoutPanel mainLayout = UITableLayoutPanelFactory.CreateStandard(1, 2);
            mainLayout.Dock = DockStyle.Fill;
            ThemeWatcher.FollowBackColor(mainLayout, () => UIColors.BackgroundMedium);
            mainLayout.Padding = new Padding(0);
            mainLayout.Margin = new Padding(0);
            mainLayout.RowStyles.Clear();
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            Panel contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            _promptContent = CreatePromptContent();
            contentHost.Controls.Add(_promptContent);

            _buttonPanel = CreateButtonPanel();

            mainLayout.Controls.Add(contentHost, 0, 0);
            mainLayout.Controls.Add(_buttonPanel, 0, 1);

            rootPanel.Controls.Add(mainLayout);

            ContentPanel.Controls.Clear();
            ContentPanel.Controls.Add(rootPanel);
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
            int laterButtonWidth = Math.Max(80, TextRenderer.MeasureText(laterText, UIFonts.Normal).Width + 50);
            int updateButtonWidth = Math.Max(100, TextRenderer.MeasureText(updateText, UIFonts.Normal).Width + 50);

            buttonPanel.ColumnStyles.Clear();
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, laterButtonWidth));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, updateButtonWidth));

            buttonPanel.RowStyles.Clear();
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Button laterButton = UIButtonFactory.CreateStandard(
                laterText, "", new Size(laterButtonWidth, 30));
            laterButton.Dock = DockStyle.Fill;
            laterButton.Margin = new Padding(0, 0, 6, 0);
            laterButton.Click += (sender, e) => Close();
            CancelButton = laterButton;

            Button updateButton = UIButtonFactory.CreateGreen(
                updateText, "", new Size(updateButtonWidth, 30));
            updateButton.Dock = DockStyle.Fill;
            updateButton.Margin = new Padding(6, 0, 0, 0);
            updateButton.Click += OnUpdateButtonClick;
            AcceptButton = updateButton;

            buttonPanel.Controls.Add(laterButton, 1, 0);
            buttonPanel.Controls.Add(updateButton, 2, 0);

            return buttonPanel;
        }

        private void OnUpdateButtonClick(object sender, EventArgs e)
        {
            // The dialog ends; what comes next (the download) is shown by a ProgressForm.
            UpdateChosen = true;
            DialogResult = DialogResult.Yes;
            Close();
        }
    }
}
