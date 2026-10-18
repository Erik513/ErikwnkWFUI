using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using Button = System.Windows.Forms.Button;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A ready-made volume control for audio: a mute button (a loudspeaker, crossed out while it is muted) with a <see cref="TrackBar"/>
    /// beside it, as on video sites. The slider shows the percentage in a popup while it is used. It works as the controls of YouTube and Twitch:
    /// a click on the button (or the key M on the slider) mutes and, the next time, brings back the volume that was before; moving the slider
    /// to 0 mutes, moving it away from 0 turns the sound on again; while it is muted the slider stands at 0 and the volume is remembered.
    /// <see cref="Volume"/> is the level the sound has when it is on, <see cref="IsMuted"/> whether it is off, and <see cref="EffectiveVolume"/>
    /// what the audio output is to get (0 while muted). <see cref="VolumeChanged"/> comes when the user changes what is heard, not when code
    /// sets <see cref="Volume"/> or <see cref="IsMuted"/> (as the <see cref="TrackBar.ValueChanged"/> of the slider). Make one with
    /// <c>UIStyles.VolumeControls</c>. The mute button and the slider are reachable for what is not offered here (<see cref="MuteButton"/>,
    /// <see cref="Slider"/>: the colors of the popup, the steps of the keys ...); right-to-left layouts are not supported.
    /// </summary>
    public class VolumeControl : Panel
    {
        private const int Width96 = 150;
        private const int Height96 = 30;
        private const int SliderHeight96 = 20;
        private const int Gap96 = 4;

        private readonly TrackBar _slider;
        private Button _button;
        private int _buttonHeight;
        private double _volume = 1.0;
        private double _volumeBeforeDrag = 1.0;
        private bool _muted;

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
            }
        }

        /// <summary>What the audio output is to get: 0 while the control is muted, else the <see cref="Volume"/>.</summary>
        [Browsable(false)]
        public double EffectiveVolume
        {
            get { return _muted ? 0.0 : _volume; }
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

            _button.SetBounds(0, (height - buttonSize) / 2, buttonSize, buttonSize);
            _slider.SetBounds(left, (height - sliderHeight) / 2, Math.Max(1, ClientSize.Width - left), sliderHeight);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                UIStrings.LanguageChanged -= OnLanguageChanged;

            base.Dispose(disposing);
        }
    }
}
