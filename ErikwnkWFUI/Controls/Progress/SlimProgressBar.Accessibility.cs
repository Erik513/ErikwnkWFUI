using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // What a screen reader is told about the slim bar.
    public partial class SlimProgressBar
    {
        // A screen reader asks for the value of a progress bar: the percentage ("42%"), and the text as its name unless a name was set.
        protected override AccessibleObject CreateAccessibilityInstance()
        {
            return new SlimProgressBarAccessibleObject(this);
        }

        private sealed class SlimProgressBarAccessibleObject : ControlAccessibleObject
        {
            private readonly SlimProgressBar _bar;

            public SlimProgressBarAccessibleObject(SlimProgressBar bar)
                : base(bar)
            {
                _bar = bar;
            }

            public override AccessibleRole Role
            {
                get { return AccessibleRole.ProgressBar; }
            }

            public override string Name
            {
                get { return base.Name ?? (string.IsNullOrEmpty(_bar.Text) ? null : _bar.Text); }
                set { base.Name = value; }
            }

            public override string Value
            {
                get { return _bar._style == ProgressBarStyle.Marquee ? null : ProgressBarPainting.GetPercentText(_bar._minimum, _bar._maximum, _bar._value); }
                set { }
            }
        }
    }
}

