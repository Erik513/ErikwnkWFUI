using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // What a screen reader is told about the switch.
    public partial class ToggleSwitch
    {
        /// <inheritdoc/>
        protected override AccessibleObject CreateAccessibilityInstance()
        {
            return new ToggleSwitchAccessibleObject(this);
        }

        // What a screen reader is told: a check button (a switch is read as one), whether it is on, and that it can be toggled. The name
        // is the AccessibleName of the control (there is no text on the switch; set it, or let a label beside it name it).
        private sealed class ToggleSwitchAccessibleObject : ControlAccessibleObject
        {
            private readonly ToggleSwitch _switch;

            public ToggleSwitchAccessibleObject(ToggleSwitch toggle)
                : base(toggle)
            {
                _switch = toggle;
            }

            public override AccessibleRole Role
            {
                get { return AccessibleRole.CheckButton; }
            }

            public override AccessibleStates State
            {
                get { return _switch.Checked ? base.State | AccessibleStates.Checked : base.State; }
            }

            public override string DefaultAction
            {
                get { return _switch.Enabled ? UIStrings.Get("ToggleSwitch.Toggle") : string.Empty; }
            }

            public override void DoDefaultAction()
            {
                if (_switch.Enabled)
                    _switch.ToggleByUser();
            }
        }
    }
}
