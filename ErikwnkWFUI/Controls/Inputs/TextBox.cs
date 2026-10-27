using System;
using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The text box of the library: the standard <see cref="System.Windows.Forms.TextBox"/> with the frame of the library (a quiet outline, and with the
    /// focus the color of <see cref="Colors"/>) instead of the system's. Make one with <c>UIStyles.TextBoxes</c>; the factories give the
    /// standard type. The class is not public on purpose: a public <c>TextBox</c> in <c>ErikwnkWFUI.Controls</c> would make the name ambiguous
    /// in every file that uses both namespaces. The color is set through the factory (<c>colors:</c>, <c>CreatePrimary</c>).
    /// </summary>
    internal class TextBox : System.Windows.Forms.TextBox
    {
        private readonly ColorSetHolder _colorSet = new ColorSetHolder();
        private readonly Action _redrawFrame;

        public TextBox()
        {
            // The border of BorderStyle.FixedSingle is part of the window outside its client area, in the system's gray: the library paints
            // its own frame over it - a quiet outline, with the focus the color of the set - and moves the line to the middle of the box.
            _redrawFrame = InputField.AttachFrame(
                this,
                () => BorderStyle == BorderStyle.FixedSingle,
                () => InputField.GetFrameColor(this, Colors, Focused),
                () => InputField.GetTextTopInset(this));
        }

        /// <summary>
        /// The color of the frame while the box has the focus: the <c>Hover</c> color of the set is the main color (brought to where it is
        /// seen against the field). Null (the default) is the text color of the field; <c>UIStyles.TextBoxes.CreatePrimary</c> makes one that
        /// follows the accent; <c>CreateStandard(colors: ...)</c> takes a color set or simply a color. The fill and the text stay those
        /// of the theme. Not written by the designer (a set is code).
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIColorSet Colors
        {
            get { return _colorSet.Value; }
            set
            {
                _colorSet.Value = value;
                _redrawFrame();
            }
        }

        // A set that is asked for again each time it is needed (the primary set, which follows the accent); a later Colors replaces it.
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorSet.Follow(source);
            _redrawFrame();
        }
    }
}
