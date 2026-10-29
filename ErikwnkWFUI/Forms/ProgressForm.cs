using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using Button = System.Windows.Forms.Button;
using Label = System.Windows.Forms.Label;
using ProgressBar = ErikwnkWFUI.Controls.ProgressBar;

namespace ErikwnkWFUI.Forms
{
    /// <summary>Where a <see cref="ProgressForm"/> is: still working, or finished with <see cref="ProgressForm.Succeed"/> or <see cref="ProgressForm.Fail"/>.</summary>
    public enum ProgressState
    {
        /// <summary>The task is running.</summary>
        Running,

        /// <summary>The task is done (<see cref="ProgressForm.Succeed"/>): the bar is full and green.</summary>
        Succeeded,

        /// <summary>The task failed (<see cref="ProgressForm.Fail"/>): the bar stands still and is red.</summary>
        Failed
    }

    /// <summary>
    /// A small dialog that shows how far a task has come: a text (<see cref="Status"/>) and a progress bar with the percentage on it, or a
    /// bar that just moves (<see cref="IsIndeterminate"/>) when the end cannot be told. The task reports with <see cref="Report(int)"/> - from any
    /// thread - and the window is also an <see cref="IProgress{T}"/>, so it can be handed to a task that wants one. The user cannot close it (the
    /// close button is greyed out): the program closes it when the task is done, or ends it with <see cref="Succeed"/> or <see cref="Fail"/> (see
    /// below). With <see cref="Cancelable"/> there is a Cancel button (Escape
    /// does the same) that asks the task to stop through <see cref="CancellationToken"/>; the window stays until the program closes it.
    /// <see cref="State"/> says where it is, and once the task has ended nothing changes any more (late reports are ignored). <see cref="ShowTime"/> adds the time that has passed and the time that is left (an estimate). <see cref="Succeed"/> and
    /// <see cref="Fail"/> end the task in the window itself: the bar turns green or red, the text says how it ended, and the user closes the
    /// window with the button (now "Close") - so a message can still be read. It is for a task that blocks the program for a while; the
    /// reports and the end can come from any thread. Make one with
    /// <c>UIStyles.Forms.CreateProgressStandard</c> / <c>CreateProgressPrimary</c>, or let <see cref="ProgressBox.Run"/> do it all.
    /// </summary>
    public class ProgressForm : AppForm, IProgress<int>
    {
        // The sizes are given for 96 dpi and scaled to the screen.
        private const int Margin96 = 24;
        private const int GapBeforeBar96 = 14;
        private const int BarHeight96 = 24;
        private const int GapBeforeTime96 = 8;
        private const int GapBeforeButton96 = 20;
        private const int ButtonWidth96 = 100;
        private const int ButtonHeight96 = 32;
        private const int ScreenMargin96 = 40;

        // Not an estimate before this much has passed and the task has begun: the first seconds say nothing about the rest.
        private static readonly TimeSpan LeastTimeForEstimate = TimeSpan.FromSeconds(2);

        private readonly Label _statusLabel;
        private readonly Label _timeLabel;
        private readonly ProgressBar _bar;
        private readonly Button _cancelButton;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer { Interval = 500 };
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly FormSizeOption _size;
        private readonly bool _defaultTitle;
        private readonly object _lock = new object();
        private int _dpiOverride;
        private int _applyQueued;
        private bool _layoutWanted;
        private int _percent;
        private string _status;
        private bool _indeterminate;
        private bool _showPercentage = true;
        private bool _showTime;
        private bool _cancelable;
        private ProgressState _state;
        private string _cancelText;
        private readonly UIColorSet _windowColors;
        private UIColorSet _barColors;
        private Color _barColorApplied;

        // The time that has passed; the seam of the tests (null: the clock of the window).
        internal Func<TimeSpan> Clock;

        // How often the state was put on the controls; the tests see that a flood of reports is put on them in a few runs.
        internal int AppliedCount;

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

            // The title of the library ("Please wait") follows the language; a title that was given stays.
            _defaultTitle = options == null || options.Title == UIStrings.Get("ProgressForm.Title");

            ShowInTaskbar = false;

            // The user cannot close it: the task ends it (or Succeed / Fail, and then the button). The program can: Close() works.
            CloseEnabled = false;

            _statusLabel = UIStyles.Labels.CreateStandard(_status);
            _statusLabel.AutoEllipsis = false;
            _statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _statusLabel.TextAlign = ContentAlignment.TopLeft;

            // The bar is in the accent, or in the color set of the window when it has one.
            UIColorSet colors = options == null ? null : options.Colors;
            _windowColors = colors;
            _bar = (ProgressBar)(colors == null ? UIStyles.ProgressBars.CreatePrimary() : UIStyles.ProgressBars.CreateStandard(colors));
            _bar.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _bar.ShowPercentage = true;

            _timeLabel = UIStyles.Labels.CreateMuted();
            _timeLabel.AutoEllipsis = false;
            _timeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _timeLabel.TextAlign = ContentAlignment.TopLeft;

            _cancelButton = UIStyles.Buttons.CreateStandard(UIStrings.Get("ProgressForm.Cancel"));
            _cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _cancelButton.Click += (sender, e) => OnButtonClick();
            _cancelButton.Visible = false;

            ContentPanel.Controls.AddRange(new Control[] { _statusLabel, _bar, _timeLabel, _cancelButton });

            _timer.Tick += (sender, e) => UpdateTime();

            Cancelable = cancelable;
            ApplyState();
            UIStrings.LanguageChanged += OnLanguageChanged;
            ErikwnkWFUI.Controls.ThemeWatcher.Attach(this, OnThemeChanged);
        }

        /// <summary>
        /// The colors of the bar while the task runs (its main color, as <c>UIStyles.ProgressBars.CreateStandard(colors)</c> uses it): for example
        /// <c>UIStyles.Colors.Sets.Green</c> for an update, as green as its Update now button. Null (the default) is the color set of the window
        /// when it has one, else the accent. Once the task has ended the bar is green (<see cref="Succeed"/>) or red (<see cref="Fail"/>) whatever this is.
        /// Set it on the thread of the window.
        /// </summary>
        public UIColorSet BarColors
        {
            get { return _barColors; }
            set
            {
                _barColors = value;
                ApplyBarColors();
            }
        }

        private void ApplyBarColors()
        {
            if (State != ProgressState.Running)
                return;

            _barColorApplied = UIProgressBarFactory.FillOf(() => _barColors ?? _windowColors ?? UIColors.PrimarySet)();
            _bar.ForeColor = _barColorApplied;
        }

        // The fill follows the theme (and the accent) as long as it is the one that was worked out here.
        private void OnThemeChanged()
        {
            if (_barColors != null && _bar.ForeColor == _barColorApplied)
                ApplyBarColors();
        }

        /// <summary>The text above the bar ("Downloading ..."); the window grows or shrinks to it. Can be set from any thread.</summary>
        public string Status
        {
            get { lock (_lock) return _status; }
            set
            {
                lock (_lock)
                {
                    if (_state != ProgressState.Running)
                        return;

                    _status = value ?? "";
                }

                PushState(layout: true);
            }
        }

        /// <summary>
        /// How far the task has come, 0 to 100 (a value outside is cut to it); setting it turns <see cref="IsIndeterminate"/> off. Can be set from
        /// any thread. Once the task has ended (<see cref="Succeed"/>, <see cref="Fail"/>) nothing changes any more: late reports are ignored.
        /// </summary>
        public int Percent
        {
            get { lock (_lock) return _percent; }
            set
            {
                lock (_lock)
                {
                    if (_state != ProgressState.Running)
                        return;

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
                {
                    if (_state != ProgressState.Running)
                        return;

                    _indeterminate = value;
                }

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
        /// Whether a gray line under the bar says how much time has passed and - when the percentage is known and the task has been running for
        /// a moment - how much is left, as an estimate ("0:12 elapsed · about 0:35 left"). Off by default.
        /// </summary>
        public bool ShowTime
        {
            get { return _showTime; }
            set
            {
                _showTime = value;
                UpdateTimerState();
                PushState(layout: true);
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
                UpdateButton();
                LayoutContent();
            }
        }

        /// <summary>
        /// The text of the Cancel button ("Cancel" or "Abbrechen"), or null for the text of the library. When the task has ended
        /// (<see cref="Succeed"/>, <see cref="Fail"/>) the button says "Close" whatever this is.
        /// </summary>
        public string CancelButtonText
        {
            get { return _cancelText ?? UIStrings.Get("ProgressForm.Cancel"); }
            set
            {
                _cancelText = value;
                UpdateButton();
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

        /// <summary>Whether the task is still running or has ended with <see cref="Succeed"/> or <see cref="Fail"/>.</summary>
        public ProgressState State
        {
            get { lock (_lock) return _state; }
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
                if (_state != ProgressState.Running)
                    return;

                _percent = Math.Max(0, Math.Min(100, percent));
                _indeterminate = false;
                _status = status ?? "";
            }

            PushState(layout: true);
        }

        /// <summary>
        /// Ends the task in the window: the bar is full and green, the text (when there is one) says how it went, and the time stops. The
        /// button now says "Close" and closes the window - the user can close it from now on (the close button, Escape). From any thread.
        /// </summary>
        /// <param name="text">What the window says now; null keeps the text it has.</param>
        public void Succeed(string text = null)
        {
            Finish(ProgressState.Succeeded, text, 100);
        }

        /// <summary>
        /// Ends the task in the window as a failure: the bar stands where it was and is red, the text says what went wrong (so that it can still
        /// be read), and the time stops. The button says "Close"; the user can close the window from now on. From any thread.
        /// </summary>
        /// <param name="text">What went wrong; null keeps the text the window has.</param>
        public void Fail(string text = null)
        {
            Finish(ProgressState.Failed, text, null);
        }

        private void Finish(ProgressState state, string text, int? percent)
        {
            lock (_lock)
            {
                // The end is final: a task that reports late, or fails after it has succeeded, does not change what the window says.
                if (_state != ProgressState.Running)
                    return;

                _state = state;
                _indeterminate = false;

                if (percent.HasValue)
                    _percent = percent.Value;

                if (text != null)
                    _status = text;
            }

            PushState(layout: true);
        }

        // The button cancels while the task runs, and closes the window once it has ended.
        private void OnButtonClick()
        {
            if (State != ProgressState.Running)
            {
                Close();

                return;
            }

            if (_cancellation.IsCancellationRequested)
                return;

            _cancelButton.Enabled = false;
            _cancellation.Cancel();

            EventHandler handler = CancelRequested;

            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        // The state is kept in fields and put on the controls on the thread of the window: a task may report before the window has a handle,
        // from any thread, even when the window has been closed already. Reports from another thread are put on the controls in one run
        // for all that came in the meantime - a download that reports thousands of times a second does not flood the message queue.
        private void PushState(bool layout)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            try
            {
                if (!InvokeRequired)
                {
                    ApplyState(layout);

                    return;
                }

                lock (_lock)
                    _layoutWanted |= layout;

                if (Interlocked.Exchange(ref _applyQueued, 1) == 0)
                    BeginInvoke(new Action(ApplyQueued));
            }
            catch (ObjectDisposedException)
            {
                // The window is gone while the task still reports: nothing is shown any more.
            }
            catch (InvalidOperationException)
            {
                // The window has no handle (yet or any more): there is nothing to update.
            }
        }

        private void ApplyQueued()
        {
            // Open for the next report first: whatever comes after the state is read below gets a run of its own.
            Interlocked.Exchange(ref _applyQueued, 0);

            bool layout;

            lock (_lock)
            {
                layout = _layoutWanted;
                _layoutWanted = false;
            }

            ApplyState(layout);
        }

        private void ApplyState(bool layout = true)
        {
            if (IsDisposed)
                return;

            AppliedCount++;

            string status;
            int percent;
            bool indeterminate;
            ProgressState state;

            lock (_lock)
            {
                status = _status;
                percent = _percent;
                indeterminate = _indeterminate;
                state = _state;
            }

            _bar.Style = indeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;

            if (!indeterminate)
                _bar.Value = percent;

            _bar.ShowPercentage = _showPercentage && !indeterminate;

            // Green or red once it has ended; before that the color the bar was made with.
            if (state != ProgressState.Running)
            {
                UIColorSet set = state == ProgressState.Succeeded ? UIStyles.Colors.Sets.Green : UIStyles.Colors.Sets.Red;
                _bar.ForeColor = set.Basic;
            }

            if (_statusLabel.Text != status)
            {
                _statusLabel.Text = status;
                layout = true;
            }

            // The user may close the window once the task has ended.
            CloseEnabled = state != ProgressState.Running;
            UpdateButton();
            UpdateTimerState();
            UpdateTime();

            if (layout)
                LayoutContent();
        }

        // ---- the button

        // Visible while the window can be cancelled and once the task has ended (then it closes); its text says which.
        private bool ButtonShown
        {
            get { return _cancelable || State != ProgressState.Running; }
        }

        private void UpdateButton()
        {
            bool ended = State != ProgressState.Running;

            _cancelButton.Visible = ButtonShown;
            CancelButton = ButtonShown ? _cancelButton : null;

            // Assigning CancelButton gives the button DialogResult.Cancel, and a click would close the window at once, while the task still runs:
            // the window stays until the task has ended (it closes itself then); Cancel only asks the task to stop.
            _cancelButton.DialogResult = DialogResult.None;
            _cancelButton.Text = ended ? UIStrings.Get("ProgressForm.Close") : CancelButtonText;

            // Cancelling asked once: the button is dead until the task has ended and it closes the window.
            _cancelButton.Enabled = ended || !_cancellation.IsCancellationRequested;
        }

        // ---- the time

        private TimeSpan GetElapsed()
        {
            return Clock != null ? Clock() : _stopwatch.Elapsed;
        }

        // The clock of the window runs from the moment the window is shown until the task ends, whether the time is shown or not (it can be
        // switched on later and still tells the truth); the timer that refreshes the line runs only while the line is wanted.
        private void UpdateTimerState()
        {
            if (_stopwatch.IsRunning && State != ProgressState.Running)
                _stopwatch.Stop();

            _timer.Enabled = _showTime && IsHandleCreated && State == ProgressState.Running;
        }

        private void UpdateTime()
        {
            if (IsDisposed)
                return;

            string text = _showTime ? FormatTime(GetElapsed(), Percent, IsIndeterminate, State) : "";

            if (_timeLabel.Text != text)
            {
                bool layoutNeeded = (text.Length == 0) != (_timeLabel.Text.Length == 0);
                _timeLabel.Text = text;

                if (layoutNeeded)
                    LayoutContent();
            }
        }

        /// <summary>The line of the time: what has passed, and - when the percentage is known and the task has run for a moment - about what is left.</summary>
        internal static string FormatTime(TimeSpan elapsed, int percent, bool indeterminate, ProgressState state)
        {
            string passed = FormatDuration(elapsed);

            if (state != ProgressState.Running || indeterminate || percent <= 0 || percent >= 100 || elapsed < LeastTimeForEstimate)
                return string.Format(UIStrings.Get("ProgressForm.Elapsed"), passed);

            TimeSpan left = TimeSpan.FromTicks((long)(elapsed.Ticks * (100 - percent) / (double)percent));

            return string.Format(UIStrings.Get("ProgressForm.ElapsedLeft"), passed, FormatDuration(left));
        }

        // m:ss, and h:mm:ss from an hour on.
        internal static string FormatDuration(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
                duration = TimeSpan.Zero;

            int hours = (int)duration.TotalHours;

            return hours > 0
                ? string.Format("{0}:{1:00}:{2:00}", hours, duration.Minutes, duration.Seconds)
                : string.Format("{0}:{1:00}", duration.Minutes, duration.Seconds);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (!_stopwatch.IsRunning && State == ProgressState.Running)
                _stopwatch.Start();

            ApplyState();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            UpdateButton();
            UpdateTime();

            if (_defaultTitle)
                FormTitle = UIStrings.Get("ProgressForm.Title");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIStrings.LanguageChanged -= OnLanguageChanged;
                _timer.Dispose();
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

        // The text, the bar, the time and (when it is there) the button one below the other; the height of the window follows the (wrapped) text.
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

            bool timeShown = _showTime && _timeLabel.Text.Length > 0;
            int timeHeight = timeShown
                ? TextRenderer.MeasureText(_timeLabel.Text, _timeLabel.Font, new Size(inner, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height
                : 0;

            int textHeight = _statusLabel.Text.Length == 0
                ? 0
                : TextRenderer.MeasureText(_statusLabel.Text, _statusLabel.Font, new Size(inner, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;

            // Never higher than the screen: a text that is longer than what fits is cut at its end (the bar and the button stay).
            int below = Scale(BarHeight96) + margin
                + (timeShown ? Scale(GapBeforeTime96) + timeHeight : 0)
                + (ButtonShown ? Scale(GapBeforeButton96) + Scale(ButtonHeight96) : 0);
            int outside = ClientSize.Height - ContentPanel.ClientSize.Height;
            int room = Screen.FromControl(this).WorkingArea.Height - Scale(ScreenMargin96) - outside - margin - below - Scale(GapBeforeBar96);
            textHeight = Math.Max(0, Math.Min(textHeight, room));

            int y = margin;
            _statusLabel.Visible = textHeight > 0;
            _statusLabel.SetBounds(margin, y, inner, textHeight);
            y += textHeight + (textHeight > 0 ? Scale(GapBeforeBar96) : 0);

            _bar.SetBounds(margin, y, inner, Scale(BarHeight96));
            y += Scale(BarHeight96);

            _timeLabel.Visible = timeShown;

            if (timeShown)
            {
                y += Scale(GapBeforeTime96);
                _timeLabel.SetBounds(margin, y, inner, timeHeight);
                y += timeHeight;
            }

            if (ButtonShown)
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
