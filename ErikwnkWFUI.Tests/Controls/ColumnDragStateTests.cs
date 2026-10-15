using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Tests.Controls;

/// <summary>The state of the drag of a column title, which ListView and ReadOnlyDataGridView share.</summary>
public class ColumnDragStateTests
{
    [Fact]
    public void ANewState_HasNoDrag_AndNoTarget()
    {
        ColumnDragState state = new ColumnDragState();

        Assert.False(state.IsDragging);
        Assert.Equal(-1, state.ColumnIndex);
        Assert.Equal(-1, state.InsertBeforeDisplayIndex);
        Assert.False(state.HasTarget);
    }

    [Fact]
    public void WhileTheDragRuns_TheColumnIsKnown_AndAfterwardsEverythingIsReset_AndTheCallbackIsCalled()
    {
        ColumnDragState state = new ColumnDragState();
        bool afterwards = false;

        state.Run(2, () =>
        {
            Assert.True(state.IsDragging);
            Assert.Equal(2, state.ColumnIndex);
            Assert.Equal(-1, state.InsertBeforeDisplayIndex);
            Assert.False(state.HasTarget);

            Assert.True(state.MoveInsertTo(1));
            Assert.True(state.HasTarget);
        }, () => afterwards = true);

        Assert.True(afterwards);
        Assert.False(state.IsDragging);
        Assert.Equal(-1, state.ColumnIndex);
        Assert.Equal(-1, state.InsertBeforeDisplayIndex);
    }

    [Fact]
    public void ADragThatEndsWithAnError_IsReset_AndTheErrorGoesOn()
    {
        ColumnDragState state = new ColumnDragState();
        bool afterwards = false;

        Assert.Throws<InvalidOperationException>(() => state.Run(0, () => throw new InvalidOperationException(), () => afterwards = true));

        Assert.True(afterwards);
        Assert.False(state.IsDragging);
        Assert.Equal(-1, state.ColumnIndex);
    }

    [Fact]
    public void MoveInsertTo_SaysWhetherThePlaceChanged()
    {
        ColumnDragState state = new ColumnDragState();

        Assert.True(state.MoveInsertTo(3));
        Assert.False(state.MoveInsertTo(3));
        Assert.True(state.MoveInsertTo(0));
        Assert.Equal(0, state.InsertBeforeDisplayIndex);
    }

    [Fact]
    public void ATargetNeedsAColumnAndAPlace()
    {
        ColumnDragState state = new ColumnDragState { ColumnIndex = 1 };
        Assert.False(state.HasTarget);

        state.InsertBeforeDisplayIndex = 0;
        Assert.True(state.HasTarget);

        state.ColumnIndex = -1;
        Assert.False(state.HasTarget);
    }
}
