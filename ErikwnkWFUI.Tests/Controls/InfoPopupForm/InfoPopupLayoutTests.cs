using System.Drawing;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>The measured layout of the popup: made when the content changes, kept for every paint, made again for another scaling.</summary>
public class InfoPopupLayoutTests
{
    private static object Layout(InfoPopupForm popup) => popup.GetPrivateField<object>("_layout")!;

    private static void Paint(InfoPopupForm popup)
    {
        using Bitmap picture = new Bitmap(popup.Width, popup.Height);
        popup.DrawToBitmap(picture, new Rectangle(Point.Empty, popup.Size));
    }

    [Fact]
    public void TheLayout_IsKept_ForEveryPaint()
    {
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard();
        popup.SetContent("Header", "A text that is long enough to need a line or two when it is wrapped at the width of the popup.");

        object first = Layout(popup);
        Paint(popup);
        Paint(popup);

        Assert.Same(first, Layout(popup));
    }

    [Fact]
    public void NewContent_GivesANewLayout_AndANewSize()
    {
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard();
        popup.SetContent("Short");
        object first = Layout(popup);
        Size before = popup.Size;

        popup.SetContent("Header", "A much longer text that needs more room than the one word before, and wraps onto several lines in the popup.");

        Assert.NotSame(first, Layout(popup));
        Assert.True(popup.Height > before.Height);
    }

    [Fact]
    public void AnotherScaling_MeasuresAgain_WhenPainting()
    {
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard();
        popup.SetContent("Header", "Text");
        object first = Layout(popup);
        popup.SetPrivateField("_layoutDpi", 0);

        Paint(popup);

        Assert.NotSame(first, Layout(popup));
        Assert.Equal(popup.DeviceDpi, popup.GetPrivateField<int>("_layoutDpi"));
    }

    [Fact]
    public void WithoutContent_NothingIsLaidOut_AndPaintingIsSafe()
    {
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard();
        popup.SetContent("Text");
        popup.SetContent(new InfoPopupSection(null, null));

        Paint(popup);

        Assert.Empty((Array)Layout(popup));
    }

    [Fact]
    public void AFixedSize_StillLaysItsContentOut_ForThePaint()
    {
        using InfoPopupForm popup = UIStyles.InfoPopups.CreateStandard(fixedSize: new Size(70, 32));
        popup.SetContent("9%");

        Assert.Single((Array)Layout(popup));
        Assert.Equal(new Size(70, 32), popup.ClientSize);
    }
}
