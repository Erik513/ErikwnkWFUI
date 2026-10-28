using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using Button = System.Windows.Forms.Button;
using Label = System.Windows.Forms.Label;
using ProgressBar = ErikwnkWFUI.Controls.ProgressBar;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// A small dialog that shows how far a task has come: a text (<see cref="Status"/>) and a progress bar with the percentage on it, or a
    /// bar that just moves (<see cref="IsIndeterminate"/>) when the end cannot be told. The task reports with <see cref="Report(int)"/> - from any
    /// thread - and the window is also an <see cref="IProgress{T}"/>, so it can be handed to a task that wants one. The user cannot close it (the
    /// close button is greyed out): the program closes it when the task is done. With <see cref="Cancelable"/> there is a Cancel button (Escape
    /// does the same) that asks the task to stop through <see cref="CancellationToken"/>; the window stays until the program closes it.
    /// Make one with <c>UIStyles.Forms.CreateProgressStandard</c> / <c>CreateProgressPrimary</c>, or let <see cref="ProgressBox.Run"/> do it all.
    /// </summary>
    public class ProgressForm : AppForm, IProgress<int>
    {
        // The sizes are given for 96 dpi and scaled to the screen.
        private const int Margin96 = 24;
        private const int GapBeforeBar96 = 14;
        private const int BarHeight96 = 24;
        private const int GapBeforeButton96 = 20;
        private const int ButtonWidth96 = 100;
        private const int ButtonHeight96 = 32;
        private const int ScreenMargin96 = 40;

        private readonly Label _statusLabel;
        private readonly ProgressBar _bar;
        private readonly Button _cancelButton;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly FormSizeOption _size;
        private readonly object _lock = new object();
        private int _dpiOverride;
        private int _percent;
        private string _status;
        private bool _indeterminate;
        private bool _showPercentage = true;
        private bool _cancelable;
        private string _cancelText;

        /// <summary>Raised (on the thread of the window) when the user asks to stop: the Cancel button or Escape. <see cref="IsCancelled"/> is true then.</summary>
        public event EventHandler CancelRequested;

        /// <param name="options">The options of the window (a dialog, as <see cref="FormOptions.CreateDialog"/>).</param>
        /// <param name="status">The text above the bar.</param>
        /// <param name="cancelable">Whether the window has a Cancel button.</param>
        /// <param name="size">One of the three widths (<see cref="FormSize.Medium"/> by default) or an own <see cref="Size"/>: its width, and its height as the least height.</param>
        public ProgressForm(FormOptions options, string status = "", bool cancelable = false, FormSizeOption? size = null)
            : base(options ?? FormOptions.CreateDialog(UIStrings.Get("ProgressForm.Title"), ContentAlignment.MiddleLeft))
        {
            _size = size ?? FormSize.Medium;
            _status = status ?? "";

            ShowInTaskbar = false;

            // The user cannot close it: the task ends it. (The program can: Close() works.)
            CloseEnabled = false;

            _statusLabel = UIStyles.Labels.CreateStandard(_status);
            _statusLabel.AutoEllipsis = false;
            _statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _statusLabel.TextAlign = ContentAlignment.TopLeft;

            // The bar is in the accent, or in the color set of the window when it has one.
            UIColorSet colors = options == null ? null : options.Colors;
            _bar = (ProgressBar)(colors == null ? UIStyles.ProgressBars.CreatePrimary() : UIStyles.ProgressBars.CreateStandard(colors));
            _bar.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _bar.ShowPercentage = true;

            _cancelButton = UIStyles.Buttons.CreateStandard(UIStrings.Get("ProgressForm.Cancel"));
            _cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _cancelButton.Click += (sender, e) => RequestCancel();
            _cancelButton.Visible = false;

            ContentPanel.Controls.AddRange(new Control[] { _statusLabel, _bar, _cancelButton });

            Cancelable = cancelable;
            ApplyState();
            UIStrings.LanguageChanged += OnLanguageChanged;
        }

        /// <summary>The text above the bar ("Downloading ..."); the window grows or shrinks to it. Can be set from any thread.</summary>
        public string Status
        {
            get { lock (_lock) return _status; }
            set
            {
                lock (_lock)
                    _status = value ?? "";

                PushState(layout: true);
            }
        }

        /// <summary>How far the task has come, 0 to 100 (a value outside is cut to it); setting it turns <see cref="IsIndeterminate"/> off. Can be set from any thread.</summary>
        public int Percent
        {
            get { lock (_lock) return _percent; }
            set
            {
                lock (_lock)
                {
                    _percent = Math.Max(0, Math.Min(100, value));
                    _indeterminate = false;
                }

                PushState(layout: false);
            }
        }

        /// <summary>Whether the bar just moves, because nobody can tell how long the task takes (the percentage is not shown then).</summary>
        public bool IsIndeterminate
        {
            get { lock (_lock) return _indeterminate; }
            set
            {
                lock (_lock)
                    _indeterminate = value;

                PushState(layout: false);
            }
        }

        /// <summary>Whether the percentage is written on the bar (the default); a bar that just moves has none.</summary>
        public bool ShowPercentage
        {
            get { return _showPercentage; }
            set
            {
                _showPercentage = value;
                PushState(layout: false);
            }
        }

        /// <summary>
        /// Whether the window has a Cancel button, which Escape presses too. Cancelling only asks: <see cref="IsCancelled"/> and
        /// <see cref="CancellationToken"/> say so, and the task stops and the program closes the window.
        /// </summary>
        public bool Cancelable
        {
            get { return _cancelable; }
            set
            {
                _cancelable = value;
                _cancelButton.Visible = value;
                CancelButton = value ? _cancelButton : null;
                LayoutContent();
            }
        }

        /// <summary>The text of the Cancel button ("Cancel" or "Abbrechen"), or null for the text of the library.</summary>
        public string CancelButtonText
        {
            get { return _cancelButton.Text; }
            set
            {
                _cancelText = value;
                _cancelButton.Text = value ?? UIStrings.Get("ProgressForm.Cancel");
            }
        }

        /// <summary>Whether the user has asked to stop.</summary>
        public bool IsCancelled
        {
            get { return _cancellation.IsCancellationRequested; }
        }

        /// <summary>Is cancelled when the user asks to stop: give it to the task, which looks at it (or passes it on) and ends.</summary>
        public CancellationToken CancellationToken
        {
            get { return _cancellation.Token; }
        }

        /// <summary>Reports how far the task has come (<see cref="Percent"/>). From any thread; also what <see cref="IProgress{T}"/> asks for.</summary>
        public void Report(int percent)
        {
            Percent = percent;
        }

        /// <summary>Reports how far the task has come and what it is doing now (<see cref="Percent"/> and <see cref="Status"/>). From any thread.</summary>
        public void Report(int percent, string status)
        {
            lock (_lock)
            {
                _percent = Math.Max(0, Math.Min(100, percent));
                _indeterminate = false;
                _status = status ?? "";
            }

            PushState(layout: true);
        }

        private void RequestCancel()
        {
            if (_cancellation.IsCancellationRequested)
                return;

            _cancelButton.Enabled = false;
            _cancellation.Cancel();

            EventHandler handler = CancelRequested;

            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        // The state is kept in fields and put on the controls on the thread of the window: a task may report before the window has a handle,
        // from any thread, even when the window has been closed already.
        private void PushState(bool layout)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                if (InvokeRequired)
                    BeginInvoke(new Action(() => ApplyState(layout)));
                else
                    ApplyState(layout);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void ApplyState(bool layout = true)
        {
            if (IsDisposed)
                return;

            string status;
            int percent;
            bool indeterminate;

            lock (_lock)
            {
                status = _status;
                percent = _percent;
                indeterminate = _indeterminate;
            }

            _bar.Style = indeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;

            if (!indeterminate)
                _bar.Value = percent;

            _bar.ShowPercentage = _showPercentage && !indeterminate;

            if (_statusLabel.Text != status)
            {
                _statusLabel.Text = status;
                layout = true;
            }

            if (layout)
                LayoutContent();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyState();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            if (_cancelText == null)
                _cancelButton.Text = UIStrings.Get("ProgressForm.Cancel");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIStrings.LanguageChanged -= OnLanguageChanged;
                _cancellation.Dispose();
            }

            base.Dispose(disposing);
        }

        // ---- layout

        internal int DpiOverride
        {
            get { return _dpiOverride; }
            set
            {
                _dpiOverride = value;
                LayoutContent();
            }
        }

        private int Scale(int value96)
        {
            return Dpi.Scale(this, value96, _dpiOverride);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            LayoutContent();
        }

        // The text, the bar and (when it is there) the Cancel button one below the other; the height of the window follows the (wrapped) text.
        private void LayoutContent()
        {
            if (_cancelButton == null)
                return;

            int margin = Scale(Margin96);
            Size wanted = _size.Resolve(FormKind.Progress);
            int width = Scale(wanted.Width);

            // The window has an edge around it: the content is as wide as the client area minus that (measured, not assumed).
            ClientSize = new Size(width, ClientSize.Height);
            int inner = ContentPanel.ClientSize.Width - 2 * margin;

            int textHeight = _statusLabel.Text.Length == 0
                ? 0
                : TextRenderer.MeasureText(_statusLabel.Text, _statusLabel.Font, new Size(inner, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;

            // Never higher than the screen: a text that is longer than what fits is cut at its end (the bar and the button stay).
            int below = Scale(BarHeight96) + margin + (_cancelable ? Scale(GapBeforeButton96) + Scale(ButtonHeight96) : 0);
            int outside = ClientSize.Height - ContentPanel.ClientSize.Height;
            int room = Screen.FromControl(this).WorkingArea.Height - Scale(ScreenMargin96) - outside - margin - below - Scale(GapBeforeBar96);
            textHeight = Math.Max(0, Math.Min(textHeight, room));

            int y = margin;
            _statusLabel.Visible = textHeight > 0;
            _statusLabel.SetBounds(margin, y, inner, textHeight);
            y += textHeight + (textHeight > 0 ? Scale(GapBeforeBar96) : 0);

            _bar.SetBounds(margin, y, inner, Scale(BarHeight96));
            y += Scale(BarHeight96);

            if (_cancelable)
            {
                y += Scale(GapBeforeButton96);
                Size button = new Size(Scale(ButtonWidth96), Scale(ButtonHeight96));
                _cancelButton.SetBounds(ContentPanel.ClientSize.Width - margin - button.Width, y, button.Width, button.Height);
                y += button.Height;
            }

            y += margin;

            // An own height that is more than the content needs: the content stays at the top.
            int leastHeight = Scale(wanted.Height) - outside;
            y = Math.Max(y, leastHeight);

            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            ClientSize = new Size(width, outside + y);
            MinimumSize = Size;
            MaximumSize = Size;
        }
    }
}
