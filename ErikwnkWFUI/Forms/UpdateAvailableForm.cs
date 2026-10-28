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
    /// <summary>How an <see cref="UpdatePrompt.ShowUpdateAvailableAsync"/> dialog ended.</summary>
    public enum UpdateOutcome
    {
        /// <summary>Closed without clicking "Update now".</summary>
        Declined,
        /// <summary>applyUpdateAsync returned true.</summary>
        Applied,
        /// <summary>applyUpdateAsync returned false or threw.</summary>
        Failed
    }

    public static class UpdatePrompt
    {
        /// <summary>
        /// Shows a standardized "a new version is available" dialog. If the user clicks "Update now", the dialog closes and a
        /// <see cref="ProgressForm"/> takes its place while applyUpdateAsync runs (on another thread), reporting its progress into the bar; the
        /// user cannot close that window. Returns how it ended: <see cref="UpdateOutcome.Declined"/> (Later, or the dialog was closed),
        /// <see cref="UpdateOutcome.Applied"/> or <see cref="UpdateOutcome.Failed"/> (applyUpdateAsync returned false or threw).
        /// </summary>
        public static Task<UpdateOutcome> ShowUpdateAvailableAsync(
            string currentVersion,
            string latestVersion,
            Func<IProgress<int>, Task<bool>> applyUpdateAsync,
            Form owner = null,
            string title = null)
        {
            using (UpdateAvailableForm form = new UpdateAvailableForm(
                currentVersion,
                latestVersion,
                title ?? UIStrings.Get("UpdateAvailable.Title")))
            {
                if (owner != null)
                {
                    form.ShowDialog(owner);
                }
                else
                {
                    form.ShowDialog();
                }

                if (!form.UpdateChosen)
                {
                    return Task.FromResult(UpdateOutcome.Declined);
                }
            }

            bool applied = false;

            try
            {
                ProgressBox.Run(
                    title ?? UIStrings.Get("UpdateAvailable.Title"),
                    async (progress, token) => applied = await applyUpdateAsync(progress),
                    UIStrings.Get("UpdateAvailable.Downloading"),
                    cancelable: false,
                    owner: owner);
            }
            catch
            {
                applied = false;
            }

            return Task.FromResult(applied ? UpdateOutcome.Applied : UpdateOutcome.Failed);
        }
    }

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
                title: title ?? UIStrings.Get("UpdateAvailable.Title"),
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

            Label headline = UILabelFactory.CreateStandard(UIStrings.Get("UpdateAvailable.Message"));
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
            string laterText = UIStrings.Get("UpdateAvailable.Later");
            string updateText = UIStrings.Get("UpdateAvailable.UpdateNow");
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
