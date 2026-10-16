using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls;

/// <summary>
/// The parts of a list or a grid that the system draws - scroll bars, the header strip behind the
/// column titles, the square where two scroll bars meet - must not be light blocks on the dark
/// theme. (Only the square is painted by the library and can be measured here: the test process
/// has no visual styles, so the dark scroll bars and header strip, which come from the system's
/// dark visual style, are checked on screen, not in a test.) [Collection]: the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class NativePartsThemeTests : IDisposable
{
    private readonly List<Form> _hosts = new List<Form>();

    public void Dispose()
    {
        foreach (Form host in _hosts)
            host.Dispose();
    }

    private void Show(Control control)
    {
        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-4000, -4000),
            ShowInTaskbar = false,
            ClientSize = new Size(420, 260),
            BackColor = UIColors.BackgroundLight
        };

        control.Location = new Point(10, 10);
        host.Controls.Add(control);
        host.Show();
        _hosts.Add(host);

        for (int i = 0; i < 4; i++)
        {
            Application.DoEvents();
            Thread.Sleep(40);
        }
    }

    private static double Brightness(Color c) => (c.R + c.G + c.B) / 3.0;

    [Fact]
    public void TheTheme_IsDarkWhenTheBackgroundIsDark_AndTheHelperToleratesNoWindow()
    {
        Assert.True(NativeControlTheme.IsDark);

        try
        {
            UIColors.ApplyTheme(UIThemes.Light);
            Assert.False(NativeControlTheme.IsDark);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Exception? thrown = Record.Exception(() =>
        {
            NativeControlTheme.ApplyToScrollBars(IntPtr.Zero);
            NativeControlTheme.ApplyToItemsView(IntPtr.Zero);
        });

        Assert.Null(thrown);
    }

    [Fact]
    public void AGridWithBothScrollBars_HasNoLightSquareWhereTheyMeet_AndItsBarsAreFound()
    {
        System.Windows.Forms.DataGridView grid = UIStyles.DataGridViews.CreateStandard();
        grid.Size = new Size(360, 150);
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "One", Width = 150 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Two", Width = 600 });

        for (int i = 0; i < 20; i++)
            grid.Rows.Add("Row " + i, "x");

        Show(grid);

        Control[] found = ((ErikwnkWFUI.Controls.ReadOnlyDataGridView)grid).GetPrivateField<Control[]>("_scrollBars")!;
        Assert.Equal(2, found.Length);

        Control horizontal = found.First(bar => bar is HScrollBar);
        Control vertical = found.First(bar => bar is VScrollBar);
        Assert.True(horizontal.Visible && vertical.Visible, "both scroll bars should be showing");

        using Bitmap picture = Draw(grid);
        Color corner = picture.GetPixel(vertical.Left + vertical.Width / 2, horizontal.Top + horizontal.Height / 2);

        Assert.Equal(grid.BackgroundColor, corner);
    }
}
