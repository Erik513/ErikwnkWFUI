using System;
using System.Collections.Generic;
using System.Text;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Controls;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;
using TableLayoutPanel = System.Windows.Forms.TableLayoutPanel;
using Panel = System.Windows.Forms.Panel;
using CheckBox = System.Windows.Forms.CheckBox;

namespace ErikwnkWFUI.Forms
{
    /// <summary>Which buttons a <see cref="MessageBox"/> shows.</summary>
    public enum MessageBoxButtons
    {
        OK,
        OKCancel,
        YesNo,
        YesNoCancel,
        RetryCancel,
        AbortRetryIgnore
    }

    /// <summary>Which icon a <see cref="MessageBox"/> shows in its title bar.</summary>
    public enum MessageBoxIcon
    {
        None,
        Info,
        Warning,
        Error,
        Question,
        Success
    }

    /// <summary>Which button of a <see cref="MessageBox"/> has the focus first (and answers Enter).</summary>
    public enum MessageBoxDefault
    {
        /// <summary>The safe one: No for Yes/No, Cancel where there is a Cancel button, Abort for Abort/Retry/Ignore, OK for OK. A stray key press then does no harm.</summary>
        Safe,

        /// <summary>The first button (the left one: OK or Yes).</summary>
        Button1,

        /// <summary>The second button (No, Cancel of OK/Cancel and Retry/Cancel, Retry of Abort/Retry/Ignore).</summary>
        Button2,

        /// <summary>The third button (Cancel of Yes/No/Cancel, Ignore of Abort/Retry/Ignore).</summary>
        Button3
    }

    /// <summary>Overall size preset for a <see cref="MessageBox"/>. Use <see cref="FormSize"/>, the one size setting of all the windows.</summary>
    [Obsolete("Use FormSize (the same three sizes, for every window and dialog of the library).")]
    public enum MessageBoxSize
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// ErikwnkWFUI's themed replacement for
    /// <see cref="System.Windows.Forms.MessageBox"/> - call <c>Show</c>.
    /// </summary>
    /// <remarks>
    /// Named the same as the native dialog it replaces, same as
    /// <see cref="Controls.ListView"/> and friends - callers never need to
    /// spell out <c>ErikwnkWFUI.Forms.MessageBox</c> themselves as long as
    /// they don't also have <c>using System.Windows.Forms;</c> in scope
    /// wherever they call <c>Show</c>.
    /// </remarks>
    public static class MessageBox
    {
        /// <summary>The old way to give the size, with <see cref="MessageBoxSize"/>.</summary>
        [Obsolete("Use the size: FormSize (or an own Size) of Show.")]
        public static DialogResult Show(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon, Form owner, MessageBoxSize size)
        {
            return Show(message, title, buttons, icon, owner, (FormSize)(int)size);
        }

        /// <summary>The old way to give the size, with <see cref="MessageBoxSize"/> and no owner.</summary>
        [Obsolete("Use the size: FormSize (or an own Size) of Show.")]
        public static DialogResult Show(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxSize size)
        {
            return Show(message, title, buttons, icon, null, (FormSize)(int)size);
        }

        /// <summary>Shows the dialog modally and returns which button was clicked (or the safe result - No, Cancel, Abort or OK, as the set has it - if the dialog is closed without clicking one).</summary>
        /// <param name="message">The text of the dialog.</param>
        /// <param name="title">The title of the window ("Notice" or "Hinweis" when none is given).</param>
        /// <param name="buttons">Which buttons it has.</param>
        /// <param name="icon">The icon in the title bar.</param>
        /// <param name="owner">The window it stands over (null: the middle of the screen).</param>
        /// <param name="size">One of the three sizes (<see cref="FormSize.Medium"/> by default) or an own <see cref="Size"/>; in pixels as they are, not scaled to the screen.</param>
        /// <param name="colors">A color set for the title bar (one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or a <see cref="Color"/>); null is the normal dark one.</param>
        /// <param name="details">A text the user can open under the message with a "Details" button, for instance the text of an exception (null: none).</param>
        /// <param name="defaultButton">Which button has the focus first and answers Enter: the safe one (No, Cancel) by default.</param>
        /// <param name="buttonTexts">Own texts for the buttons, in their order (a null entry keeps the symbol or the usual text of that button): "Save", "Discard", "Cancel" for Yes/No/Cancel, say. An &amp; in a text makes the next letter the access key (&amp;&amp; is an ampersand).</param>
        /// <param name="playSound">Whether the sound of the icon is played when the box opens (the sounds of Windows for information, warning, error and question); off by default.</param>
        public static DialogResult Show(
            string message,
            string title = null,
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Info,
            Form owner = null,
            FormSizeOption? size = null,
            UIColorSet colors = null,
            string details = null,
            MessageBoxDefault defaultButton = MessageBoxDefault.Safe,
            string[] buttonTexts = null,
            bool playSound = false)
        {
            MessageBoxForm form = UIStyles.Forms.CreateMessageStandard(message, title, buttons, icon, colors, size, details, defaultButton, buttonTexts, playSound);

            try
            {
                if (owner != null)
                    return form.ShowDialog(owner);

                return form.ShowDialog();
            }
            finally
            {
                form.Dispose();
            }
        }

        /// <summary>
        /// Like <c>Show</c> with a check box under the message ("Do not ask again", "Apply to all"): <paramref name="isChecked"/> is what it
        /// starts with and what the user left it at (also when the dialog was closed without a button).
        /// </summary>
        /// <param name="message">The text of the dialog.</param>
        /// <param name="checkBoxText">The text of the check box.</param>
        /// <param name="isChecked">Whether the check box is checked: at the start, and when the dialog has ended.</param>
        /// <param name="title">The title of the window ("Notice" or "Hinweis" when none is given).</param>
        /// <param name="buttons">Which buttons it has.</param>
        /// <param name="icon">The icon in the title bar.</param>
        /// <param name="owner">The window it stands over (null: the middle of the screen).</param>
        /// <param name="size">One of the three sizes or an own <see cref="Size"/>.</param>
        /// <param name="colors">A color set for the title bar; null is the normal dark one.</param>
        public static DialogResult ShowWithCheckBox(
            string message,
            string checkBoxText,
            ref bool isChecked,
            string title = null,
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Info,
            Form owner = null,
            FormSizeOption? size = null,
            UIColorSet colors = null)
        {
            MessageBoxForm form = UIStyles.Forms.CreateMessageStandard(message, title, buttons, icon, colors, size);
            form.CheckBoxText = checkBoxText;
            form.CheckBoxChecked = isChecked;

            try
            {
                DialogResult result = owner != null ? form.ShowDialog(owner) : form.ShowDialog();
                isChecked = form.CheckBoxChecked;

                return result;
            }
            finally
            {
                form.Dispose();
            }
        }
    }
}
