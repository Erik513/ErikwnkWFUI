using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTabControl = ErikwnkWFUI.Controls.TabControl;

namespace ErikwnkWFUI.Tests.Controls.TabControl;

/// <summary>What the library draws itself (the bar, the padlock and its room, the arrows) grows with the scaling of the screen.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TabControlScalingTests
{
    private static WfuiTabControl CreateTabs(int dpi)
    {
        WfuiTabControl tabs = new WfuiTabControl { Size = new Size(400, 200), DpiOverride = dpi };
        tabs.TabPages.Add("Home");
        tabs.TabPages.Add("Reports");
        _ = tabs.Handle;
        return tabs;
    }

    [Theory]
    [InlineData(96, 7, 14)]
    [InlineData(144, 11, 21)]
    [InlineData(192, 14, 28)]
    public void TheRoomForThePadlock_HasTheSizeOfTheScreen(int dpi, int width, int height)
    {
        using WfuiTabControl tabs = CreateTabs(dpi);

        tabs.SetTabClosable(tabs.TabPages[1], false);

        ImageList slots = tabs.GetPrivateField<ImageList>("_lockSlots")!;
        Assert.Equal(new Size(width, height), slots.ImageSize);
    }

    [Fact]
    public void ThePadlock_IsDrawnInTheSizeOfItsSlot()
    {
        int Height(Rectangle slot)
        {
            using Bitmap bitmap = new Bitmap(60, 60);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Black);
                typeof(WfuiTabControl).GetMethod("DrawPadlock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .Invoke(null, new object[] { graphics, slot, Color.White });
            }

            int top = bitmap.Height, bottom = -1;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).R > 40)
                    {
                        top = Math.Min(top, y);
                        bottom = Math.Max(bottom, y);
                    }
                }
            }

            return bottom - top + 1;
        }

        int normal = Height(new Rectangle(10, 10, 7, 14));
        int doubled = Height(new Rectangle(10, 10, 14, 28));

        Assert.InRange(doubled, normal * 2 - 3, normal * 2 + 3);
    }

    [Fact]
    public void TheBarOfTheSelectedTab_IsThickerOnAFinerScreen()
    {
        int Thickness(int dpi)
        {
            using WfuiTabControl tabs = CreateTabs(dpi);
            tabs.SelectedTabIndicatorColor = Color.Magenta;
            Rectangle tab = tabs.GetTabRect(0);
            using Bitmap bitmap = new Bitmap(tabs.Width, tabs.Height);
            tabs.DrawToBitmap(bitmap, new Rectangle(Point.Empty, tabs.Size));
            int x = tab.Left + tab.Width / 2;
            int rows = 0;

            for (int y = tab.Top; y < tab.Top + 12; y++)
            {
                Color pixel = bitmap.GetPixel(x, y);

                if (pixel.R == 255 && pixel.G == 0 && pixel.B == 255)
                    rows++;
            }

            return rows;
        }

        Assert.Equal(2, Thickness(96));
        Assert.Equal(4, Thickness(192));
    }
}
