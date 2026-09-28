using System.Collections.Generic;
using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Tests.Controls;

/// <summary>
/// ColumnLayoutMath backs both ListView's and ReadOnlyDataGridView's own
/// hand-rolled column reorder/resize math - this used to be re-derived,
/// nearly verbatim, through each control's own private wrapper method
/// (MoveColumnToDisplayIndex, TryGetColumnAtBorder, ...) in its own test
/// file. Tested directly here instead, via InternalsVisibleTo (see
/// ErikwnkWFUI/InternalsVisibleTo.cs) - it's a standalone utility type, not
/// one of the controls' own private members. Columns are plain ints (the
/// width itself, via the identity accessor) rather than real
/// DataGridViewColumn/ColumnHeader instances - the algorithm only ever
/// needs a width per column, and using real column types here would just
/// be extra setup for no extra coverage. Each control's own test file now
/// keeps just ONE minimal test confirming its own wrapper actually
/// delegates to this, not the full scenario matrix covered once here.
/// </summary>
public class ColumnLayoutMathTests
{
    private static readonly System.Func<int, int> Identity = width => width;

    [Fact]
    public void GetMoveTargetDisplayIndex_MovingPastItsOwnPosition_SubtractsOneForTheGapItLeavesBehind()
    {
        // Moving column 0 to "insert before display index 2" (past the
        // last column) - removing it from slot 0 first shifts everything
        // after it left by one, so the real target is display index 1, not 2.
        int? target = ColumnLayoutMath.GetMoveTargetDisplayIndex(originalDisplayIndex: 0, insertBeforeDisplayIndex: 2);

        Assert.Equal(1, target);
    }

    [Fact]
    public void GetMoveTargetDisplayIndex_MovingBeforeItsOwnPosition_UsesTheInsertionIndexAsIs()
    {
        int? target = ColumnLayoutMath.GetMoveTargetDisplayIndex(originalDisplayIndex: 1, insertBeforeDisplayIndex: 0);

        Assert.Equal(0, target);
    }

    [Fact]
    public void GetMoveTargetDisplayIndex_SameTargetAsCurrent_ReturnsNull()
    {
        int? target = ColumnLayoutMath.GetMoveTargetDisplayIndex(originalDisplayIndex: 0, insertBeforeDisplayIndex: 0);

        Assert.Null(target);
    }

    [Fact]
    public void GetDropInsertionIndex_XPastEveryColumn_ReturnsInsertAfterTheLastOne()
    {
        List<int> columns = new List<int> { 100, 100 };

        int index = ColumnLayoutMath.GetDropInsertionIndex(columns, Identity, x: 100_000);

        Assert.Equal(2, index);
    }

    [Fact]
    public void GetDropInsertionIndex_XAtTheVeryStart_ReturnsInsertBeforeTheFirstColumn()
    {
        List<int> columns = new List<int> { 100, 100 };

        int index = ColumnLayoutMath.GetDropInsertionIndex(columns, Identity, x: 0);

        Assert.Equal(0, index);
    }

    [Fact]
    public void GetDropInsertionIndex_SnapsToWhicheverSideOfAColumnsMidpointXIsCloserTo()
    {
        // One column, width 100 - its midpoint is x=50.
        List<int> columns = new List<int> { 100 };

        Assert.Equal(0, ColumnLayoutMath.GetDropInsertionIndex(columns, Identity, x: 49));
        Assert.Equal(1, ColumnLayoutMath.GetDropInsertionIndex(columns, Identity, x: 51));
    }

    [Fact]
    public void TryGetColumnAtBorder_WithinGripToleranceOfABorder_ReturnsThatColumn()
    {
        List<int> columns = new List<int> { 100, 60 };

        bool found = ColumnLayoutMath.TryGetColumnAtBorder(columns, Identity, x: 100, gripWidth: 5, out int column);

        Assert.True(found);
        Assert.Equal(100, column);
    }

    [Fact]
    public void TryGetColumnAtBorder_AtTheSecondColumnsBorder_ReturnsTheSecondColumn()
    {
        List<int> columns = new List<int> { 100, 60 };

        bool found = ColumnLayoutMath.TryGetColumnAtBorder(columns, Identity, x: 160, gripWidth: 5, out int column);

        Assert.True(found);
        Assert.Equal(60, column);
    }

    [Fact]
    public void TryGetColumnAtBorder_WellInsideAColumn_ReturnsFalse()
    {
        List<int> columns = new List<int> { 100 };

        bool found = ColumnLayoutMath.TryGetColumnAtBorder(columns, Identity, x: 50, gripWidth: 5, out int column);

        Assert.False(found);
        Assert.Equal(0, column); // default(int)
    }

    [Fact]
    public void TryGetColumnAtBorder_JustOutsideGripTolerance_ReturnsFalse()
    {
        List<int> columns = new List<int> { 100 };

        bool found = ColumnLayoutMath.TryGetColumnAtBorder(columns, Identity, x: 94, gripWidth: 5, out int column);

        Assert.False(found);
    }

    [Fact]
    public void OrderByDisplayIndex_SortsByTheGivenDisplayIndexRegardlessOfInputOrder()
    {
        List<(string Name, int DisplayIndex)> columns = new List<(string, int)>
        {
            ("C", 2),
            ("A", 0),
            ("B", 1)
        };

        List<(string Name, int DisplayIndex)> ordered = ColumnLayoutMath.OrderByDisplayIndex(columns, c => c.DisplayIndex);

        Assert.Equal("A", ordered[0].Name);
        Assert.Equal("B", ordered[1].Name);
        Assert.Equal("C", ordered[2].Name);
    }
}
