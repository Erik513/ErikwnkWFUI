using System;
using System.Threading;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // The steps of the movement on the beat of LabelAnimationClock, the messages of the window, and what the label does when it is told something (text, size, visibility, handle, dispose).
    public sealed partial class ThemedLabel : IAnimationSettingsListener
    {

        // ---- the beat of the clock and the steps ----

        private void StopAnimating()
        {
            _animating = false;
            LabelAnimationClock.Remove(this);
        }

        private void StopWatching()
        {
            if (!_watching)
                return;

            _watching = false;
            AnimationSettings.Remove(this);
        }

        // Called from the thread of the system events (AnimationSettings): tell the window that a setting of Windows has changed.
        void IAnimationSettingsListener.NotifySettingChanged()
        {
            IntPtr handle = _postHandle;

            if (handle != IntPtr.Zero)
                PostMessage(handle, WM_SETTING_CHANGED, IntPtr.Zero, IntPtr.Zero);
        }

        // Called from the thread of the clock: ask the window for the next picture, once.
        internal void RequestFrame()
        {
            IntPtr handle = _postHandle;

            if (handle == IntPtr.Zero || Interlocked.Exchange(ref _framePending, 1) == 1)
                return;

            if (!PostMessage(handle, WM_NEXT_PICTURE, IntPtr.Zero, IntPtr.Zero))
                Interlocked.Exchange(ref _framePending, 0);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NEXT_PICTURE)
            {
                MakeFrame();

                return;
            }

            // The user may have switched the animations of Windows on or off: the text moves, or stops, at once.
            if (m.Msg == WM_SETTING_CHANGED)
            {
                Restart();

                return;
            }

            base.WndProc(ref m);
        }

        private void MakeFrame()
        {
            Interlocked.Exchange(ref _framePending, 0);

            if (!_animating || IsDisposed)
                return;

            // A window that is hidden does not tell its labels (they get no VisibleChanged): the clock
            // finds out here, and the label is on it again when it is shown (VisibleChanged then comes).
            if (!Visible)
            {
                StopAnimating();

                return;
            }

            if (Step() && IsHandleCreated)
            {
                // At once, not when the queue has nothing better to do: the picture belongs to this beat.
                Update();
            }
        }

        // One beat without the painting: what the tests call to let the clock beat when they want to look at
        // the movement only.
        private void OnFrame(object sender, EventArgs e)
        {
            Step();
        }

        // One step of the movement, back and forth: it counts the time that has passed since the last step,
        // so the speed does not depend on how often the clock manages to beat. Whether the text has moved
        // (then it is invalidated and a picture is due).
        private bool Step()
        {
            long now = Milliseconds();

            // While the mouse is on the label the text stands still, and what is left of its rest stays: the end of
            // the rest moves on as much as the time does (one that was over stays over).
            if (_scrollPauseOnHover && _hovered)
            {
                _restUntil += now - _lastFrame;
                _lastFrame = now;

                return false;
            }

            if (_cycle > 0)
                return StepEndless(now);

            long from = Math.Max(_lastFrame, _restUntil);
            _lastFrame = now;

            if (now <= _restUntil)
                return false;

            _offset += _direction * _scrollSpeed.ToPixelsPerSecond() * (now - from) / 1000f;

            if (_direction > 0 && _offset >= _maxOffset)
            {
                _offset = _maxOffset;
                _direction = -1;
                _restUntil = now + _scrollPause.ToMilliseconds();
            }
            else if (_direction < 0 && _offset <= 0f)
            {
                _offset = 0f;
                _direction = 1;
                _restUntil = now + _scrollPause.ToMilliseconds();
            }

            // A picture only when the text has moved at all (not while it rests).
            if (_offset == _paintedOffset)
                return false;

            _paintedOffset = _offset;
            Invalidate();

            return true;
        }

        // The endless text goes on at its speed, round after round, without a rest.
        private bool StepEndless(long now)
        {
            long passed = now - _lastFrame;
            _lastFrame = now;

            _offset = (_offset + _scrollSpeed.ToPixelsPerSecond() * passed / 1000f) % _cycle;

            if (_offset == _paintedOffset)
                return false;

            _paintedOffset = _offset;
            Invalidate();

            return true;
        }

        // ---- what the label is told ----

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);

            // Another text than the joined one: the label moves that text alone from now on.
            if (_texts != null && Text != _textsJoined)
                _texts = null;

            Restart();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            Restart();
        }

        // A size is changed again and again while a window is resized: the text must not jump back to its
        // start each time. The measure is made again, the place in the movement stays (as far as it still exists).
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            float offset = _offset;
            int direction = _direction;
            long restUntil = _restUntil;
            bool wasMoving = _animating;

            Restart();

            if (wasMoving && _cycle > 0)
            {
                _offset = offset % _cycle;
            }
            else if (wasMoving && _maxOffset > 0)
            {
                _offset = Math.Min(offset, _maxOffset);
                _direction = direction;
                _restUntil = restUntil;
            }
        }

        protected override void OnPaddingChanged(EventArgs e)
        {
            base.OnPaddingChanged(e);
            Restart();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            // (A label that is disabled gets no MouseLeave: the mouse is not "on" it any more.)
            _hovered = false;
            Restart();
        }

        // A label hidden by itself, or a window shown again, comes here; a window that is hidden does not tell
        // its labels (see MakeFrame).
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            // (A label that is hidden gets no MouseLeave either.)
            _hovered = false;
            Restart();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _postHandle = Handle;
            Restart();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _postHandle = IntPtr.Zero;
            _hovered = false;

            if (_animating)
                StopAnimating();

            StopWatching();

            base.OnHandleDestroyed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_animating)
                    StopAnimating();

                StopWatching();
                DisposePicture();
                _fullTextToolTip.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
