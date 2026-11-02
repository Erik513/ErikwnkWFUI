using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTabControl = ErikwnkWFUI.Controls.ReadOnlyTabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>
/// The themed TabControl keeps the standard control's behavior and only
/// replaces the drawing, so these cover the theme defaults, the page
/// theming, the behavior that must stay standard, and - by drawing into a
/// bitmap and reading pixels back - that the colors actually end up where
/// they are meant to. [Collection] (see AccentColorTestCollection): the
/// defaults read UIColors, which UIColorsTests changes via SetAccent.
/// </summary>
public abstract class TabControlTestBase
{
    protected static WfuiTabControl CreateTabs(TabAlignment alignment = TabAlignment.Top, int pageCount = 3)
    {
        WfuiTabControl tabs = new WfuiTabControl
        {
            Size = new Size(300, 260),
            Alignment = alignment
        };

        for (int i = 0; i < pageCount; i++)
        {
            tabs.TabPages.Add("Tab " + i);
        }

        _ = tabs.Handle;
        return tabs;
    }

    protected static Bitmap Render(WfuiTabControl tabs)
    {
        Bitmap bitmap = new Bitmap(tabs.Width, tabs.Height);
        tabs.DrawToBitmap(bitmap, new Rectangle(Point.Empty, tabs.Size));
        return bitmap;
    }

    protected static Point Center(Rectangle rectangle)
    {
        return new Point(rectangle.Left + rectangle.Width / 2, rectangle.Top + rectangle.Height / 2);
    }

    protected static Color Pixel(Bitmap bitmap, int x, int y)
    {
        Color color = bitmap.GetPixel(x, y);
        return Color.FromArgb(color.R, color.G, color.B);
    }

    [DllImport("user32.dll")]
    protected static extern System.IntPtr SendMessage(System.IntPtr hWnd, int message, System.IntPtr wParam, System.IntPtr lParam);

    [DllImport("user32.dll")]
    protected static extern int GetWindowLong(System.IntPtr hWnd, int index);

    protected const int GWL_STYLE = -16;

    protected const int TCS_BUTTONS = 0x0100;

    protected sealed class DrawItemRecorder
    {
        public System.Collections.Generic.List<DrawItemEventArgs> Calls { get; } = new System.Collections.Generic.List<DrawItemEventArgs>();
    }

    public static System.Collections.Generic.IEnumerable<object[]> ColorProperties()
    {
        foreach (string name in new[]
        {
            "HeaderBackColor", "TabBackColor", "HoverTabBackColor", "SelectedTabBackColor", "TabForeColor",
            "SelectedTabForeColor", "DisabledTabForeColor", "SelectedTabIndicatorColor", "BorderColor", "PageBackColor"
        })
        {
            yield return new object[] { name };
        }
    }

    protected static int CountColored(Bitmap bitmap, Rectangle tab, bool reddish)
    {
        int count = 0;

        for (int x = tab.Left + 3; x < tab.Right - 3; x++)
        {
            for (int y = tab.Top + 4; y < tab.Bottom - 4; y++)
            {
                Color pixel = bitmap.GetPixel(x, y);
                bool isReddish = pixel.R > 200 && pixel.G < 50 && pixel.B < 50;
                bool isWhitish = pixel.R > 140 && pixel.G > 140 && pixel.B > 140;

                if (reddish ? isReddish : isWhitish)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
