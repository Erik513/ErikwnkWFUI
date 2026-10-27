using System;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The old name of <see cref="PropertyColumn"/>: <c>UIColumn.Percent(...)</c>, <c>Absolute</c> and <c>Auto</c> still work (and give a
    /// <see cref="PropertyColumn"/>); code that declares a variable of the type <c>UIColumn</c> has to use <see cref="PropertyColumn"/>. It is
    /// removed once the projects that use it have been changed.
    /// </summary>
    [Obsolete("Use PropertyColumn (the same, a name that says which table it belongs to).")]
    public sealed class UIColumn : PropertyColumn
    {
        private UIColumn()
            : base(null, null)
        {
        }
    }
}
