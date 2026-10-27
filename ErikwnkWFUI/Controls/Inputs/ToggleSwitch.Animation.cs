using System;
using System.Drawing;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // The knob glides from one end to the other when the user switches (mouse, space bar, screen reader): on the beat of the display,
    // as the other things that move. Setting Checked from code, a control that is not shown and a Windows with the animations
    // switched off stand it at the other end at once.
    public partial class ToggleSwitch
    {
        // How long the knob takes from one end to the other.
        private const double GlideMilliseconds = 150;

        private FrameTicker _ticker;
        private ControlAnimationListener _settings;   // the animations of Windows were switched: a glide in progress ends
        private double _position;                      // 0 = off (knob left) ... 1 = on (knob right); between while it glides
        private bool _glideNext;                       // the Checked that is being set is the user's

        private void InitializeAnimation()
        {
            _ticker = new FrameTicker(this, Step);
            _settings = new ControlAnimationListener(this, OnAnimationSettingChanged);
        }

        private void DisposeAnimation()
        {
            AnimationSettings.Remove(_settings);
            _ticker.Dispose();
        }

        // What the user does: the knob glides.
        private void ToggleByUser()
        {
            _glideNext = true;

            try
            {
                Checked = !Checked;
            }
            finally
            {
                _glideNext = false;
            }
        }

        // The state has just changed (any way): glide for the user's change, stand at the end at once for the others.
        private void OnCheckedChangedAnimation()
        {
            if (_glideNext && IsHandleCreated && Visible && AnimationSettings.Enabled())
                _ticker.Start();
            else
                Settle();
        }

        // The knob at the end that belongs to the state, no glide.
        private void Settle()
        {
            _ticker.Stop();
            _position = _checked ? 1.0 : 0.0;
            Invalidate();
        }

        // On each beat: the knob moves by the time that has passed.
        private void Step(double milliseconds)
        {
            double target = _checked ? 1.0 : 0.0;
            double step = milliseconds / GlideMilliseconds;

            _position = target > _position ? Math.Min(target, _position + step) : Math.Max(target, _position - step);

            if (_position == target)
                _ticker.Stop();

            Invalidate();
        }

        private void OnAnimationSettingChanged()
        {
            if (!AnimationSettings.Enabled())
                Settle();
        }

        // The color between the one of the knob at the left end (off) and the one at the right end (on).
        private Color Mix(Color off, Color on)
        {
            if (_position <= 0)
                return off;

            if (_position >= 1)
                return on;

            return UIColors.BlendToward(off, on, _position);
        }
    }
}
