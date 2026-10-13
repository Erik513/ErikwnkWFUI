using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>
/// The features of the toast together and at their limits: everything a toast carries
/// (title, colors, symbol, position) through a delay, a thread change and a wait; odd titles;
/// scaling with a title; fonts that are given back.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastCombinationTests : ToastTestBase
{
    // A disposed GDI+ font has no native handle any more.
    private static bool IsDisposed(Font font)
    {
        FieldInfo handle = (typeof(Font).GetField("nativeFont", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? typeof(Font).GetField("_nativeFont", BindingFlags.NonPublic | BindingFlags.Instance))!;

        return (IntPtr)handle.GetValue(font)! == IntPtr.Zero;
    }

    private static readonly Color Red = Color.FromArgb(255, 110, 110);

    private static InfoPopupSection Everything() => new InfoPopupSection("The title", "The message") { HeaderColor = Red };

    private static void AssertEverything(Rectangle client)
    {
        ToastForm toast = Current()!;

        Assert.Equal("The title", Part("_titleLabel").Text);
        Assert.Equal("The message", Part("_messageLabel").Text);
        Assert.Equal(Red, Part("_titleLabel").ForeColor);
        Assert.Equal(ErikwnkWFUI.UIStyles.Symbols.Copy, Part("_iconLabel").Text);
        Assert.Equal(PopupTransparency.High, toast.Transparency);
        Assert.Equal(PopupDuration.Long, toast.Duration);
        Assert.Equal(ErikwnkWFUI.UIStyles.Colors.Sets.Green.Basic, toast.Colors!.Basic);
        Assert.True(toast.Left < client.Left + client.Width / 4 && toast.Top < client.Top + client.Height / 4, "not at the top left: " + toast.Location);
    }

    private static void ShowEverything(Form host, PopupDelay delay = PopupDelay.Instant)
    {
        ToastForm.ShowToast(Everything(), host, ErikwnkWFUI.UIStyles.Colors.Sets.Green, PopupTransparency.High, PopupDuration.Long, delay,
            ToastIcon.Warning, ErikwnkWFUI.UIStyles.Symbols.Copy, ToastPosition.TopLeft);
    }

    [Fact]
    public void AToastThatIsDelayed_KeepsEverythingItCarries()
    {
        Form host = Host();

        ShowEverything(host, PopupDelay.Short);
        Assert.Null(Current());

        Scheduled()[0].InvokePrivate("OnTick", EventArgs.Empty);

        AssertEverything(host.RectangleToScreen(host.ClientRectangle));
    }

    [Fact]
    public void AToastThatWaitsForItsWindow_KeepsEverythingItCarries()
    {
        Form host = Host();
        PopupSupport.OwnerIsActive = _ => false;

        ShowEverything(host);
        Assert.Null(Current());

        PopupSupport.OwnerIsActive = _ => true;
        typeof(ToastForm).GetMethod("OnOwnerActivated", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { host, EventArgs.Empty });

        AssertEverything(host.RectangleToScreen(host.ClientRectangle));
    }

    [Fact]
    public void AToastAskedForFromAnotherThread_KeepsEverythingItCarries()
    {
        Form host = Host();
        Exception? thrown = null;

        Thread worker = new Thread(() =>
        {
            try
            {
                ShowEverything(host);
            }
            catch (Exception exception)
            {
                thrown = exception;
            }
        });
        worker.Start();
        worker.Join();

        for (int i = 0; i < 20 && Current() == null; i++)
        {
            Application.DoEvents();
            Thread.Sleep(30);
        }

        Assert.Null(thrown);
        Assert.NotNull(Current());
        AssertEverything(host.RectangleToScreen(host.ClientRectangle));
    }

    [Fact]
    public void ATitleOfOnlySpaces_IsNotShown_TheMessageIs()
    {
        ToastForm.ShowToast(new InfoPopupSection("   ", "Message only"), Host());

        Assert.False(Part("_titleLabel").Visible);
        Assert.Equal("Message only", Part("_messageLabel").Text);
        Assert.Equal(80, Current()!.Height * 96 / Current()!.DeviceDpi);
    }

    [Fact]
    public void AMessageOfOnlySpaces_WithATitle_ShowsTheTitleAlone()
    {
        ToastForm.ShowToast(new InfoPopupSection("Title", "   "), Host());

        Assert.True(Part("_titleLabel").Visible);
        Assert.Equal("", Part("_messageLabel").Text);
    }

    [Fact]
    public void AVeryLongTitle_StaysOneLine_AndTheToastDoesNotGrowForIt()
    {
        Form host = Host();

        ToastForm.ShowToast(new InfoPopupSection("T", "Message"), host);
        int normal = Current()!.Height;
        ToastForm.ShowToast(new InfoPopupSection(string.Join(" ", Enumerable.Repeat("a very long title", 30)), "Message"), host);

        Assert.Equal(normal, Current()!.Height);
        Assert.True(Part("_titleLabel").AutoEllipsis);
        Assert.True(Part("_titleLabel").Height < 2 * TextRenderer.MeasureText("Ag", Part("_titleLabel").Font).Height);
    }

    [Fact]
    public void ATitleWithAFullMessage_FitsTheLimitOfLines()
    {
        Form host = Host();
        string many = string.Join(" ", Enumerable.Repeat("a long message of many words", 60));

        ToastForm.ShowToast(new InfoPopupSection("Title", many), host);
        ToastForm withTitle = Current()!;
        int titled = withTitle.Height;
        ToastForm.ShowToast(many, host);

        Assert.True(titled > Current()!.Height, "the title took no room");
        Assert.True(Part("_messageLabel").Bottom <= titled);
        Assert.True(Rectangle.Intersect(host.Bounds, Screen.FromControl(host).WorkingArea).Contains(withTitle.Bounds) || withTitle.IsDisposed);
    }

    [Fact]
    public void AToastOnAFinerScreen_WithATitle_IsTallerAndStillFits()
    {
        Form host = Host();

        ToastForm.OwnerDpi = _ => 96;
        ToastForm.ShowToast(new InfoPopupSection("Title", "Message"), host, icon: ToastIcon.Info);
        Size small = Current()!.Size;

        ToastForm.OwnerDpi = _ => 192;
        ToastForm.ShowToast(new InfoPopupSection("Title", "Message"), host, icon: ToastIcon.Info);
        Size large = Current()!.Size;

        Assert.True(large.Width == small.Width * 2, "the width does not double: " + small + " -> " + large);
        Assert.True(large.Height >= small.Height * 19 / 10, "the height does not double: " + small + " -> " + large);
        Assert.True(Part("_titleLabel").Bottom <= Part("_messageLabel").Top);
    }

    [Fact]
    public void ReplacingAToast_GivesBackTheFontOfItsSymbol()
    {
        Form host = Host();
        ToastForm.ShowToast("One", host, icon: ToastIcon.Success);
        ToastForm first = Current()!;
        Font font = first.GetPrivateField<Font>("_iconFont")!;

        ToastForm.ShowToast("Two", host, icon: ToastIcon.Success);

        Assert.True(first.IsDisposed);
        Assert.Null(first.GetPrivateField<Font>("_iconFont"));
        Assert.True(IsDisposed(font), "the font of the replaced toast was not given back");
    }

    [Fact]
    public void ChangingTheSymbolOfAToastThatIsUp_GivesBackTheOldFont()
    {
        ToastForm.ShowToast("Up", Host(), icon: ToastIcon.Success);
        ToastForm toast = Current()!;
        Font old = toast.GetPrivateField<Font>("_iconFont")!;

        toast.Icon = ToastIcon.Warning;

        Assert.NotSame(old, toast.GetPrivateField<Font>("_iconFont"));
        Assert.True(IsDisposed(old), "the old font was not given back");
    }

    [Fact]
    public void WithoutASymbol_TheLabelHasTheSharedFontAgain_NotOneThatWasGivenBack()
    {
        ToastForm.ShowToast("Up", Host(), icon: ToastIcon.Warning);
        ToastForm toast = Current()!;
        Font used = toast.GetPrivateField<Font>("_iconFont")!;

        toast.Icon = ToastIcon.None;

        Assert.True(IsDisposed(used));
        Assert.Same(ErikwnkWFUI.Styles.UIFonts.Icon, Part("_iconLabel").Font);
        Assert.Null(toast.GetPrivateField<Font>("_iconFont"));
        Assert.False(IsDisposed(Part("_iconLabel").Font));
    }

    [Fact]
    public void AClickOnAToastThatWasReplaced_IsHarmless_AndDoesNotCloseTheNewOne()
    {
        Form host = Host();
        ToastForm.ShowToast("Old", host);
        ToastForm old = Current()!;
        ToastForm.ShowToast("New", host);
        ToastForm current = Current()!;

        Exception? thrown = Record.Exception(() => old.InvokePrivate("OnToastClick", old, EventArgs.Empty));

        Assert.Null(thrown);
        Assert.Same(current, Current());
        Assert.False(current.IsDisposed);
    }

    [Fact]
    public void WaitingToasts_AtOtherPlaces_AreAllKept_UpToTheLimit()
    {
        Form host = Host();
        PopupSupport.OwnerIsActive = _ => false;

        foreach (ToastPosition position in new[] { ToastPosition.TopLeft, ToastPosition.TopRight, ToastPosition.BottomLeft, ToastPosition.BottomRight })
            ToastForm.ShowToast("Same", host, position: position);

        var queue = (System.Collections.IEnumerable)((System.Collections.IDictionary)typeof(ToastForm)
            .GetField("_pending", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!)[host]!;

        Assert.Equal(ToastForm.MaxPendingToasts, queue.Cast<object>().Count());
    }

    // Two toasts in a row that differ in one thing are two; in nothing, one.
    [Theory]
    [InlineData("title")]
    [InlineData("message")]
    [InlineData("color of the title")]
    [InlineData("icon")]
    [InlineData("symbol")]
    [InlineData("position")]
    public void WaitingToasts_AreOneOnlyWhenTheyShowTheSame(string difference)
    {
        Form host = Host();
        PopupSupport.OwnerIsActive = _ => false;

        void Show(bool other)
        {
            InfoPopupSection content = new InfoPopupSection(
                difference == "title" && other ? "Other" : "Title",
                difference == "message" && other ? "Other" : "Message");

            if (difference == "color of the title" && other)
                content.HeaderColor = Red;

            ToastForm.ShowToast(
                content,
                host,
                icon: difference == "icon" && other ? ToastIcon.Error : ToastIcon.Warning,
                symbol: difference == "symbol" && other ? ErikwnkWFUI.UIStyles.Symbols.Delete : ErikwnkWFUI.UIStyles.Symbols.Copy,
                position: difference == "position" && other ? ToastPosition.TopLeft : ToastPosition.BottomRight);
        }

        Show(false);
        Show(false);
        Assert.Equal(1, WaitingCount(host));

        Show(true);
        Show(true);
        Assert.Equal(2, WaitingCount(host));
    }

    [Fact]
    public void ADelayedToast_WhoseWindowWasDisposedMeanwhile_IsHarmless()
    {
        Form host = Host();
        ShowEverything(host, PopupDelay.Short);
        host.Dispose();

        Exception? thrown = Record.Exception(() => Scheduled()[0].InvokePrivate("OnTick", EventArgs.Empty));

        Assert.Null(thrown);
        Assert.Null(Current());
    }

    [Fact]
    public void ALineBreakInTheMessage_MakesAnotherLine()
    {
        Form host = Host();

        ToastForm.ShowToast("One line", host);
        int one = Current()!.Height;
        ToastForm.ShowToast("Line one\nLine two\nLine three\nLine four", host);

        Assert.True(Current()!.Height > one, "line breaks did not make the toast higher");
    }
}
