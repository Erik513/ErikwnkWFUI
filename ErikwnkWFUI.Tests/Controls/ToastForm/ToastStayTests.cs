using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>How long a toast stays: the time of its level, longer for a long text, and until it is clicked.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastStayTests : ToastTestBase
{
    private static string Words(int count)
    {
        return string.Join(" ", Enumerable.Repeat("word", count));
    }

    private static int Interval(ToastForm toast) => toast.GetPrivateField<System.Windows.Forms.Timer>("_closeTimer")!.Interval;

    private static bool TimerRuns(ToastForm toast) => toast.GetPrivateField<System.Windows.Forms.Timer>("_closeTimer")!.Enabled;

    [Theory]
    [InlineData(PopupDuration.Short, 1500)]
    [InlineData(PopupDuration.Medium, 2500)]
    [InlineData(PopupDuration.Long, 4000)]
    public void AShortMessage_KeepsTheTimeOfItsLevel(PopupDuration duration, int milliseconds)
    {
        ToastForm.ShowToast(Words(3), Host(), duration: duration);

        Assert.Equal(milliseconds, Interval(Current()!));
        Assert.True(TimerRuns(Current()!));
    }

    [Theory]
    [InlineData(PopupDuration.Short, 15, 4500)]
    [InlineData(PopupDuration.Medium, 15, 4500)]
    [InlineData(PopupDuration.Long, 15, 4500)]
    [InlineData(PopupDuration.Medium, 20, 6000)]
    [InlineData(PopupDuration.Medium, 100, 8000)]
    [InlineData(PopupDuration.Long, 500, 8000)]
    public void ALongMessage_StaysLonger_AboutAThirdOfASecondAWord_ButNotMoreThanEightSeconds(PopupDuration duration, int words, int milliseconds)
    {
        ToastForm.ShowToast(Words(words), Host(), duration: duration);

        Assert.Equal(milliseconds, Interval(Current()!));
    }

    [Fact]
    public void TheTitleIsReadToo_AndTheLevelIsTheLeastTime()
    {
        ToastForm.ShowToast(new InfoPopupSection(Words(10), Words(10)), Host(), duration: PopupDuration.Medium);
        Assert.Equal(6000, Interval(Current()!));
        Current()!.Dispose();

        // Eight words are 2.4 seconds: the level (2.5) is longer.
        ToastForm.ShowToast(Words(8), Host(), duration: PopupDuration.Medium);
        Assert.Equal(2500, Interval(Current()!));
    }

    [Fact]
    public void TheGrowthCanBeSwitchedOff_OnTheToast_OnTheToaster_AndItIsOnByDefault()
    {
        Assert.True(new Toaster().DurationGrowsWithText);
        Assert.True(UIStyles.Toasts.CreateStandard().DurationGrowsWithText);

        ToastForm.ShowToast(Words(40), Host(), durationGrowsWithText: false);
        Assert.Equal(2500, Interval(Current()!));
        Current()!.Dispose();

        Toaster toaster = UIStyles.Toasts.CreateStandard();
        toaster.DurationGrowsWithText = false;
        toaster.Show(Words(40), Host());
        Assert.Equal(2500, Interval(Current()!));
        Current()!.Dispose();

        toaster.DurationGrowsWithText = true;
        toaster.Show(Words(40), Host());
        Assert.Equal(8000, Interval(Current()!));
    }

    [Fact]
    public void UntilClicked_HasNoTime_ShowsACross_AndAClickClosesIt()
    {
        ToastForm.ShowToast(Words(60), Host(), duration: PopupDuration.UntilClicked);
        ToastForm toast = Current()!;

        Assert.False(TimerRuns(toast));
        Assert.True(toast.GetPrivateField<Label>("_closeLabel")!.Visible);

        toast.InvokePrivate("OnToastClick", toast, EventArgs.Empty);

        Assert.True(toast.IsDisposed);
        Assert.Null(Current());
    }

    [Theory]
    [InlineData("_messageLabel")]
    [InlineData("_titleLabel")]
    [InlineData("_closeLabel")]
    [InlineData("_iconLabel")]
    public void AClickAnywhereOnTheToast_ClosesIt_ThePartsAreNotDifferent_TheCrossIsOnlyAHint(string part)
    {
        ToastForm.ShowToast(new InfoPopupSection("Title", "A message that stays"), Host(), duration: PopupDuration.UntilClicked, icon: ToastIcon.Info);
        ToastForm toast = Current()!;

        Part(part).InvokePrivate("OnClick", EventArgs.Empty);

        Assert.True(toast.IsDisposed, "a click on " + part + " did not close the toast");
    }
    [Fact]
    public void AToastThatIsNotSticky_HasNoCross_AndTheTextHasItsFullWidth()
    {
        ToastForm.ShowToast("A message", Host());
        Label plain = Part("_messageLabel");
        int plainPadding = plain.Padding.Right;
        Assert.False(Part("_closeLabel").Visible);
        Current()!.Dispose();

        ToastForm.ShowToast("A message", Host(), duration: PopupDuration.UntilClicked);

        Assert.True(Part("_messageLabel").Padding.Right > plainPadding, "the text leaves room for the cross");
        Assert.True(Part("_titleLabel").Padding.Right > plainPadding);
    }

    [Fact]
    public void TheCross_IsInTheUpperRightCorner_AndTheTextDoesNotReachIt()
    {
        ToastForm.ShowToast(new InfoPopupSection("Title", Words(40)), Host(), duration: PopupDuration.UntilClicked);
        ToastForm toast = Current()!;
        Label cross = Part("_closeLabel");
        Label message = Part("_messageLabel");

        Assert.True(cross.Right <= toast.ClientSize.Width);
        Assert.True(cross.Top >= 0 && cross.Top < toast.ClientSize.Height / 2, "the cross is in the upper half");
        Assert.True(cross.Left > toast.ClientSize.Width / 2, "the cross is on the right");

        // The room of the text ends before the cross (the label of the message is inside the padding of the panel).
        int textRight = message.Right - message.Padding.Right;
        Assert.True(textRight <= cross.Left + 1, "the text reaches under the cross: text " + textRight + ", cross " + cross.Left);
    }

    [Fact]
    public void ANewerToast_ReplacesOneThatStaysUntilClicked_AlsoTheWayItsWindowIsAsked()
    {
        Form host = Host();
        ToastForm.ShowToast("First", host, duration: PopupDuration.UntilClicked);
        ToastForm first = Current()!;

        ToastForm.ShowToast("Second", host);

        Assert.True(first.IsDisposed, "the sticky toast is gone");
        Assert.NotSame(first, Current());
        Assert.True(TimerRuns(Current()!), "the new one has its time");

        UIStyles.Toasts.ShowStandard("Third", host, duration: PopupDuration.UntilClicked);
        UIStyles.Toasts.ShowPrimary("Fourth", host);

        Assert.Equal("Fourth", Part("_messageLabel").Text);
    }

    [Fact]
    public void UntilClicked_CanBeGivenToTheToasterAndTheFactories()
    {
        Toaster toaster = UIStyles.Toasts.CreateStandard(duration: PopupDuration.UntilClicked);
        toaster.Show("From the toaster", Host());

        Assert.False(TimerRuns(Current()!));
        Current()!.Dispose();

        UIStyles.Toasts.ShowPrimary("From the factory", Host(), duration: PopupDuration.UntilClicked);
        Assert.False(TimerRuns(Current()!));
    }

    [Fact]
    public void AnInfoPopup_TreatsUntilClickedAsLong()
    {
        Assert.Equal(PopupDuration.Long.ToInfoMilliseconds(), PopupDuration.UntilClicked.ToInfoMilliseconds());
        Assert.Equal(8000, PopupDuration.UntilClicked.ToInfoMilliseconds());
    }
}