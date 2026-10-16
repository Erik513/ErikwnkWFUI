namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// Which lines of a table with <c>CellBorderStyle.Single</c> are shown (<see cref="TableLayoutPanel.CellBorderLines"/>).
    /// </summary>
    public enum CellBorderLines
    {
        /// <summary>The lines around the table and the ones between the cells (the standard look).</summary>
        All,

        /// <summary>Only the lines around the whole table.</summary>
        OuterOnly,

        /// <summary>Only the lines between the cells.</summary>
        InnerOnly
    }
}
