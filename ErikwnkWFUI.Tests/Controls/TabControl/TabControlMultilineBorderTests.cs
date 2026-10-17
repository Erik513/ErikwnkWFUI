using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// With several rows of tabs (Multiline) only the row next to the page ends on the page's own border; every tab of
/// another row is outlined on that side as well, so it does not float over other tabs or over nothing.
/// </summary>
public class TabControlMultilineBorderTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();
    }

    private ReadOnlyTabControl Make(TabAlignment alignment, bool multiline)
    {
        ReadOnlyTabControl tabs = new ReadOnlyTabControl { Alignment = alignment, Multiline = multiline, Bounds = new Rectangle(0, 0, 330, 220) };

        for (int i = 0; i < 9; i++)
            tabs.TabPages.Add(new TabPage("Tab number " + i));

        Form form = OffscreenForm.Create(new Size(330, 220));
        form.Controls.Add(tabs);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return tabs;
    }

    private static Bitmap Render(Control control)
    {
        Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));

        return bitmap;
    }

    [Fact]
    public void ATabOfAnotherRow_IsOutlinedOnTheSideTowardThePage_TopStrip()
    {
        ReadOnlyTabControl tabs = Make(TabAlignment.Top, true);
        Assert.True(tabs.RowCount > 1);

        using Bitmap bitmap = Render(tabs);
        int checkedTabs = 0;

        for (int i = 0; i < tabs.TabCount; i++)
        {
            Rectangle rect = tabs.GetTabRect(i);
            bool nextToPage = i == tabs.SelectedIndex || rect.Bottom >= tabs.DisplayRectangle.Top - 6;

            if (nextToPage)
                continue;

            checkedTabs++;
            Assert.Equal(tabs.BorderColor.ToArgb(), bitmap.GetPixel(rect.Left + rect.Width / 2, rect.Bottom - 1).ToArgb());
        }

        Assert.True(checkedTabs > 0);
    }
}
