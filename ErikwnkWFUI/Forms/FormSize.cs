using System;
using System.Drawing;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// The three sizes every window and dialog of the library can have. What a size means depends on the kind of window: a normal window
    /// is larger than a dialog, a progress dialog is wider than an input box is high, and so on (see <see cref="FormSizeOption"/>); the
    /// sizes are given for 96 dpi and scaled to the screen.
    /// </summary>
    public enum FormSize
    {
        /// <summary>The compact size.</summary>
        Small,

        /// <summary>The usual size (the default of the dialogs).</summary>
        Medium,

        /// <summary>The roomy size.</summary>
        Large
    }

    /// <summary>
    /// The size of a window or dialog: one of the three <see cref="FormSize"/> values or an own <see cref="Size"/>, one parameter for both -
    /// <c>size: FormSize.Small</c> or <c>size: new Size(500, 300)</c> (both convert by themselves). An own size is for 96 dpi like the
    /// ready-made ones. For a dialog whose height follows its content (input box, progress dialog, options window) the width is the one that is
    /// given, and the height is the least height: the content decides when it needs more.
    /// </summary>
    public readonly struct FormSizeOption : IEquatable<FormSizeOption>
    {
        private readonly FormSize _size;
        private readonly Size? _custom;

        private FormSizeOption(FormSize size, Size? custom)
        {
            _size = size;
            _custom = custom;
        }

        /// <summary>The own size, when there is one (null: one of the ready-made sizes).</summary>
        public Size? Custom
        {
            get { return _custom; }
        }

        /// <summary>The ready-made size (<see cref="FormSize.Medium"/> for an own size, which does not have one).</summary>
        public FormSize Preset
        {
            get { return _custom.HasValue ? FormSize.Medium : _size; }
        }

        /// <summary>A ready-made size is a size option.</summary>
        public static implicit operator FormSizeOption(FormSize size)
        {
            return new FormSizeOption(size, null);
        }

        /// <summary>A size of your own (in pixels at 96 dpi) is a size option.</summary>
        public static implicit operator FormSizeOption(Size size)
        {
            return new FormSizeOption(FormSize.Medium, size);
        }

        /// <summary>The size for 96 dpi: the own one, or the one the kind of window has for the ready-made size.</summary>
        internal Size Resolve(FormKind kind)
        {
            return _custom ?? FormSizes.Get(kind, _size);
        }

        /// <summary>Whether the other option is the same ready-made size or the same own size.</summary>
        public bool Equals(FormSizeOption other)
        {
            return _size == other._size && _custom == other._custom;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is FormSizeOption && Equals((FormSizeOption)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _custom.HasValue ? _custom.Value.GetHashCode() : (int)_size;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return _custom.HasValue ? _custom.Value.Width + " x " + _custom.Value.Height : _size.ToString();
        }
    }

    // The kinds of window that have sizes of their own.
    internal enum FormKind
    {
        Window,
        Dialog,
        Settings,
        Input,
        Progress,
        Options,
        MessageBox,
        Update
    }

    // The sizes (for 96 dpi) of each kind of window. For the dialogs whose height follows the content (Input, Progress, Options) only the
    // width counts - the height is the least one.
    internal static class FormSizes
    {
        public static Size Get(FormKind kind, FormSize size)
        {
            int index = (int)size;

            switch (kind)
            {
                case FormKind.Window:
                    return new[] { new Size(480, 320), new Size(800, 560), new Size(1200, 800) }[index];
                case FormKind.Dialog:
                    return new[] { new Size(360, 240), new Size(480, 320), new Size(640, 440) }[index];
                case FormKind.Settings:
                    return new[] { new Size(420, 320), new Size(540, 420), new Size(720, 560) }[index];
                case FormKind.Input:
                    return new[] { new Size(340, 0), new Size(400, 0), new Size(520, 0) }[index];
                case FormKind.Progress:
                    return new[] { new Size(380, 0), new Size(480, 0), new Size(640, 0) }[index];
                case FormKind.Options:
                    return new[] { new Size(420, 0), new Size(520, 0), new Size(680, 0) }[index];
                case FormKind.Update:
                    return new[] { new Size(360, 170), new Size(420, 190), new Size(560, 250) }[index];
                default:
                    return new[] { new Size(380, 160), new Size(500, 220), new Size(700, 320) }[index];
            }
        }
    }
}
