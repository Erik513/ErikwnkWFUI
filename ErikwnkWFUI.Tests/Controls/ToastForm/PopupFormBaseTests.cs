using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>What the info popup and the toast share through <see cref="PopupFormBase"/>.</summary>
public class PopupFormBaseTests
{
    private static PopupFormBase[] Both() => new PopupFormBase[] { new InfoPopupForm(), new ToastForm() };

    [Fact]
    public void BothPopups_AreBorderlessOutOfTheTaskbarAndMediumTransparent()
    {
        foreach (PopupFormBase popup in Both())
        {
            using (popup)
            {
                Assert.Equal(FormBorderStyle.None, popup.FormBorderStyle);
                Assert.False(popup.ShowInTaskbar);
                Assert.Equal(FormStartPosition.Manual, popup.StartPosition);
                Assert.Equal(PopupTransparency.Medium, popup.Transparency);
                Assert.Equal(PopupTransparency.Medium.ToOpacity(), popup.Opacity);
            }
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr handle, int attribute, out int value, int size);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr handle, int index);

    private static Bitmap Layer(PopupFormBase popup, int width, int height)
    {
        popup.Transparency = PopupTransparency.Opaque;
        popup.Size = new Size(width, height);
        _ = popup.Handle;

        return popup.RenderLayer();
    }

    [Fact]
    public void BothPopups_HaveSmoothlyRoundedCorners_InTheirPicture()
    {
        foreach (PopupFormBase popup in Both())
        {
            using (popup)
            using (Bitmap layer = Layer(popup, 200, 60))
            {
                Assert.Equal(new Size(200, 60), layer.Size);
                Assert.Equal(0, layer.GetPixel(0, 0).A);
                Assert.Equal(0, layer.GetPixel(199, 59).A);
                Assert.Equal(255, layer.GetPixel(100, 30).A);
                Assert.Equal(255, layer.GetPixel(100, 0).A);

                // Along the curve of a corner there are pixels that are partly there: the edge is smoothed.
                int partly = 0;

                for (int y = 0; y < 14; y++)
                {
                    for (int x = 0; x < 14; x++)
                    {
                        int alpha = layer.GetPixel(x, y).A;

                        if (alpha > 8 && alpha < 247)
                            partly++;
                    }
                }

                Assert.True(partly >= 6, "the corner has no smoothed pixels: " + partly);
            }
        }
    }

    [Fact]
    public void BothPopups_HaveAThinLineAroundThem_InAColorOtherThanTheirBackground()
    {
        foreach (PopupFormBase popup in Both())
        {
            using (popup)
            using (Bitmap layer = Layer(popup, 200, 60))
            {
                Color edge = layer.GetPixel(100, 0);
                Color inside = layer.GetPixel(100, 30);

                Assert.NotEqual(inside.ToArgb(), edge.ToArgb());
                Assert.Equal(inside.ToArgb(), layer.GetPixel(100, 8).ToArgb());
            }
        }
    }

    [Fact]
    public void ThePicture_FollowsTheSizeOfThePopup()
    {
        foreach (PopupFormBase popup in Both())
        {
            using (popup)
            {
                using (Layer(popup, 200, 60))
                {
                }

                popup.Size = new Size(300, 90);

                using Bitmap layer = popup.RenderLayer();

                Assert.Equal(new Size(300, 90), layer.Size);
                Assert.Equal(0, layer.GetPixel(299, 89).A);
                Assert.Equal(255, layer.GetPixel(250, 85).A);
            }
        }
    }

    [Fact]
    public void BothPopups_AreLayeredWindows_WhoseOwnOpacityIsNotUsed_AndWindowsDoesNotRoundThem()
    {
        foreach (PopupFormBase popup in Both())
        {
            using (popup)
            {
                popup.Transparency = PopupTransparency.High;
                popup.Size = new Size(200, 60);
                _ = popup.Handle;

                // Layered, and the opacity of the form itself (which could not be used together with the picture) untouched.
                Assert.NotEqual(0, GetWindowLong(popup.Handle, -20) & 0x00080000);
                Assert.Equal(1.0, ((Form)popup).Opacity);
                Assert.Equal(PopupTransparency.High.ToOpacity(), popup.Opacity);

                // Windows 11 shades a window it rounds itself: it must not round this one (a missing attribute, before
                // Windows 11, is as good).
                if (DwmGetWindowAttribute(popup.Handle, 33, out int preference, sizeof(int)) == 0)
                    Assert.Equal(1, preference);
            }
        }
    }

    [Fact]
    public void TheTransparency_ChangesTheOpacity_AndTheBackgroundFollowsIt_OfBoth()
    {
        foreach (PopupFormBase popup in Both())
        {
            using (popup)
            {
                popup.Transparency = PopupTransparency.High;

                Assert.Equal(PopupTransparency.High.ToOpacity(), popup.Opacity);
                Assert.Equal(PopupSupport.GetBackground(ErikwnkWFUI.UIStyles.Colors.Sets.Gray, popup.Opacity), popup.BackColor);
            }
        }
    }

    [Fact]
    public void ATransparencyOnADisposedPopup_IsHarmless()
    {
        foreach (PopupFormBase popup in Both())
        {
            popup.Dispose();

            Assert.Null(Record.Exception(() => popup.Transparency = PopupTransparency.High));
        }
    }
}
