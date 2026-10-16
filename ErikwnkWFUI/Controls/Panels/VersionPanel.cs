using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ErikwnkCore.Updater;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A narrow strip with the version of the program at its right end and - while an update is known - a small green button
    /// next to it that leads to the update. It is the version strip of a settings window as an element of its own, made by
    /// <c>UIStyles.VersionPanels</c>: it can be put in any window or page, not only in <c>StyledForm</c>.
    /// </summary>
    /// <remarks>
    /// The version is the one of the program (the entry assembly, like "v1.2.3"; <see cref="VersionText"/> changes it). The button is not
    /// there until <see cref="ShowUpdate"/> or <see cref="CheckForUpdateAsync"/> says that there is an update; a click on it
    /// shows the update prompt of the <see cref="AppUpdater"/> over the window the strip is in.
    /// </remarks>
    public class VersionPanel : Panel
    {
        private const int Height96 = 24;
        private const int RightInset96 = 10;
        private const int Gap96 = 6;
        private const int ButtonWidth96 = 22;
        private const int ButtonHeight96 = 20;
        private const double TextBlend = 0.45;
        private const double DisabledFade = 0.5;

        private string _versionText = EntryAssemblyVersion.GetText() ?? string.Empty;
        private Button _updateButton;

        public VersionPanel()
        {
            Height = Dpi.Scale(this, Height96);
            Padding = new Padding(0);
            Margin = new Padding(0);
        }

        /// <summary>The text at the right end; the version of the program ("v1.2.3") until it is set, an empty text shows none.</summary>
        public string VersionText
        {
            get { return _versionText; }
            set
            {
                _versionText = value ?? string.Empty;
                PerformLayout();
                Invalidate();
            }
        }

        /// <summary>Whether the update button is there (an update was announced with <see cref="ShowUpdate"/> or found by <see cref="CheckForUpdateAsync"/>).</summary>
        public bool UpdateAvailable
        {
            get { return _updateButton != null; }
        }

        /// <summary>
        /// Shows the update button for an update that is known: a click on it asks the user, downloads and installs the new version
        /// (see <see cref="AppUpdater.ShowUpdatePromptAsync"/>). A button that was there is replaced.
        /// </summary>
        /// <param name="updater">The updater of the program.</param>
        /// <param name="result">The newer version, from <see cref="AppUpdater.CheckForUpdateAsync(Version, TimeSpan)"/>.</param>
        /// <param name="currentVersion">The version that is running; the one of the program when left out.</param>
        public void ShowUpdate(AppUpdater updater, UpdateCheckResult result, Version currentVersion = null)
        {
            if (updater == null)
                throw new ArgumentNullException(nameof(updater));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            HideUpdate();

            // The window is asked for at the click: the strip may not be in one yet when it is made.
            Button button = updater.CreateUpdateAvailableButton(result, currentVersion ?? EntryAssemblyVersion.Get() ?? new Version(1, 0, 0), () => FindForm());
            button.Size = new Size(Dpi.Scale(this, ButtonWidth96), Dpi.Scale(this, ButtonHeight96));
            button.Margin = new Padding(0);
            _updateButton = button;
            Controls.Add(button);
            PerformLayout();
        }

        /// <summary>Takes the update button away (and disposes it).</summary>
        public void HideUpdate()
        {
            if (_updateButton == null)
                return;

            Button button = _updateButton;
            _updateButton = null;
            Controls.Remove(button);
            button.Dispose();
        }

        /// <summary>
        /// Asks GitHub whether there is a newer version and shows the update button if there is one. Silent: it never shows a message and
        /// never throws (no net, a slow answer or no newer version just leave the strip as it is).
        /// </summary>
        /// <param name="updater">The updater of the program.</param>
        /// <param name="timeout">How long the check may take.</param>
        /// <param name="currentVersion">The version that is running; the one of the program when left out.</param>
        /// <returns>Whether an update was found.</returns>
        public async Task<bool> CheckForUpdateAsync(AppUpdater updater, TimeSpan timeout, Version currentVersion = null)
        {
            if (updater == null)
                throw new ArgumentNullException(nameof(updater));

            Version version = currentVersion ?? EntryAssemblyVersion.Get() ?? new Version(1, 0, 0);
            UpdateCheckResult result = await updater.CheckForUpdateAsync(version, timeout);

            if (result == null || IsDisposed)
                return false;

            ShowUpdate(updater, result, version);

            return true;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            if (_updateButton == null)
                return;

            int inset = Dpi.Scale(this, RightInset96);
            int gap = Dpi.Scale(this, Gap96);
            int textLeft = ClientSize.Width - inset;

            if (_versionText.Length > 0)
                textLeft -= MeasureText().Width + gap;

            _updateButton.Location = new Point(textLeft - _updateButton.Width, (ClientSize.Height - _updateButton.Height) / 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_versionText.Length == 0)
                return;

            Color surface = SurfaceColor.Of(this);
            Color text = UIColors.GetMutedText(surface, TextBlend);

            // A disabled strip is quieter still (than the muted version, not than the full text).
            if (!Enabled)
                text = UIColors.BlendToward(text, surface, DisabledFade);

            Size size = MeasureText();
            Rectangle area = new Rectangle(ClientSize.Width - Dpi.Scale(this, RightInset96) - size.Width, 0, size.Width, ClientSize.Height);

            TextRenderer.DrawText(e.Graphics, _versionText, UIFonts.Small, area, text, TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            PerformLayout();
            Invalidate();
        }

        private Size MeasureText()
        {
            return TextRenderer.MeasureText(_versionText, UIFonts.Small, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
    }
}
