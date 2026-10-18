using System;
using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // The popup with the current value above the thumb (ShowValuePopup): while the user drags it, then briefly after they let go,
    // click once, or change the value with a key or the wheel - and for as long as the mouse simply hovers over it,
    // disappearing the instant the mouse leaves. The mechanics are those of ValuePopup; this part says when it is wanted.
    public partial class TrackBar : IValuePopupHost
    {
        private ValuePopup _valuePopup;
        private UIColorSet _popupColors;

        /// <summary>
        /// Whether the value is shown in a small popup above the thumb: while the user drags it, then briefly after they let go, click
        /// once or use a key or the wheel, and for as long as the mouse hovers over the slider. Off by default.
        /// </summary>
        [DefaultValue(false)]
        public bool ShowValuePopup
        {
            get { return _valuePopup.Enabled; }
            set { _valuePopup.Enabled = value; }
        }

        /// <summary>
        /// The colors of the popup (its background and its text): a color set, or simply a color. Null (the default) gives it the colors
        /// of the slider - gray for <c>CreateStandard</c>, the accent for <c>CreatePrimary</c>, the set of <see cref="Colors"/> - and it
        /// follows them. Not written by the designer (a set is code).
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIColorSet ValuePopupColors
        {
            get { return _popupColors; }
            set
            {
                _popupColors = value;
                _valuePopup.ColorsChanged();
            }
        }

        private void InitializePopup()
        {
            _valuePopup = new ValuePopup(this);

            // ValueChanged alone would already cover dragging too (it fires
            // continuously on every mouse-move, restarting the hide timer
            // each time so it never gets a quiet moment of that length to actually fire) -
            // DragStarted is only needed for the click-without-moving edge
            // case, where SetValueFromUser's no-op-if-unchanged check means
            // ValueChanged never fires at all.
            DragStarted += delegate { _valuePopup.ShowUpdate(); };
            ValueChanged += delegate { _valuePopup.ShowAfterInteraction(); };

            // Forced (bypasses the throttle) - releasing a fast drag
            // right on 0% or 100% could otherwise land its very last
            // ValueChanged inside the throttle window, leaving the popup
            // stuck showing a slightly-stale value forever (the drag is
            // over, so nothing else would ever trigger another update).
            DragEnded += delegate { _valuePopup.ShowAfterInteraction(force: true); };

            // Same problem, same fix, for holding an arrow key: OS key-repeat
            // can outrun the throttle just like a fast drag can, so the last
            // repeat before release might get skipped, stranding the popup on
            // a slightly-stale value (e.g. 95% instead of 100%). KeyUp fires
            // even though IsInputKey claims these keys - it only changes
            // which keys generate KeyDown/KeyUp, not whether they do.
            KeyUp += delegate(object sender, KeyEventArgs e) { if (IsValueKey(e.KeyCode)) _valuePopup.ShowAfterInteraction(force: true); };
        }

        // The keys that move the value: only these show the popup (not every key that is let go while the slider has the focus: Shift,
        // Ctrl, the Tab that brought the focus here ...).
        private static bool IsValueKey(Keys key)
        {
            return key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down
                || key == Keys.PageUp || key == Keys.PageDown || key == Keys.Home || key == Keys.End;
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
            _valuePopup.LostFocus();
        }

        // What the popup asks the slider (explicit: not part of what the slider offers).
        Control IValuePopupHost.Owner
        {
            get { return this; }
        }

        double IValuePopupHost.Value
        {
            get { return Value; }
        }

        double IValuePopupHost.Minimum
        {
            get { return Minimum; }
        }

        double IValuePopupHost.Maximum
        {
            get { return Maximum; }
        }

        string IValuePopupHost.Format(double value)
        {
            return FormatValue(value);
        }

        int IValuePopupHost.AnchorX
        {
            get { return ThumbCenterX; }
        }

        // Its own colors if it has some, else the colors of the slider (gray, a set, the accent).
        UIColorSet IValuePopupHost.PopupColors
        {
            get { return _popupColors ?? Colors; }
        }
    }
}
