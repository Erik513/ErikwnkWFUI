using System;
using System.ComponentModel;
using ErikwnkCore;

namespace ErikwnkWFUI.Controls
{
    // The coupling to a player (Target): the control sets it, and follows it when it changes by itself.
    public partial class VolumeControl
    {
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

                // A control that has been disposed does not take a player any more (nothing would let go of it again).
                _target = IsDisposed ? null : value;

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
    }
}
