using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using Button = System.Windows.Forms.Button;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A ready-made volume control for audio: a mute button (a loudspeaker, crossed out while it is muted) with a <see cref="TrackBar"/>
    /// beside it, as on video sites. The slider shows the percentage in a popup while it is used (or, with <see cref="ShowPercentage"/>, as
    /// a number beside it). It works as the controls of YouTube and Twitch: a click on the button (or the key M on the slider) mutes and,
    /// the next time, brings back the volume that was before; moving the slider to 0 mutes, moving it away from 0 turns the sound on again;
    /// the mouse wheel over the button turns the volume like the wheel over the slider; while it is muted the slider stands at 0 and the
    /// volume is remembered.
    /// <see cref="Volume"/> is the level the sound has when it is on, <see cref="IsMuted"/> whether it is off, and <see cref="EffectiveVolume"/>
    /// what the audio output is to get (0 while muted). <see cref="VolumeChanged"/> comes when the user changes what is heard, not when code
    /// sets <see cref="Volume"/> or <see cref="IsMuted"/> (as the <see cref="TrackBar.ValueChanged"/> of the slider). Make one with
    /// <c>UIStyles.VolumeControls</c>. The mute button and the slider are reachable for what is not offered here (<see cref="MuteButton"/>,
    /// <see cref="Slider"/>: the colors of the popup, the steps of the keys ...); right-to-left layouts are not supported. It makes no sound
    /// itself: a player that implements <see cref="IAudioVolume"/> (of ErikwnkCore) is coupled with <see cref="Target"/> - the control
    /// sets it, and follows it when it changes by itself.
    /// </summary>
    public class VolumeControl : Panel
    {
        private const int Width96 = 150;
        private const int Height96 = 30;
        private const int SliderHeight96 = 20;
        private const int Gap96 = 4;

        private readonly TrackBar _slider;
        private Button _button;

        // The size the button was made for (the symbol grows with it, so another size makes a new button).
        private int _buttonHeight;

        // The level for when the sound is on; it stays while the control is muted.
        private double _volume = 1.0;

        // The level when a slide began: a slide down to 0 mutes, and what the button brings back is this, not the sliver the thumb passed last.
        private double _volumeBeforeDrag = 1.0;
        private bool _muted;
        private IAudioVolume _target;

        // True while the control sets the player: the event the player raises for that is not an answer to follow.
        private bool _pushing;
        private bool _showPercentage;

        // The number beside the slider; made when ShowPercentage is first switched on.
        private Label _percent;

        public VolumeControl()
        {
            BackColor = Color.Transparent;
            Size = new Size(Dpi.Scale(this, Width96), Dpi.Scale(this, Height96));

            _slider = new TrackBar { Maximum = 1.0, Value = 1.0, ShowValuePopup = true, FormatValue = TrackBar.FormatPercent };
            _slider.DragStarted += (sender, e) => _volumeBeforeDrag = _volume;
            _slider.ValueChanged += OnSliderChanged;
            _slider.KeyDown += OnSliderKeyDown;
            Controls.Add(_slider);

            BuildButton(Size.Height);
            UIStrings.LanguageChanged += OnLanguageChanged;
            UpdateParts();
        }

        /// <summary>Raised when the user changes what is heard: moves the slider or switches the sound off or on (see <see cref="EffectiveVolume"/>). Not when code sets the volume.</summary>
        public event EventHandler VolumeChanged;

        /// <summary>
        /// The level the sound has when it is on, from 0 to 1 (1 by default); it is remembered while the control is muted. Setting it
        /// from code moves the slider (while it is not muted) without raising <see cref="VolumeChanged"/>; 0 mutes the control
        /// and leaves the remembered level as it was, a value above 0 does not turn the sound on.
        /// </summary>
        [DefaultValue(1.0)]
        public double Volume
        {
            get { return _volume; }
            set
            {
                if (double.IsNaN(value))
                    return;

                if (value <= 0)
                    _muted = true;
                else
                    _volume = Math.Min(1.0, value);

                UpdateParts();
                PushToTarget();
            }
        }

        /// <summary>Whether the sound is off. The slider stands at 0 while it is, and the <see cref="Volume"/> is remembered.</summary>
        [DefaultValue(false)]
        public bool IsMuted
        {
            get { return _muted; }
            set
            {
                if (value == _muted)
                    return;

                _muted = value;
                UpdateParts();
                PushToTarget();
            }
        }

        /// <summary>What the audio output is to get: 0 while the control is muted, else the <see cref="Volume"/>.</summary>
        [Browsable(false)]
        public double EffectiveVolume
        {
            get { return _muted ? 0.0 : _volume; }
        }

        /// <summary>
        /// Whether the volume is written as a percentage ("60 %") beside the slider, as in players like VLC (off by default). While it
        /// is on, the popup of the slider is off, so that the number is not shown twice; the text is 0 % while the control is muted.
        /// </summary>
        [DefaultValue(false)]
        public bool ShowPercentage
        {
            get { return _showPercentage; }
            set
            {
                if (value == _showPercentage)
                    return;

                _showPercentage = value;
                _slider.ShowValuePopup = !value;

                if (value && _percent == null)
                {
                    _percent = UIStyles.Labels.CreateStandard();
                    _percent.AutoSize = false;
                    _percent.TextAlign = ContentAlignment.MiddleRight;
                    _percent.TabStop = false;
                    Controls.Add(_percent);
                }

                if (_percent != null)
                    _percent.Visible = value;

                UpdateParts();
                PerformLayout();
            }
        }

        /// <summary>
        /// The colors of the slider and its popup (a color set, or simply a color; the mute button has the neutral look). Null (the
        /// default) is gray; <c>UIStyles.VolumeControls.CreatePrimary</c> makes one that follows the accent. Not written by the designer.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIColorSet Colors
        {
            get { return _slider.Colors; }
            set { _slider.Colors = value; }
        }

        internal void FollowColors(Func<UIColorSet> source)
        {
            _slider.FollowColors(source);
        }

        /// <summary>
        /// The player (or anything else that makes sound) this control is coupled to; null (the default) is none. On assigning, the control
        /// takes over its volume and mute; from then on every change by the user - and every <see cref="Volume"/> or <see cref="IsMuted"/> set
        /// from code - is passed on to it (the volume that is remembered and the mute as two things), and the control follows the
        /// <see cref="IAudioVolume.VolumeChanged"/> of the player (also when it is raised on another thread). The control lets go of it when it is
        /// disposed.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IAudioVolume Target
        {
            get { return _target; }
            set
            {
                if (ReferenceEquals(value, _target))
                    return;

                if (_target != null)
                    _target.VolumeChanged -= OnTargetChanged;

                _target = value;

                if (_target == null)
                    return;

                _target.VolumeChanged += OnTargetChanged;
                SyncFromTarget();
            }
        }

        // The player changed by itself (maybe on the thread of the sound): the control follows, on its own thread.
        private void OnTargetChanged(object sender, EventArgs e)
        {
            if (_pushing || IsDisposed)
                return;

            if (IsHandleCreated && InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(SyncFromTarget));
                }
                catch (InvalidOperationException)
                {
                    // the window has gone since: nothing is shown any more
                }

                return;
            }

            SyncFromTarget();
        }

        private void SyncFromTarget()
        {
            IAudioVolume target = _target;

            if (target == null || IsDisposed)
                return;

            double volume = target.Volume;

            if (!double.IsNaN(volume) && volume > 0)
                _volume = Math.Min(1.0, volume);

            // A volume of 0 is as good as muted (the remembered level stays), as when it is set from code.
            _muted = target.IsMuted || double.IsNaN(volume) || volume <= 0;
            UpdateParts();
        }

        // The control sets the player: the volume that is remembered, and whether it is muted.
        private void PushToTarget()
        {
            IAudioVolume target = _target;

            if (target == null)
                return;

            _pushing = true;

            try
            {
                target.Volume = _volume;
                target.IsMuted = _muted;
            }
            finally
            {
                _pushing = false;
            }
        }

        /// <summary>The slider, for what the control does not offer by itself (the popup, the steps of the keys, its own events).</summary>
        [Browsable(false)]
        public TrackBar Slider
        {
            get { return _slider; }
        }

        /// <summary>The mute button.</summary>
        [Browsable(false)]
        public Button MuteButton
        {
            get { return _button; }
        }

        // The mute button: a symbol button the size of the height, in the neutral look. It takes no focus (the focus stays on the slider,
        // where the key M does the same); it is made again when the control gets another height, because the symbol grows with the button.
        private void BuildButton(int height)
        {
            Button old = _button;
            int size = Math.Max(1, Math.Min(height, Math.Max(1, ClientSize.Width / 2)));

            _button = UIButtonFactory.CreateSymbol(GetSymbol(), string.Empty, new Size(size, size), (UIColorSet)null);
            _buttonHeight = size;
            ((SymbolButton)_button).TakeNoFocus();
            _button.Click += OnButtonClick;
            _button.MouseWheel += OnWheelOverPart;
            Controls.Add(_button);

            if (old != null)
            {
                Controls.Remove(old);
                old.Dispose();
            }

            // The tooltip and the name belong to the new button as well.
            UpdateParts();
        }

        private string GetSymbol()
        {
            return EffectiveVolume <= 0 ? UIStyles.Symbols.Mute : UIStyles.Symbols.Volume;
        }

        // The slider, the symbol and the tooltip show what the state is.
        private void UpdateParts()
        {
            _slider.Value = EffectiveVolume;

            string symbol = GetSymbol();

            if (_button != null && _button.Text != symbol)
                _button.Text = symbol;

            string text = UIStrings.Get(_muted ? "VolumeControl.Unmute" : "VolumeControl.Mute");

            if (_button != null)
            {
                UIButtonFactory.UpdateTooltip(_button, text);
                _button.AccessibleName = text;
            }

            _slider.AccessibleName = UIStrings.Get("VolumeControl.Volume");

            if (_percent != null)
                _percent.Text = TrackBar.FormatPercent(EffectiveVolume);
        }

        // The wheel over the button (or the gap beside it) turns the volume like the wheel over the slider.
        private void OnWheelOverPart(object sender, MouseEventArgs e)
        {
            HandledMouseEventArgs handled = e as HandledMouseEventArgs;

            if (handled != null)
                handled.Handled = true;

            _slider.StepByWheel(e.Delta);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            OnWheelOverPart(this, e);
        }

        private void OnSliderChanged(object sender, EventArgs e)
        {
            double value = _slider.Value;

            if (value <= 0)
            {
                _muted = true;

                // Slid down to 0: what is brought back is the volume from before the slide, not the sliver of it the thumb passed last.
                if (_slider.IsDragging)
                    _volume = _volumeBeforeDrag;
            }
            else
            {
                _volume = value;
                _muted = false;
            }

            UpdateParts();
            PushToTarget();
            RaiseVolumeChanged();
        }

        private void OnButtonClick(object sender, EventArgs e)
        {
            ToggleMute();
        }

        private void OnSliderKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.M || e.Control || e.Alt || e.Shift)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            ToggleMute();
        }

        private void ToggleMute()
        {
            if (!Enabled)
                return;

            _muted = !_muted;
            UpdateParts();
            PushToTarget();
            RaiseVolumeChanged();
        }

        private void RaiseVolumeChanged()
        {
            if (VolumeChanged != null)
            {
                VolumeChanged(this, EventArgs.Empty);
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            UpdateParts();

            // (The number is written with the decimal sign and the space of the language, so the room for it may change.)
            if (_showPercentage)
                PerformLayout();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            // (While the control is being made, the button is not there yet.)
            if (_button == null)
                return;

            int height = Math.Max(1, ClientSize.Height);
            int buttonSize = Math.Max(1, Math.Min(height, Math.Max(1, ClientSize.Width / 2)));

            if (buttonSize != _buttonHeight)
                BuildButton(buttonSize);

            int gap = Dpi.Scale(this, Gap96);
            int sliderHeight = Math.Min(height, Dpi.Scale(this, SliderHeight96));
            int left = buttonSize + gap;

            // The room for the percentage: the widest text it can have (100 %), at the right edge.
            int percentWidth = 0;

            if (_showPercentage && _percent != null)
            {
                percentWidth = TextRenderer.MeasureText(TrackBar.FormatPercent(1.0), _percent.Font, new Size(short.MaxValue, short.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width + gap;
                _percent.SetBounds(Math.Max(left, ClientSize.Width - percentWidth), 0, Math.Min(percentWidth, Math.Max(1, ClientSize.Width - left)), height);
            }

            _button.SetBounds(0, (height - buttonSize) / 2, buttonSize, buttonSize);
            _slider.SetBounds(left, (height - sliderHeight) / 2, Math.Max(1, ClientSize.Width - left - percentWidth), sliderHeight);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIStrings.LanguageChanged -= OnLanguageChanged;

                if (_target != null)
                {
                    _target.VolumeChanged -= OnTargetChanged;
                    _target = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
