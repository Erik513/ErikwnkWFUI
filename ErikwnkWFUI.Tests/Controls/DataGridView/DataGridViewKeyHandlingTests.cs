using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>
/// IsInputKey - decides whether Ctrl+V/Ctrl+X reach this control's own
/// OnKeyDown at all, or pass through as an unclaimed shortcut - plus
/// ProcessDataGridViewKey (the Delete-key path) and OnKeyDown itself (the
/// Ctrl+X/Ctrl+V dispatch), both invoked directly via reflection since
/// there's no real keyboard to drive here either way. Some of these touch
/// the clipboard, so the whole class carries the same
/// [Collection(ClipboardTestCollection.Name)] every other clipboard-touching
/// test class does (see ClipboardTestCollection).
/// </summary>
[Collection(ClipboardTestCollection.Name)]
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

    [Fact]
    public void ProcessDataGridViewKey_Delete_DeletesEverySelectedRow()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1), ("B", 2));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
        GridTestHelpers.SelectCells(grid, (0, 0));

        bool handled = grid.InvokePrivate<bool>("ProcessDataGridViewKey", new KeyEventArgs(Keys.Delete));

        Assert.True(handled);
        Assert.Single(items);
        Assert.Equal("B", items[0].Name);
        Assert.Equal(new[] { "Row deleted" }, grid.ActionConfirmationMessages);
    }

    [Fact]
    public void ProcessDataGridViewKey_Delete_AllowUserToDeleteRowsFalse_DoesNotDelete()
    {
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
        grid.AllowUserToDeleteRows = false;
        GridTestHelpers.SelectCells(grid, (0, 0));

        grid.InvokePrivate<bool>("ProcessDataGridViewKey", new KeyEventArgs(Keys.Delete));

        Assert.Single(items);
        Assert.Empty(grid.ActionConfirmationMessages);
    }

    // The IsCurrentCellInEditMode guard on Delete/Ctrl+X (below) isn't
    // covered here - BeginEdit needs a real, laid-out editing control host,
    // which crashes on Dispose for a handle-only, never-shown grid like
    // these tests use (confirmed live: ArgumentOutOfRangeException deep in
    // DataGridView.PositionEditingControl) - the same "isn't practical to
    // drive headlessly" limitation DataGridViewColumnResizeTests/
    // DataGridViewColumnReorderTests already document for Cursor.Position-
    // driven mechanics.

    [Fact]
    public void ProcessDataGridViewKey_NonDeleteKey_FallsThroughToBase()
    {
        // F5 has no default DataGridView behavior to speak of (unlike e.g.
        // Enter, which commits/advances the current row) - a safe stand-in
        // for "some other key" that doesn't risk side effects of its own.
        BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
        using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
        GridTestHelpers.SelectCells(grid, (0, 0));

        grid.InvokePrivate<bool>("ProcessDataGridViewKey", new KeyEventArgs(Keys.F5));

        Assert.Single(items);
        Assert.Empty(grid.ActionConfirmationMessages);
    }

    [Fact]
    public void OnKeyDown_CtrlX_EditableGridWithSelection_CutsAndMarksHandled()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0));

            KeyEventArgs args = new KeyEventArgs(Keys.Control | Keys.X);
            grid.InvokePrivate("OnKeyDown", args);

            Assert.True(args.Handled);
            Assert.Equal(new[] { "Cell cut" }, grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void OnKeyDown_CtrlV_EditableGridWithClipboardText_PastesAndMarksHandled()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0));
            Clipboard.SetText("Pasted\t99");

            KeyEventArgs args = new KeyEventArgs(Keys.Control | Keys.V);
            grid.InvokePrivate("OnKeyDown", args);

            Assert.True(args.Handled);
            Assert.Equal("Pasted", items[0].Name);
            Assert.Equal(99, items[0].Value);
        });
    }

    [Fact]
    public void OnKeyDown_CtrlX_ReadOnlyGrid_DoesNothing()
    {
        // Belt-and-suspenders with IsInputKey's own ReadOnly guard (see
        // above) - even if a ReadOnly grid's OnKeyDown somehow got called
        // directly for Ctrl+X, it must still refuse to cut.
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            grid.ReadOnly = true;
            GridTestHelpers.SelectCells(grid, (0, 0));

            KeyEventArgs args = new KeyEventArgs(Keys.Control | Keys.X);
            grid.InvokePrivate("OnKeyDown", args);

            Assert.False(args.Handled);
            Assert.Empty(grid.ActionConfirmationMessages);
        });
    }

    [Fact]
    public void OnKeyDown_PlainXWithoutControl_FallsThroughToBase()
    {
        StaThread.Run(() =>
        {
            BindingList<TestItem> items = GridTestHelpers.CreateItems(("A", 1));
            using TestableDataGridView grid = GridTestHelpers.CreateTestableGrid(items);
            GridTestHelpers.SelectCells(grid, (0, 0));

            KeyEventArgs args = new KeyEventArgs(Keys.X);
            grid.InvokePrivate("OnKeyDown", args);

            Assert.False(args.Handled);
            Assert.Empty(grid.ActionConfirmationMessages);
        });
    }
}
