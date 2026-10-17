using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // What a screen reader is told about the slider.
    public partial class TrackBar
    {
        protected override AccessibleObject CreateAccessibilityInstance()
        {
            return new TrackBarAccessibleObject(this);
        }

        // A screen reader asks for the role (a slider) and the value, written as the popup writes it.
        private sealed class TrackBarAccessibleObject : ControlAccessibleObject
        {
            private readonly TrackBar _bar;

            public TrackBarAccessibleObject(TrackBar bar)
                : base(bar)
            {
                _bar = bar;
            }

            public override AccessibleRole Role
            {
                get { return AccessibleRole.Slider; }
            }

            public override string Value
            {
                get { return _bar.FormatValue(_bar._value); }
                set { }
            }
        }
    }
}
