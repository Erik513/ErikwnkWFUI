using System.Windows.Forms;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>IsInputKey - decides whether Ctrl+V/Ctrl+X reach this control's own OnKeyDown at all, or pass through as an unclaimed shortcut.</summary>
public class DataGridViewKeyHandlingTests
{
    [Fact]
    public void IsInputKey_CtrlV_EditableGrid_IsClaimed()
    {
        using TestableDataGridView grid = new TestableDataGridView { ReadOnly = false };

        Assert.True(grid.PublicIsInputKey(Keys.Control | Keys.V));
    }

    [Fact]
    public void IsInputKey_CtrlX_EditableGrid_IsClaimed()
    {
        using TestableDataGridView grid = new TestableDataGridView { ReadOnly = false };

        Assert.True(grid.PublicIsInputKey(Keys.Control | Keys.X));
    }

    [Fact]
    public void IsInputKey_CtrlV_ReadOnlyGrid_IsNotClaimed()
    {
        // Regression test: claiming Ctrl+V on a ReadOnly grid used to
        // swallow the keystroke silently (OnKeyDown's own ReadOnly check
        // means it never actually pastes), instead of letting it pass
        // through to whatever ambient shortcut/mnemonic would otherwise
        // handle it - exactly as it did before this control had any
        // clipboard support at all.
        using TestableDataGridView grid = new TestableDataGridView { ReadOnly = true };

        Assert.False(grid.PublicIsInputKey(Keys.Control | Keys.V));
    }

    [Fact]
    public void IsInputKey_CtrlX_ReadOnlyGrid_IsNotClaimed()
    {
        using TestableDataGridView grid = new TestableDataGridView { ReadOnly = true };

        Assert.False(grid.PublicIsInputKey(Keys.Control | Keys.X));
    }

    [Fact]
    public void IsInputKey_PlainVWithoutControl_IsNotClaimed()
    {
        using TestableDataGridView grid = new TestableDataGridView { ReadOnly = false };

        Assert.False(grid.PublicIsInputKey(Keys.V));
    }
}
