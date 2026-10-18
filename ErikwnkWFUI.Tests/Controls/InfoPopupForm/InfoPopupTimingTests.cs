using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>
/// The info popup: what a tooltip does, the delays and the time it stays.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class InfoPopupTimingTests : InfoPopupTestBase
{
    // ---- what a tooltip does ----

    [Fact]
    public void LineBreaksInTheText_AreKept_AndTheAmpersandIsShownAsItIs()
    {
        InfoPopupForm popup = CreatePopup();

        popup.SetContent("one line");
        int oneLine = popup.Height;
        popup.SetContent("first\nsecond\nthird");
        Assert.True(popup.Height > oneLine + 20, "the line breaks were not kept");

        // With a mnemonic "A&&B" would be as wide as "A&B"; shown as it is, it is wider.
        popup.SetContent("A&B");
        int single = popup.Width;
        popup.SetContent("A&&B");
        Assert.True(popup.Width > single, "the ampersand is treated as a mnemonic");
    }

    [Fact]
    public void ADisposedPopup_ShowsNothing_InsteadOfThrowing()
    {
        (Form _, Button anchor) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");
        popup.ShowNextTo(anchor);

        // Close disposes a form that was shown with an owner; asking again must not crash.
        popup.Close();

        Exception? thrown = Record.Exception(() =>
        {
            popup.ShowNextTo(anchor);
            popup.ShowAtMouse(anchor, new Point(100, 100));
            popup.ShowCenteredAbove(anchor, 10);
        });

        Assert.Null(thrown);
        Assert.True(popup.IsDisposed);
    }

    [Fact]
    public void AnOpacitySetDirectly_StaysAcrossShows()
    {
        (Form _, Button anchor) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");

        popup.Opacity = 0.5;
        popup.ShowNextTo(anchor);
        popup.ShowNextTo(anchor);

        Assert.Equal(0.5, popup.Opacity, 2);
    }

    [Fact]
    public void ContentThatGrowsWhileTheCopyIsUp_StillFitsInsideTheWindow()
    {
        (Form host, Button anchor) = CreateHost(new Point(100, 100), new Size(400, 300), new Point(310, 220));
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("short");
        popup.ShowAtMouse(anchor, host.PointToScreen(new Point(300, 200)));

        popup.SetContent("Info", "A text that grows a lot while the popup is shown, so that it needs much more room than before and would run over the edge of the window.");

        Rectangle area = Area(host);
        Assert.True(area.Contains(popup.Bounds), "outside the window: " + popup.Bounds + " in " + area);
    }

    // ---- the delay ----

    [Fact]
    public void ADelayedShow_ShowsTheContent_WhenTheTimerFires_NotBefore()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();

        popup.ShowAtMouseDelayed(owner, new Point(100, 100), Section("A", "b"));
        Assert.False(popup.Visible);
        Assert.Empty(Content(popup));

        popup.InvokePrivate("OnShowDelayTimerTick", popup, EventArgs.Empty);

        Assert.True(popup.Visible);
        Assert.Equal(new[] { "A|b" }, Content(popup));
    }

    [Fact]
    public void ACancelledDelayedShow_NeverShows_AndAnOwnerDisposedInTheMeantimeIsHarmless()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();

        popup.ShowAtMouseDelayed(owner, new Point(100, 100), Section("A", "b"));
        popup.CancelPendingShow();
        popup.InvokePrivate("OnShowDelayTimerTick", popup, EventArgs.Empty);
        Assert.False(popup.Visible);

        popup.ShowAtMouseDelayed(owner, new Point(100, 100), Section("A", "b"));
        owner.Dispose();
        Exception? thrown = Record.Exception(() => popup.InvokePrivate("OnShowDelayTimerTick", popup, EventArgs.Empty));

        Assert.Null(thrown);
        Assert.False(popup.Visible);
    }

    // ---- the wait before a hover popup shows ----

    [Fact]
    public void TheDelay_IsMediumByDefault_AndTheThreeLevelsAreOrdered()
    {
        InfoPopupForm popup = CreatePopup();

        Assert.Equal(PopupDelay.Medium, popup.Delay);
        Assert.Equal(400, WaitInterval(popup));

        popup.Delay = PopupDelay.Short;
        int shortWait = WaitInterval(popup);
        popup.Delay = PopupDelay.Long;
        int longWait = WaitInterval(popup);

        Assert.True(shortWait < 400 && 400 < longWait, "the levels are not ordered");
    }

    [Fact]
    public void TheFactoryMethods_TakeTheDelay()
    {
        InfoPopupForm standard = ErikwnkWFUI.UIStyles.InfoPopups.CreateStandard(delay: PopupDelay.Long);
        InfoPopupForm primary = ErikwnkWFUI.UIStyles.InfoPopups.CreatePrimary(delay: PopupDelay.Instant);
        _forms.Add(standard);
        _forms.Add(primary);

        Assert.Equal(PopupDelay.Long, standard.Delay);
        Assert.Equal(800, WaitInterval(standard));
        Assert.Equal(PopupDelay.Instant, primary.Delay);
    }

    [Fact]
    public void WithoutADelay_TheHoverPopupShowsAtOnce_AndNothingIsScheduled()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.Delay = PopupDelay.Instant;

        popup.ShowAtMouseDelayed(owner, new Point(100, 100), Section("A", "b"));

        Assert.True(popup.Visible);
        Assert.Equal(new[] { "A|b" }, Content(popup));
        Assert.False(popup.GetPrivateField<System.Windows.Forms.Timer>("_showDelayTimer")!.Enabled);
    }

    [Fact]
    public void WithADelay_TheHoverPopupWaits_AndTheTimerRuns()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();

        popup.ShowAtMouseDelayed(owner, new Point(100, 100), Section("A", "b"));

        Assert.False(popup.Visible);
        Assert.True(popup.GetPrivateField<System.Windows.Forms.Timer>("_showDelayTimer")!.Enabled);
    }

    // ---- the time it stays ----

    [Fact]
    public void TheDuration_IsMediumByDefault_AndTheThreeLevelsAreOrdered()
    {
        InfoPopupForm popup = CreatePopup();

        Assert.Equal(PopupDuration.Medium, popup.Duration);
        Assert.Equal(5000, Interval(popup));

        popup.Duration = PopupDuration.Short;
        int shortTime = Interval(popup);
        popup.Duration = PopupDuration.Long;
        int longTime = Interval(popup);

        Assert.True(shortTime < 5000 && 5000 < longTime, "the levels are not ordered");
    }

    [Fact]
    public void TheFactoryMethods_TakeTheDuration()
    {
        InfoPopupForm standard = ErikwnkWFUI.UIStyles.InfoPopups.CreateStandard(duration: PopupDuration.Short);
        InfoPopupForm primary = ErikwnkWFUI.UIStyles.InfoPopups.CreatePrimary(duration: PopupDuration.Long);
        _forms.Add(standard);
        _forms.Add(primary);

        Assert.Equal(PopupDuration.Short, standard.Duration);
        Assert.Equal(3000, Interval(standard));
        Assert.Equal(PopupDuration.Long, primary.Duration);
        Assert.Equal(8000, Interval(primary));
    }

    [Fact]
    public void TheTime_StartsWithEveryShow_AndStopsWhenThePopupIsHidden()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");

        Assert.False(TimeRuns(popup));

        popup.ShowNextTo(owner);
        Assert.True(TimeRuns(popup));

        popup.Hide();
        Assert.False(TimeRuns(popup));
    }

    [Fact]
    public void WhenTheTimeIsUp_ThePopupHidesItself()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");
        popup.ShowNextTo(owner);
        Assert.True(popup.Visible);

        popup.GetPrivateField<System.Windows.Forms.Timer>("_hideTimer")!.InvokePrivate("OnTick", EventArgs.Empty);

        Assert.False(popup.Visible);
    }

    [Fact]
    public void AClickOnThePopup_DoesNotActivateIt()
    {
        InfoPopupForm popup = CreatePopup();
        System.Reflection.PropertyInfo property = typeof(Form).GetProperty("CreateParams", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        CreateParams parameters = (CreateParams)property.GetValue(popup)!;

        Assert.NotEqual(0, parameters.ExStyle & 0x08000000);
    }

    [Fact]
    public void AHiddenPopup_CanBeShownAgain()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("one");

        popup.ShowNextTo(owner);
        popup.Hide();
        Assert.False(popup.Visible);

        popup.SetContent("two");
        popup.ShowNextTo(owner);
        Assert.True(popup.Visible);
        Assert.Equal(new[] { "-|two" }, Content(popup));
    }

    [Fact]
    public void AnApplicationsOwnBackColor_IsReplacedOnTheNextShow_TheColorsPropertyIsTheWay()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");
        Color expected = popup.BackColor;

        popup.BackColor = Color.HotPink;
        popup.ShowNextTo(owner);

        Assert.Equal(expected, popup.BackColor);
    }
}
