using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A row (or section header) of a <see cref="PropertyTable"/>, as <c>AddRow</c> and <c>AddSection</c> return it: it can be hidden or
    /// disabled later, for an option that depends on another one. After <see cref="PropertyTable.ClearRows"/> the row is gone and
    /// the handle does nothing any more.
    /// </summary>
    public sealed class PropertyRow
    {
        private readonly RowStyle _style;
        private readonly float _height;
        private readonly Control[] _parts;
        private bool _visible = true;

        internal PropertyRow(RowStyle style, params Control[] parts)
        {
            _style = style;
            _height = style.Height;
            _parts = parts;
        }

        /// <summary>Whether the row is shown (the default). Hidden, it takes no room: the rows below move up.</summary>
        public bool Visible
        {
            get { return _visible; }
            set
            {
                if (value == _visible || IsGone)
                    return;

                _visible = value;
                _style.Height = value ? _height : 0f;

                foreach (Control part in _parts)
                    part.Visible = value;
            }
        }

        /// <summary>Whether the label and the editors of the row can be used (the default). The editors keep their own <c>Enabled</c>; they are only dimmed and locked as long as the row is not.</summary>
        public bool Enabled
        {
            get { return _parts.Length == 0 || _parts[0].Enabled; }
            set
            {
                if (IsGone)
                    return;

                foreach (Control part in _parts)
                    part.Enabled = value;
            }
        }

        private bool IsGone
        {
            get { return _parts.Length == 0 || _parts[0].IsDisposed; }
        }
    }
}
