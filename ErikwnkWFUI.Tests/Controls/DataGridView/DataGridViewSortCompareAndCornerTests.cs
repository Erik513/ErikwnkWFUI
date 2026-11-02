using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>How two cell values are put in order when the user sorts by a column (nothing, one of them, two kinds of value, two that cannot be compared), and the square where both scroll bars meet.</summary>
public class DataGridViewSortCompareAndCornerTests
{
    private static readonly MethodInfo Compare = typeof(ReadOnlyDataGridView).GetMethod("CompareCellValues", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static int CompareValues(object? x, object? y, int direction) => (int)Compare.Invoke(null, new[] { x, y, direction })!;

    [Fact]
    public void NothingComesFirstInAscendingOrder_AndLastInDescendingOrder()
    {
        Assert.Equal(0, CompareValues(null, null, 1));
        Assert.Equal(-1, CompareValues(null, 5, 1));
        Assert.Equal(1, CompareValues(null, 5, -1));
        Assert.Equal(1, CompareValues(5, null, 1));
        Assert.Equal(-1, CompareValues(5, null, -1));
    }

    [Fact]
    public void ValuesThatCanBeCompared_AreComparedAndTheDirectionTurnsTheAnswerAround()
    {
        Assert.True(CompareValues(1, 2, 1) < 0);
        Assert.True(CompareValues(1, 2, -1) > 0);
        Assert.Equal(0, CompareValues("a", "a", 1));
    }

    [Fact]
    public void ValuesOfAnotherKind_OrThatCannotBeCompared_AreComparedAsTexts()
    {
        // (An int cannot be compared with a string: the texts are, "10" before "9" as texts.)
        Assert.True(CompareValues(10, "9", 1) < 0);

        object first = new object();
        object second = new object();
        Assert.Equal(string.Compare(first.ToString(), second.ToString(), StringComparison.CurrentCultureIgnoreCase), CompareValues(first, second, 1));
    }

    [Fact]
    public void WhereBothScrollBarsMeet_TheSquareIsPaintedInTheColorOfTheGrid()
    {
        using ReadOnlyDataGridView grid = new ReadOnlyDataGridView { Width = 160, Height = 120, ScrollBars = ScrollBars.Both };

        for (int i = 0; i < 8; i++)
            grid.Columns.Add("c" + i, "Column " + i);

        for (int i = 0; i < 40; i++)
            grid.Rows.Add(new object[8]);

        using Form host = OffscreenForm.Show(grid, new Size(180, 140), Color.Black, bounds: new Rectangle(0, 0, 160, 120));
        Application.DoEvents();

        using Bitmap picture = Draw(grid);

        Assert.Equal(160, picture.Width);
    }
}
