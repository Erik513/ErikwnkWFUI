using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Panels;

/// <summary>The frame of a panel and the lines of a table: in the color of the theme, or the one that was set.</summary>
[Collection(AccentColorTestCollection.Name)]
public class PanelBorderTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private T Show<T>(T control) where T : Control
    {
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(300, 200) };
        control.Location = new Point(20, 20);
        host.Controls.Add(control);
        host.Show();
        Application.DoEvents();
        _forms.Add(host);

        return control;
    }

    private static Bitmap Draw(Control control)
    {
        Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));

        return bitmap;
    }

    private static Color Opaque(Color color) => Color.FromArgb(color.R, color.G, color.B);

    public static IEnumerable<object[]> ThreePanels()
    {
        yield return new object[] { (Func<Control>)(() => new ThemedPanel()) };
        yield return new object[] { (Func<Control>)(() => new ThemedFlowLayoutPanel { AutoSize = false }) };
        yield return new object[] { (Func<Control>)(() => new ThemedTableLayoutPanel()) };
    }

    private static void SetBorderStyle(Control panel, BorderStyle value) => panel.GetType().GetProperty("BorderStyle")!.SetValue(panel, value);

    private static BorderStyle GetBorderStyle(Control panel) => (BorderStyle)panel.GetType().GetProperty("BorderStyle")!.GetValue(panel)!;

    [Theory]
    [MemberData(nameof(ThreePanels))]
    public void BorderStyle_IsNoneByDefault_AndSwitchesTheFrameOfTheTheme(Func<Control> make)
    {
        Control panel = make();
        Assert.Equal(BorderStyle.None, GetBorderStyle(panel));

        panel.Size = new Size(100, 60);
        panel.BackColor = Color.Black;
        Show(panel);

        using (Bitmap picture = Draw(panel))
            Assert.Equal(Color.Black.ToArgb(), Opaque(picture.GetPixel(0, 30)).ToArgb());

        SetBorderStyle(panel, BorderStyle.FixedSingle);
        Application.DoEvents();

        Assert.Equal(BorderStyle.FixedSingle, GetBorderStyle(panel));

        using (Bitmap picture = Draw(panel))
            Assert.Equal(UIColors.BorderMedium, Opaque(picture.GetPixel(0, 30)));

        SetBorderStyle(panel, BorderStyle.None);
        Application.DoEvents();

        Assert.Equal(BorderStyle.None, GetBorderStyle(panel));

        using (Bitmap picture = Draw(panel))
            Assert.Equal(Color.Black.ToArgb(), Opaque(picture.GetPixel(0, 30)).ToArgb());
    }

    [Fact]
    public void ThePanelFrame_IsInTheColorOfTheTheme_UntilOneIsSet_AndFollowsTheTheme()
    {
        ThemedPanel panel = Show(new ThemedPanel { BorderStyle = BorderStyle.FixedSingle, Size = new Size(100, 60), BackColor = Color.Black });

        using (Bitmap picture = Draw(panel))
            Assert.Equal(UIColors.BorderMedium, Opaque(picture.GetPixel(0, 30)));

        panel.BorderColor = Color.Red;

        using (Bitmap picture = Draw(panel))
        {
            Assert.Equal(Color.Red.ToArgb(), Opaque(picture.GetPixel(0, 30)).ToArgb());
            Assert.Equal(Color.Red.ToArgb(), Opaque(picture.GetPixel(50, 0)).ToArgb());
            Assert.Equal(Color.Red.ToArgb(), Opaque(picture.GetPixel(99, 59)).ToArgb());
        }

        panel.BorderColor = Color.Empty;
        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        using (Bitmap picture = Draw(panel))
            Assert.Equal(UIColors.BorderMedium, Opaque(picture.GetPixel(0, 30)));
    }

    [Fact]
    public void WithoutABorder_NothingIsPainted_AndTheOtherPanelsHaveTheFrameToo()
    {
        ThemedPanel none = Show(new ThemedPanel { Size = new Size(100, 60), BackColor = Color.Black, BorderColor = Color.Red });
        ThemedFlowLayoutPanel flow = Show(new ThemedFlowLayoutPanel { AutoSize = false, BorderStyle = BorderStyle.FixedSingle, Size = new Size(100, 60), BackColor = Color.Black, BorderColor = Color.Red });
        ThemedTableLayoutPanel table = Show(new ThemedTableLayoutPanel { BorderStyle = BorderStyle.FixedSingle, Size = new Size(100, 60), BackColor = Color.Black, BorderColor = Color.Red });

        using (Bitmap picture = Draw(none))
            Assert.Equal(Color.Black.ToArgb(), Opaque(picture.GetPixel(0, 30)).ToArgb());

        using (Bitmap picture = Draw(flow))
            Assert.Equal(Color.Red.ToArgb(), Opaque(picture.GetPixel(0, 30)).ToArgb());

        using (Bitmap picture = Draw(table))
            Assert.Equal(Color.Red.ToArgb(), Opaque(picture.GetPixel(0, 30)).ToArgb());
    }

    private ThemedTableLayoutPanel Table()
    {
        ThemedTableLayoutPanel table = new ThemedTableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 2,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
            Size = new Size(100, 60),
            BackColor = Color.White
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        return Show(table);
    }

    [Fact]
    public void TheLinesOfATable_AreInTheColorOfTheTheme_UntilOneIsSet_AndFollowTheTheme()
    {
        ThemedTableLayoutPanel table = Table();

        using (Bitmap picture = Draw(table))
        {
            Assert.Equal(UIColors.BorderMedium, Opaque(picture.GetPixel(0, 30)));
            Assert.Equal(UIColors.BorderMedium, Opaque(picture.GetPixel(49, 15)));
        }

        table.CellBorderColor = Color.Red;

        using (Bitmap picture = Draw(table))
        {
            Assert.Equal(Color.Red.ToArgb(), Opaque(picture.GetPixel(0, 30)).ToArgb());
            Assert.Equal(Color.Red.ToArgb(), Opaque(picture.GetPixel(49, 15)).ToArgb());
            Assert.Equal(Color.White.ToArgb(), Opaque(picture.GetPixel(25, 15)).ToArgb());
        }

        table.CellBorderColor = Color.Empty;
        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        using (Bitmap picture = Draw(table))
            Assert.Equal(UIColors.BorderMedium, Opaque(picture.GetPixel(49, 15)));
    }

    [Fact]
    public void WithoutLines_OrWithAnotherStyle_TheTableIsLeftAlone()
    {
        ThemedTableLayoutPanel table = Table();
        table.CellBorderColor = Color.Red;
        table.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
        Application.DoEvents();

        using Bitmap picture = Draw(table);

        Assert.DoesNotContain(Enumerable.Range(0, 60).Select(y => Opaque(picture.GetPixel(49, y)).ToArgb()), argb => argb == Color.Red.ToArgb());
    }

    private ThemedTableLayoutPanel Grid(int columns, int rows, Color back)
    {
        ThemedTableLayoutPanel table = new ThemedTableLayoutPanel
        {
            ColumnCount = columns,
            RowCount = rows,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
            CellBorderColor = Color.Red,
            Size = new Size(120, 90),
            BackColor = back
        };

        for (int i = 0; i < columns; i++)
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));

        for (int i = 0; i < rows; i++)
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));

        return Show(table);
    }

    // Where the red lines are, read along a row / a column that does not meet a crossing.
    private static List<int> RedColumns(Bitmap picture) => Enumerable.Range(0, picture.Width).Where(x => Opaque(picture.GetPixel(x, 8)).ToArgb() == Color.Red.ToArgb()).ToList();

    private static List<int> RedRows(Bitmap picture) => Enumerable.Range(0, picture.Height).Where(y => Opaque(picture.GetPixel(8, y)).ToArgb() == Color.Red.ToArgb()).ToList();

    [Fact]
    public void CellBorderLines_AllIsTheDefault_AndShowsTheLinesAroundAndBetween()
    {
        ThemedTableLayoutPanel table = Grid(3, 3, Color.White);

        Assert.Equal(CellBorderLines.All, table.CellBorderLines);

        using Bitmap picture = Draw(table);
        Assert.Equal(4, RedColumns(picture).Count);
        Assert.Equal(4, RedRows(picture).Count);
        Assert.Equal(0, RedColumns(picture)[0]);
        Assert.Equal(table.Width - 1, RedColumns(picture)[3]);
    }

    [Fact]
    public void CellBorderLines_OuterOnly_ShowsTheLinesAroundTheTable_NoneBetweenTheCells_NotEvenTheCrossings()
    {
        ThemedTableLayoutPanel table = Grid(3, 3, Color.White);
        using Bitmap all = Draw(table);
        List<int> columns = RedColumns(all);
        List<int> rows = RedRows(all);

        table.CellBorderLines = CellBorderLines.OuterOnly;
        Application.DoEvents();

        using Bitmap picture = Draw(table);
        Assert.Equal(new[] { columns[0], columns[3] }, RedColumns(picture));
        Assert.Equal(new[] { rows[0], rows[3] }, RedRows(picture));

        // The crossings of the inner lines are gone, the corners of the outer line are there.
        Assert.Equal(Color.White.ToArgb(), Opaque(picture.GetPixel(columns[1], rows[1])).ToArgb());
        Assert.Equal(Color.White.ToArgb(), Opaque(picture.GetPixel(columns[2], rows[1])).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), Opaque(picture.GetPixel(columns[0], rows[0])).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), Opaque(picture.GetPixel(columns[3], rows[3])).ToArgb());

        // The line along the edge is whole where an inner line meets it.
        Assert.Equal(Color.Red.ToArgb(), Opaque(picture.GetPixel(columns[1], rows[0])).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), Opaque(picture.GetPixel(columns[0], rows[2])).ToArgb());
    }

    [Fact]
    public void CellBorderLines_InnerOnly_ShowsTheLinesBetweenTheCells_NoneAroundTheTable()
    {
        ThemedTableLayoutPanel table = Grid(3, 3, Color.White);
        using Bitmap all = Draw(table);
        List<int> columns = RedColumns(all);
        List<int> rows = RedRows(all);

        table.CellBorderLines = CellBorderLines.InnerOnly;
        Application.DoEvents();

        using Bitmap picture = Draw(table);
        Assert.Equal(new[] { columns[1], columns[2] }, RedColumns(picture));
        Assert.Equal(new[] { rows[1], rows[2] }, RedRows(picture));

        // Where they cross, and nothing on the outer lines.
        Assert.Equal(Color.Red.ToArgb(), Opaque(picture.GetPixel(columns[1], rows[1])).ToArgb());
        Assert.Equal(Color.White.ToArgb(), Opaque(picture.GetPixel(columns[0], rows[0])).ToArgb());
        Assert.Equal(Color.White.ToArgb(), Opaque(picture.GetPixel(columns[1], rows[0])).ToArgb());
        Assert.Equal(Color.White.ToArgb(), Opaque(picture.GetPixel(columns[3], rows[2])).ToArgb());
    }

    [Theory]
    [InlineData(1, 1, CellBorderLines.OuterOnly)]
    [InlineData(1, 1, CellBorderLines.InnerOnly)]
    [InlineData(1, 3, CellBorderLines.OuterOnly)]
    [InlineData(3, 1, CellBorderLines.OuterOnly)]
    [InlineData(1, 3, CellBorderLines.InnerOnly)]
    [InlineData(2, 2, CellBorderLines.OuterOnly)]
    public void CellBorderLines_OnAnyGridSize_AreDrawnWithoutAnError_EvenWhereThereIsNothingToHide(int columns, int rows, CellBorderLines lines)
    {
        ThemedTableLayoutPanel table = Grid(columns, rows, Color.White);
        table.CellBorderLines = lines;

        // (A control swallows an error while it paints and shows a red cross, so the painting is called directly.)
        Application.DoEvents();
        Assert.Null(Record.Exception(() => PaintDirectly(table)));

        // Also with a transparent table (a different way of painting).
        table.BackColor = Color.Transparent;
        Application.DoEvents();
        Assert.Null(Record.Exception(() => PaintDirectly(table)));
    }

    private static void PaintDirectly(ThemedTableLayoutPanel table)
    {
        using Bitmap picture = new Bitmap(table.Width, table.Height);
        using Graphics graphics = Graphics.FromImage(picture);
        PaintEventArgs args = new PaintEventArgs(graphics, new Rectangle(Point.Empty, table.Size));
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        // The cells are noted while the background is painted.
        typeof(ThemedTableLayoutPanel).GetMethod("OnPaintBackground", flags)!.Invoke(table, new object[] { args });
        try
        {
            typeof(ThemedTableLayoutPanel).GetMethod("OnPaint", flags)!.Invoke(table, new object[] { args });
        }
        catch (TargetInvocationException exception)
        {
            throw exception.InnerException!;
        }
    }

    private static List<int> ColumnsOf(Bitmap picture, Color color, int y = 8) => Enumerable.Range(0, picture.Width).Where(x => Opaque(picture.GetPixel(x, y)).ToArgb() == color.ToArgb()).ToList();

    private static List<int> RowsOf(Bitmap picture, Color color, int x = 8) => Enumerable.Range(0, picture.Height).Where(y => Opaque(picture.GetPixel(x, y)).ToArgb() == color.ToArgb()).ToList();

    [Fact]
    public void TheInnerAndTheOuterLines_CanHaveTheirOwnColors_TheCornersBelongToTheOuterOnes()
    {
        ThemedTableLayoutPanel table = Grid(3, 3, Color.White);
        using Bitmap all = Draw(table);
        List<int> columns = RedColumns(all);
        List<int> rows = RedRows(all);

        table.InnerCellBorderColor = Color.Blue;
        table.OuterCellBorderColor = Color.Green;
        Application.DoEvents();

        using Bitmap picture = Draw(table);
        Assert.Equal(new[] { columns[0], columns[3] }, ColumnsOf(picture, Color.Green));
        Assert.Equal(new[] { columns[1], columns[2] }, ColumnsOf(picture, Color.Blue));
        Assert.Equal(new[] { rows[0], rows[3] }, RowsOf(picture, Color.Green));
        Assert.Equal(new[] { rows[1], rows[2] }, RowsOf(picture, Color.Blue));
        Assert.Empty(RedColumns(picture));

        Assert.Equal(Color.Green.ToArgb(), Opaque(picture.GetPixel(columns[0], rows[0])).ToArgb());
        Assert.Equal(Color.Green.ToArgb(), Opaque(picture.GetPixel(columns[1], rows[0])).ToArgb());
        Assert.Equal(Color.Blue.ToArgb(), Opaque(picture.GetPixel(columns[1], rows[1])).ToArgb());
        Assert.Equal(Color.Blue.ToArgb(), Opaque(picture.GetPixel(columns[1], rows[3] - 1)).ToArgb());
    }

    [Fact]
    public void WithoutTheirOwnColor_TheInnerAndTheOuterLinesFollowTheCellBorderColor_AlsoWhenItChanges()
    {
        ThemedTableLayoutPanel table = Grid(2, 2, Color.White);

        Assert.Equal(Color.Red, table.InnerCellBorderColor);
        Assert.Equal(Color.Red, table.OuterCellBorderColor);

        table.CellBorderColor = Color.Orange;
        table.InnerCellBorderColor = Color.Blue;

        Assert.Equal(Color.Blue, table.InnerCellBorderColor);
        Assert.Equal(Color.Orange, table.OuterCellBorderColor);

        table.InnerCellBorderColor = Color.Empty;
        Assert.Equal(Color.Orange, table.InnerCellBorderColor);

        table.CellBorderColor = Color.Empty;
        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        using Bitmap picture = Draw(table);
        Assert.Equal(UIColors.BorderMedium, table.InnerCellBorderColor);
        Assert.Equal(UIColors.BorderMedium, table.OuterCellBorderColor);
        Assert.Equal(3, ColumnsOf(picture, UIColors.BorderMedium).Count);
    }

    [Fact]
    public void ALineThatIsNotShown_HasNoColor_TheOtherOnesKeepTheirs()
    {
        ThemedTableLayoutPanel table = Grid(3, 3, Color.White);
        table.InnerCellBorderColor = Color.Blue;
        table.OuterCellBorderColor = Color.Green;

        table.CellBorderLines = CellBorderLines.InnerOnly;
        Application.DoEvents();

        using (Bitmap picture = Draw(table))
        {
            Assert.Empty(ColumnsOf(picture, Color.Green));
            Assert.Empty(RowsOf(picture, Color.Green));
            Assert.Equal(2, ColumnsOf(picture, Color.Blue).Count);
        }

        table.CellBorderLines = CellBorderLines.OuterOnly;
        Application.DoEvents();

        using (Bitmap picture = Draw(table))
        {
            Assert.Empty(ColumnsOf(picture, Color.Blue));
            Assert.Equal(2, ColumnsOf(picture, Color.Green).Count);
        }
    }

    [Fact]
    public void CellBorderLines_WithOneColumnOrRow_HasNoInnerLinesInThatDirection()
    {
        ThemedTableLayoutPanel table = Grid(1, 3, Color.White);
        table.CellBorderLines = CellBorderLines.InnerOnly;
        Application.DoEvents();

        using Bitmap picture = Draw(table);
        Assert.Empty(RedColumns(picture));
        Assert.Equal(2, RedRows(picture).Count);
    }

    [Fact]
    public void CellBorderLines_OnATransparentTable_ShowWhatLiesBehindInPlaceOfTheHiddenLines()
    {
        ThemedTableLayoutPanel table = new ThemedTableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 2,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
            CellBorderColor = Color.Red,
            Size = new Size(100, 60),
            BackColor = Color.Transparent,
            CellBorderLines = CellBorderLines.InnerOnly
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, ClientSize = new Size(300, 200), BackColor = Color.Blue };
        table.Location = new Point(20, 20);
        host.Controls.Add(table);
        host.Show();
        Application.DoEvents();
        _forms.Add(host);

        using Bitmap picture = Draw(table);
        Assert.Equal(Color.Blue.ToArgb(), Opaque(picture.GetPixel(0, 30)).ToArgb());
        Assert.Equal(Color.Blue.ToArgb(), Opaque(picture.GetPixel(50, 0)).ToArgb());
        Assert.Equal(Color.Blue.ToArgb(), Opaque(picture.GetPixel(99, 59)).ToArgb());
        Assert.Single(RedColumns(picture));
    }

    [Theory]
    [InlineData(CellBorderLines.OuterOnly)]
    [InlineData(CellBorderLines.InnerOnly)]
    public void CellBorderLines_StayHidden_AfterAChangeOfTheTheme_AndWithoutTheSingleStyle_NothingIsTouched(CellBorderLines lines)
    {
        ThemedTableLayoutPanel table = Grid(3, 3, Color.White);
        table.CellBorderColor = Color.Empty;
        table.CellBorderLines = lines;
        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        using (Bitmap picture = Draw(table))
        {
            Color line = UIColors.BorderMedium;
            bool outerAt0 = Opaque(picture.GetPixel(0, 40)).ToArgb() == line.ToArgb();
            Assert.Equal(lines == CellBorderLines.OuterOnly, outerAt0);
        }

        table.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
        Application.DoEvents();

        using (Bitmap picture = Draw(table))
            Assert.Equal(Color.White.ToArgb(), Opaque(picture.GetPixel(0, 40)).ToArgb());
    }

    [Fact]
    public void WhatAPaintHandlerDraws_ComesOverTheLines_AlsoOverALineThatIsHidden()
    {
        ThemedTableLayoutPanel table = Grid(3, 3, Color.White);
        using Bitmap all = Draw(table);
        List<int> columns = RedColumns(all);
        List<int> rows = RedRows(all);

        table.CellBorderLines = CellBorderLines.InnerOnly;
        table.Paint += (sender, e) =>
        {
            // On the outer line (hidden), and on an inner one (shown).
            e.Graphics.FillRectangle(Brushes.Magenta, columns[0], rows[1] + 3, 1, 1);
            e.Graphics.FillRectangle(Brushes.Magenta, columns[1], rows[1] + 3, 1, 1);
        };
        table.Invalidate();
        Application.DoEvents();

        using Bitmap picture = Draw(table);
        Assert.Equal(Color.Magenta.ToArgb(), Opaque(picture.GetPixel(columns[0], rows[1] + 3)).ToArgb());
        Assert.Equal(Color.Magenta.ToArgb(), Opaque(picture.GetPixel(columns[1], rows[1] + 3)).ToArgb());
    }
}
