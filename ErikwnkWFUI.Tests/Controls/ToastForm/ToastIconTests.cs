using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>The symbol of a toast: none by default, a check mark, info, warning or error on request.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastIconTests : ToastTestBase
{
    private static Label IconLabel() => Current()!.GetPrivateField<Label>("_iconLabel")!;

    private static Panel ContentPanel() => Current()!.GetPrivateField<Panel>("_contentPanel")!;

    private static Label MessageLabel() => Current()!.GetPrivateField<Label>("_messageLabel")!;

    [Theory]
    [InlineData(ToastIcon.Success)]
    [InlineData(ToastIcon.Info)]
    [InlineData(ToastIcon.Warning)]
    [InlineData(ToastIcon.Error)]
    public void TheFontOfTheSymbol_IsOneThatCanStillBeUsed_ForEveryIcon(ToastIcon icon)
    {
        ToastForm.ShowToast("Message", Host(), icon: icon);

        // A font that was given back throws when it is used - and the label paints a red cross instead of the symbol.
        Assert.Null(Record.Exception(() => IconLabel().Font.Height));
    }

    [Fact]
    public void TheSameSymbolFontTwice_IsOneFont_NotAnEqualOneThatTheLabelWouldNotTake()
    {
        ToastForm.ShowToast("Message", Host(), icon: ToastIcon.Info);
        ToastForm toast = Current()!;
        Font first = toast.GetPrivateField<Font>("_iconFont")!;

        toast.CustomSymbol = ErikwnkWFUI.UIStyles.Symbols.Info;

        Assert.Same(first, toast.GetPrivateField<Font>("_iconFont"));
        Assert.Null(Record.Exception(() => IconLabel().Font.Height));
    }

    [Fact]
    public void TheSymbol_IsInThePictureOfTheWindow()
    {
        ToastForm.ShowToast("Message", Host(), icon: ToastIcon.Info);
        ToastForm toast = Current()!;
        toast.Transparency = PopupTransparency.Opaque;
        Rectangle area = IconLabel().Bounds;
        area.Offset(ContentPanel().Left, ContentPanel().Top);

        using Bitmap picture = toast.RenderLayer();
        Color background = picture.GetPixel(area.Left - 2, area.Top + area.Height / 2);
        int light = 0;

        for (int y = area.Top; y < area.Bottom; y++)
        {
            for (int x = area.Left; x < area.Right; x++)
            {
                Color pixel = picture.GetPixel(x, y);

                if (pixel.R > background.R + 100 && pixel.G > background.G + 100 && pixel.B > background.B + 100)
                    light++;
            }
        }

        // (Controls that lie over one another are printed in an order that can wipe out the one below: this is what it was.)
        Assert.True(light >= 10, "the symbol is not in the picture of the window: " + light);
    }

    [Fact]
    public void WithoutAnIcon_NoSymbolIsShown_AndTheTextUsesTheWidth()
    {
        ToastForm.ShowToast("Plain", Host());

        Assert.Equal(ToastIcon.None, Current()!.MessageIcon);
        Assert.False(IconLabel().Visible);
        Assert.Equal("", IconLabel().Text);
        Assert.True(ContentPanel().Padding.Left < 45, "room for a symbol that is not there");
    }

    [Theory]
    [InlineData(ToastIcon.Success)]
    [InlineData(ToastIcon.Info)]
    [InlineData(ToastIcon.Warning)]
    [InlineData(ToastIcon.Error)]
    public void AnIcon_ShowsItsSymbol_AndMakesRoomForIt(ToastIcon icon)
    {
        ToastForm.ShowToast("With a symbol", Host(), icon: icon);

        Assert.Equal(icon, Current()!.MessageIcon);
        Assert.True(IconLabel().Visible);
        Assert.False(string.IsNullOrEmpty(IconLabel().Text));
        Assert.True(ContentPanel().Padding.Left >= 45, "no room for the symbol");
        Assert.True(IconLabel().Right <= MessageLabel().Left, "the symbol overlaps the text");
    }

    [Fact]
    public void TheSymbol_IsInFrontOfTheTexts_SoTheirBackgroundDoesNotCoverIt()
    {
        ToastForm.ShowToast("Front", Host(), icon: ToastIcon.Success);

        Control parent = IconLabel().Parent!;

        Assert.True(parent.Controls.GetChildIndex(IconLabel()) < parent.Controls.GetChildIndex(MessageLabel()));
        Assert.True(parent.Controls.GetChildIndex(IconLabel()) < parent.Controls.GetChildIndex(Current()!.GetPrivateField<Label>("_titleLabel")!));
    }

    [Fact]
    public void TheFourSymbols_AreDifferent()
    {
        Form host = Host();
        List<string> glyphs = new List<string>();

        foreach (ToastIcon icon in new[] { ToastIcon.Success, ToastIcon.Info, ToastIcon.Warning, ToastIcon.Error })
        {
            ToastForm.ShowToast("x", host, icon: icon);
            glyphs.Add(IconLabel().Text);
        }

        Assert.Equal(4, glyphs.Distinct().Count());
    }

    [Fact]
    public void TheSymbolColor_FollowsTheTextColor()
    {
        ToastForm.ShowToast("Colored", Host(), ErikwnkWFUI.UIStyles.Colors.Sets.Yellow, icon: ToastIcon.Warning);

        Assert.Equal(PopupSupport.GetTextColor(ErikwnkWFUI.UIStyles.Colors.Sets.Yellow), IconLabel().ForeColor);
    }

    [Fact]
    public void ChangingTheSymbol_OfAToastThatIsUp_ShowsTheNewOne_AndHidesItForNone()
    {
        ToastForm.ShowToast("Up", Host(), icon: ToastIcon.Error);
        ToastForm toast = Current()!;
        string error = IconLabel().Text;

        toast.MessageIcon = ToastIcon.Success;
        Assert.NotEqual(error, IconLabel().Text);

        toast.MessageIcon = ToastIcon.None;
        Assert.False(IconLabel().Visible);
        Assert.True(ContentPanel().Padding.Left < 45, "room for a symbol that is not there");
    }

    [Fact]
    public void ASymbolOnADisposedToast_IsHarmless()
    {
        ToastForm toast = new ToastForm();
        toast.Dispose();

        Assert.Null(Record.Exception(() => toast.MessageIcon = ToastIcon.Warning));
    }

}
