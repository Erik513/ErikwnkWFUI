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
