using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // What a screen reader is told about the spinner.
    public partial class Spinner
    {
        /// <inheritdoc/>
        protected override AccessibleObject CreateAccessibilityInstance()
        {
            return new SpinnerAccessibleObject(this);
        }

        // A screen reader asks for the value of a progress bar: the percentage when the load is known, nothing when it is not.
        private sealed class SpinnerAccessibleObject : ControlAccessibleObject
        {
            private readonly Spinner _spinner;

            public SpinnerAccessibleObject(Spinner spinner)
                : base(spinner)
            {
                _spinner = spinner;
            }

            public override AccessibleRole Role
            {
                get { return AccessibleRole.ProgressBar; }
            }

            public override string Name
            {
                get { return base.Name ?? (string.IsNullOrEmpty(_spinner.Text) ? null : _spinner.Text); }
                set { base.Name = value; }
            }

            public override string Value
            {
                get { return _spinner._isIndeterminate ? null : ProgressBarPainting.GetPercentText(0, 100, _spinner._value); }
                set { }
            }
        }
    }
}

