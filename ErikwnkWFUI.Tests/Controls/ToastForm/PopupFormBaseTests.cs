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

    [Fact]
    public void BothPopups_HaveRoundedCorners_CutWithARegion_AndNoShadowOfTheSystem()
    {
        foreach (PopupFormBase popup in Both())
        {
            using (popup)
            {
                popup.Size = new Size(200, 60);
                _ = popup.Handle;

                Assert.NotNull(popup.Region);
                Assert.False(popup.Region!.IsVisible(new Point(0, 0)), "the corner is part of the window");
                Assert.False(popup.Region.IsVisible(new Point(199, 59)), "the corner is part of the window");
                Assert.True(popup.Region.IsVisible(new Point(100, 30)));
                Assert.True(popup.Region.IsVisible(new Point(20, 0)));

                // Windows 11 shades a window it rounds itself: it must not round this one (a missing attribute, before
                // Windows 11, is as good).
                if (DwmGetWindowAttribute(popup.Handle, 33, out int preference, sizeof(int)) == 0)
                    Assert.Equal(1, preference);
            }
        }
    }

    [Fact]
    public void BothPopups_HaveAThinLineAroundThem_InAColorOtherThanTheirBackground()
    {
        foreach (PopupFormBase popup in Both())
        {
            using (popup)
            {
                popup.Transparency = PopupTransparency.Opaque;
                popup.Size = new Size(200, 60);
                _ = popup.Handle;

                using Bitmap bitmap = new Bitmap(popup.Width, popup.Height);
                popup.DrawToBitmap(bitmap, new Rectangle(Point.Empty, popup.Size));

                Color edge = bitmap.GetPixel(100, 0);
                Color inside = bitmap.GetPixel(100, 30);

                Assert.NotEqual(inside.ToArgb(), edge.ToArgb());
                Assert.NotEqual(Color.Transparent.ToArgb(), edge.ToArgb());
            }
        }
    }

    [Fact]
    public void TheRegion_FollowsTheSizeOfThePopup()
    {
        foreach (PopupFormBase popup in Both())
        {
            using (popup)
            {
                popup.Size = new Size(200, 60);
                _ = popup.Handle;
                popup.Size = new Size(300, 90);

                Assert.False(popup.Region!.IsVisible(new Point(299, 89)));
                Assert.True(popup.Region.IsVisible(new Point(250, 45)));
                Assert.True(popup.Region.IsVisible(new Point(250, 85)));
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
