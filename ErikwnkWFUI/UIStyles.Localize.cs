using System;
using System.Windows.Forms;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>
        /// Gives a control a text that follows the language: <paramref name="text"/> is asked now and again each time the language changes
        /// (<see cref="LanguageChanged"/>), so a program's own texts - from <c>AppLocalization.Get</c>, from its own class, from anywhere -
        /// change with the library's. The binding ends when the control is disposed. Returns the control, to use it in a line:
        /// <c>UIStyles.Localize(UIStyles.Labels.CreateStandard(), () =&gt; Loc.S("name"))</c>.
        /// </summary>
        public static T Localize<T>(T control, Func<string> text) where T : Control
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            return Localize(control, c => c.Text = text() ?? "");
        }

        /// <summary>
        /// The same for anything a control shows that depends on the language (a tooltip, a list of items, a text of its own):
        /// <paramref name="apply"/> is called now and each time the language changes, on the thread of the control.
        /// </summary>
        public static T Localize<T>(T control, Action<T> apply) where T : Control
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (apply == null)
                throw new ArgumentNullException(nameof(apply));

            apply(control);

            EventHandler changed = (sender, e) =>
            {
                if (control.IsDisposed)
                    return;

                if (control.InvokeRequired)
                {
                    if (control.IsHandleCreated)
                        control.BeginInvoke(new Action(() => { if (!control.IsDisposed) apply(control); }));
                }
                else
                {
                    apply(control);
                }
            };

            UIStrings.LanguageChanged += changed;
            control.Disposed += (sender, e) => UIStrings.LanguageChanged -= changed;

            return control;
        }
    }
}
