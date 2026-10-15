using System.Drawing;
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

    private static bool GetShowBorder(Control panel) => (bool)panel.GetType().GetProperty("ShowBorder")!.GetValue(panel)!;

    private static void SetShowBorder(Control panel, bool value) => panel.GetType().GetProperty("ShowBorder")!.SetValue(panel, value);

    private static BorderStyle GetBorderStyle(Control panel) => (BorderStyle)panel.GetType().GetProperty("BorderStyle")!.GetValue(panel)!;

    [Theory]
    [MemberData(nameof(ThreePanels))]
    public void ShowBorder_IsOffByDefault_AndSwitchesTheFrameOfTheTheme(Func<Control> make)
    {
        Control panel = make();
        Assert.False(GetShowBorder(panel));
        Assert.Equal(BorderStyle.None, GetBorderStyle(panel));

        panel.Size = new Size(100, 60);
        panel.BackColor = Color.Black;
        Show(panel);

        using (Bitmap picture = Draw(panel))
            Assert.Equal(Color.Black.ToArgb(), Opaque(picture.GetPixel(0, 30)).ToArgb());

        SetShowBorder(panel, true);
        Application.DoEvents();

        Assert.True(GetShowBorder(panel));
        Assert.Equal(BorderStyle.FixedSingle, GetBorderStyle(panel));

        using (Bitmap picture = Draw(panel))
            Assert.Equal(UIColors.BorderMedium, Opaque(picture.GetPixel(0, 30)));

        SetShowBorder(panel, false);
        Application.DoEvents();

        Assert.Equal(BorderStyle.None, GetBorderStyle(panel));

        using (Bitmap picture = Draw(panel))
            Assert.Equal(Color.Black.ToArgb(), Opaque(picture.GetPixel(0, 30)).ToArgb());
    }

    [Fact]
    public void ShowBorder_LeavesAFrameThatWasSetByHandAlone()
    {
        ThemedPanel panel = new ThemedPanel { BorderStyle = BorderStyle.Fixed3D };
        Assert.True(panel.ShowBorder);

        panel.ShowBorder = true;

        Assert.Equal(BorderStyle.Fixed3D, panel.BorderStyle);
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
}
