using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>
/// The info popup: content, colors and size.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class InfoPopupBehaviorTests : InfoPopupTestBase
{
    // ---- content ----

    [Fact]
    public void TheContent_IsBlocksOfAHeaderAndATextEachPartOptional_EmptyPartsAreLeftOut()
    {
        InfoPopupForm popup = CreatePopup();

        popup.SetContent(Section("Only header", null), Section(null, "Only text"), Section("Both", "Together"), Section("  ", ""), Section(null, null), null!);

        Assert.Equal(new[] { "Only header|-", "-|Only text", "Both|Together" }, Content(popup));
    }

    [Fact]
    public void TheShortcuts_SetOneTextOrOneHeaderWithItsText()
    {
        InfoPopupForm popup = CreatePopup();

        popup.SetContent("Plain");
        Assert.Equal(new[] { "-|Plain" }, Content(popup));

        popup.SetContent("Title", "Body");
        Assert.Equal(new[] { "Title|Body" }, Content(popup));

        popup.SetContent();
        Assert.Empty(Content(popup));
    }

    [Fact]
    public void NewContent_ReplacesTheOldOne_AlsoOnAPopupThatIsShown()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();

        popup.SetContent("Header", "first");
        popup.ShowNextTo(owner);
        popup.SetContent("second");
        popup.ShowNextTo(owner);

        Assert.True(popup.Visible);
        Assert.Equal(new[] { "-|second" }, Content(popup));
    }

    [Fact]
    public void WithoutAnyText_NothingIsShown_AndAPopupThatIsUpGoesAway()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();

        popup.SetContent("  ");
        popup.ShowNextTo(owner);
        Assert.False(popup.Visible);

        popup.SetContent("text");
        popup.ShowNextTo(owner);
        Assert.True(popup.Visible);

        popup.SetContent(Section(null, ""));
        popup.ShowNextTo(owner);
        Assert.False(popup.Visible);
    }

    [Fact]
    public void TheContent_IsDrawn_AsInkOnTheBackground()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();

        popup.SetContent("Header", "A text under the header.");
        popup.ShowNextTo(owner);

        Assert.True(InkPixels(popup) > 50, "no text was drawn");
    }

    // ---- colors ----

    [Fact]
    public void HeaderAndText_AreWhiteByDefault_BlackOnALightBackground()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm dark = CreatePopup();
        InfoPopupForm light = ErikwnkWFUI.UIStyles.InfoPopups.CreateStandard(ErikwnkWFUI.UIStyles.Colors.CreateSet(Color.White));
        _forms.Add(light);

        dark.SetContent("Header", "A text under the header.");
        dark.ShowNextTo(owner);
        light.SetContent("Header", "A text under the header.");
        light.ShowNextTo(owner);

        Assert.True(Count(dark, Color.White) > 20, "no white text on the gray popup");
        Assert.Equal(0, Count(dark, Color.Black));
        Assert.True(Count(light, Color.Black) > 20, "no black text on the light popup");
    }

    [Fact]
    public void EveryHeaderAndEveryText_CanHaveItsOwnColor()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        Color red = Color.FromArgb(255, 0, 0);
        Color lime = Color.FromArgb(0, 255, 0);

        popup.SetContent(
            new InfoPopupSection("Red header", "A plain text") { HeaderColor = red },
            new InfoPopupSection("Plain header", "A lime text") { TextColor = lime });
        popup.ShowNextTo(owner);

        Assert.True(Count(popup, red) > 5, "the header is not red");
        Assert.True(Count(popup, lime) > 5, "the text is not lime");
        Assert.True(Count(popup, Color.White) > 20, "the parts left alone are not white");
    }

    [Fact]
    public void AColorChangedForTheSameWords_IsShown()
    {
        InfoPopupForm popup = CreatePopup();

        popup.SetContent("Header", "Text", Color.Red, default);
        Color before = popup.GetPrivateField<InfoPopupSection[]>("_sections")![0].HeaderColor;
        popup.SetContent("Header", "Text", Color.Blue, default);

        Assert.Equal(Color.Red, before);
        Assert.Equal(Color.Blue, popup.GetPrivateField<InfoPopupSection[]>("_sections")![0].HeaderColor);
    }

    // ---- size ----

    [Fact]
    public void TheSize_FollowsTheContent_AndALongTextWrapsAtTheMaximumWidth()
    {
        InfoPopupForm popup = CreatePopup();

        popup.SetContent("Short");
        int shortWidth = popup.Width;
        int shortHeight = popup.Height;

        popup.SetContent("A text that is much too long for one line of the popup, so that it has to wrap onto several lines and grow in height instead of in width.");
        int wrappedWidth = popup.Width;

        Assert.True(wrappedWidth > shortWidth);
        Assert.True(popup.Height >= shortHeight + 2 * 14, "the long text did not wrap into more lines");

        popup.SetContent("Word " + new string('x', 300));
        Assert.True(popup.Width <= wrappedWidth + 40, "a long word made the popup grow without end");
    }

    [Fact]
    public void AFixedSize_StaysWhateverTheContent_AndCentersIt()
    {
        InfoPopupForm popup = CreatePopup(new Size(70, 32));

        popup.SetContent("9%");
        Assert.Equal(new Size(70, 32), popup.ClientSize);

        popup.SetContent("100%");
        Assert.Equal(new Size(70, 32), popup.ClientSize);
        Assert.Equal(popup.Size, popup.MinimumSize);
        Assert.Equal(popup.Size, popup.MaximumSize);

        popup.FixedSize = Size.Empty;
        Assert.NotEqual(new Size(70, 32), popup.ClientSize);
    }

    [Fact]
    public void AFixedSizePopup_DrawsItsShortValue()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup(new Size(70, 32));

        popup.SetContent("70%");
        popup.ShowCenteredAbove(owner, 40);

        Assert.True(InkPixels(popup) > 20, "no value was drawn");
    }
}
