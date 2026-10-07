using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>
/// What the info popup does with its content and its window: the blocks and what is left out,
/// the size that follows the content or stays fixed, the three places, the show delay, a
/// disposed owner, the position on the screen. Popups are shown for real (briefly) next to a
/// host form. [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class InfoPopupBehaviorTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();
    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;

    // The popups are shown only while the application is in front; a test must not depend on
    // what is in front on the machine it runs on.
    public InfoPopupBehaviorTests()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;

        foreach (Form form in _forms)
            form.Dispose();
    }

    private (Form Host, Button Owner) CreateHost(Point? location = null, Size? clientSize = null, Point? ownerLocation = null)
    {
        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = location ?? new Point(-4000, -4000),
            ShowInTaskbar = false,
            ClientSize = clientSize ?? new Size(300, 200)
        };
        Button owner = new Button { Location = ownerLocation ?? new Point(20, 60), Size = new Size(80, 30) };

        host.Controls.Add(owner);
        host.Show();
        _forms.Add(host);

        return (host, owner);
    }

    private InfoPopupForm CreatePopup(Size? fixedSize = null)
    {
        InfoPopupForm popup = ErikwnkWFUI.UIStyles.InfoPopups.CreateStandard(fixedSize: fixedSize);
        _forms.Add(popup);

        return popup;
    }

    private static InfoPopupSection Section(string? header, string? text) => new InfoPopupSection(header!, text!);

    // What the popup holds now: header and text of each block ("-" for a missing part).
    private static string[] Content(InfoPopupForm popup)
    {
        InfoPopupSection[] sections = popup.GetPrivateField<InfoPopupSection[]>("_sections")!;

        return sections.Select(section => (section.Header ?? "-") + "|" + (section.Text ?? "-")).ToArray();
    }

    // The number of pixels that are not the background: the text that was drawn.
    private static int InkPixels(Form form)
    {
        using Bitmap bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        int ink = 0;

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) != form.BackColor)
                    ink++;
            }
        }

        return ink;
    }

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

    private static int Count(Form form, Color color)
    {
        using Bitmap bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        form.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        int count = 0;

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() == color.ToArgb())
                    count++;
            }
        }

        return count;
    }

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

    // ---- the places ----

    [Fact]
    public void TheThreePlaces_ShowThePopup_AtTheirOwnPosition()
    {
        (Form _, Button owner) = CreateHost(new Point(300, 300), new Size(400, 300), new Point(20, 150));
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("Info", "text");

        popup.ShowNextTo(owner);
        Point next = popup.Location;
        Assert.True(popup.Visible);
        Assert.True(next.X >= owner.PointToScreen(Point.Empty).X + owner.Width, "not to the right of the owner");

        Point mouse = new Point(owner.PointToScreen(Point.Empty).X + 10, owner.PointToScreen(Point.Empty).Y + 10);
        popup.ShowAtMouse(owner, mouse);
        Assert.NotEqual(next, popup.Location);

        popup.ShowCenteredAbove(owner, 40);
        int centerX = popup.Location.X + popup.Width / 2;
        Assert.InRange(centerX - owner.PointToScreen(new Point(40, 0)).X, -2, 2);
        Assert.True(popup.Location.Y + popup.Height <= owner.PointToScreen(Point.Empty).Y);
    }

    [Fact]
    public void AnOwnerAtTheEdgeOfTheScreen_GetsAPopupThatStaysOnTheScreen()
    {
        Rectangle area = Screen.PrimaryScreen!.WorkingArea;
        (Form _, Button owner) = CreateHost(new Point(area.Right - 120, area.Bottom - 120));
        InfoPopupForm popup = CreatePopup();

        popup.SetContent("Info", "A text that is long enough to wrap onto a few lines in the popup, " +
            "so that the popup is not just one small line at the corner.");
        popup.ShowNextTo(owner);

        Assert.True(area.Contains(popup.Bounds), "the popup left the screen: " + popup.Bounds + " in " + area);
    }

    [Fact]
    public void ANullOrDisposedOwner_ShowsNothing_AndRaisesNothing()
    {
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");
        Button gone = new Button();
        gone.Dispose();

        Exception? thrown = Record.Exception(() =>
        {
            popup.ShowNextTo(null!);
            popup.ShowNextTo(gone);
            popup.ShowAtMouse(gone, new Point(1, 1));
            popup.ShowCenteredAbove(gone, 1);
            popup.ShowAtMouseDelayed(gone, new Point(1, 1), Section("a", "b"));
        });

        Assert.Null(thrown);
        Assert.False(popup.Visible);
    }

    // ---- inside the window ----

    // Where the popup may be: the window, as far as it is on the screen.
    private static Rectangle Area(Form host)
    {
        return Rectangle.Intersect(host.Bounds, Screen.FromControl(host).WorkingArea);
    }

    private static Point OnScreen(Form host, int x, int y)
    {
        return host.PointToScreen(new Point(x, y));
    }

    [Fact]
    public void NextToAControlAtTheEdgeOfTheWindow_ThePopupStaysInsideTheWindow()
    {
        (Form host, Button owner) = CreateHost(new Point(100, 100), new Size(400, 300), new Point(310, 220));
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("Info", "A text that is long enough to take some room next to the control.");

        popup.ShowNextTo(owner);

        Rectangle area = Area(host);
        Assert.True(area.Contains(popup.Bounds), "outside the window: " + popup.Bounds + " in " + area);
    }

    [Fact]
    public void AtTheMouseInTheCornerOfTheWindow_ThePopupStaysInsideTheWindow_WithAMarginToTheEdge()
    {
        (Form host, Button owner) = CreateHost(new Point(100, 100), new Size(400, 300));
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("Info", "A text that is long enough to take some room at the mouse.");

        popup.ShowAtMouse(owner, OnScreen(host, 395, 295));

        Rectangle area = Area(host);
        Assert.True(area.Contains(popup.Bounds), "outside the window: " + popup.Bounds + " in " + area);
        Assert.True(popup.Right <= area.Right - 10 && popup.Bottom <= area.Bottom - 10, "no margin to the edge");
    }

    [Fact]
    public void CenteredAboveAControlAtTheTopOfTheWindow_ThePopupGoesBelow_InsideTheWindow()
    {
        (Form host, Button owner) = CreateHost(new Point(100, 100), new Size(400, 300), new Point(10, 4));
        InfoPopupForm popup = CreatePopup(new Size(70, 32));
        popup.SetContent("70%");

        popup.ShowCenteredAbove(owner, 5);

        Rectangle area = Area(host);
        Assert.True(area.Contains(popup.Bounds), "outside the window: " + popup.Bounds + " in " + area);
        Assert.True(popup.Top >= owner.PointToScreen(Point.Empty).Y, "not below the control");
    }

    [Fact]
    public void AWindowThatReachesBeyondTheScreen_GivesAPopupInsideTheVisiblePart()
    {
        Rectangle screen = SystemInformation.VirtualScreen;
        (Form host, Button owner) = CreateHost(new Point(screen.Right - 150, 100), new Size(400, 300), new Point(20, 100));
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("Hi");

        popup.ShowAtMouse(owner, OnScreen(host, 100, 150));

        Rectangle area = Area(host);
        Assert.True(area.Right < host.Right, "the window is not beyond the screen");
        Assert.True(area.Contains(popup.Bounds), "outside the visible part: " + popup.Bounds + " in " + area);
    }

    [Fact]
    public void APopupLargerThanTheWindow_SitsAtItsTopLeft_WithoutFuss()
    {
        (Form host, Button owner) = CreateHost(new Point(100, 100), new Size(120, 70), new Point(10, 10));
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("Info", "A text that is long enough to make the popup larger than this small window, so it cannot fit inside.");

        Exception? thrown = Record.Exception(() => popup.ShowNextTo(owner));

        Assert.Null(thrown);
        Assert.True(popup.Visible);
        Assert.Equal(Area(host).Location, popup.Location);
    }

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

    // ---- the window ----

    [Fact]
    public void ThePopup_NeverTakesTheActivationAway_AndClosesWithItsOwnerForm()
    {
        (Form host, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");

        popup.ShowNextTo(owner);

        Assert.True((bool)typeof(InfoPopupForm).GetProperty("ShowWithoutActivation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(popup)!);
        Assert.Same(host, popup.Owner);

        host.Close();
        Assert.True(popup.IsDisposed || !popup.Visible);
    }

    [Fact]
    public void WhileTheApplicationIsNotInFront_NothingIsShown_NotEvenTheDelayedOne()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");
        PopupSupport.OwnerIsActive = _ => false;

        popup.ShowNextTo(owner);
        popup.ShowAtMouse(owner, new Point(100, 100));
        popup.ShowCenteredAbove(owner, 10);
        popup.ShowAtMouseDelayed(owner, new Point(100, 100), Section("A", "b"));
        popup.InvokePrivate("OnShowDelayTimerTick", popup, EventArgs.Empty);

        Assert.False(popup.Visible);

        PopupSupport.OwnerIsActive = _ => true;
        popup.ShowNextTo(owner);
        Assert.True(popup.Visible);
    }

    [Fact]
    public void OverAMinimizedWindow_NothingIsShown()
    {
        (Form host, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");

        host.WindowState = FormWindowState.Minimized;
        popup.ShowNextTo(owner);

        Assert.False(popup.Visible);
    }

    [Fact]
    public void ThePopup_IsNotTopmost_SoItStaysAboveItsWindowOnly()
    {
        (Form _, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");

        popup.ShowNextTo(owner);

        Assert.False(popup.TopMost);
        Assert.NotNull(popup.Owner);
    }

    [Fact]
    public void WhenTheWindowIsMoved_ThePopupGoesWithIt_UntilItIsHidden()
    {
        (Form host, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");
        popup.ShowNextTo(owner);
        Point before = popup.Location;

        host.Location = new Point(host.Left + 40, host.Top + 25);
        Assert.Equal(new Point(before.X + 40, before.Y + 25), popup.Location);

        popup.Hide();
        Point hidden = popup.Location;
        host.Location = new Point(host.Left + 10, host.Top + 10);
        Assert.Equal(hidden, popup.Location);
    }

    [Fact]
    public void AfterBeingPlacedAgain_ThePopupFollowsFromItsNewPlace()
    {
        (Form host, Button owner) = CreateHost();
        InfoPopupForm popup = CreatePopup();
        popup.SetContent("x");
        popup.ShowNextTo(owner);
        popup.ShowAtMouse(owner, new Point(host.Left + 50, host.Top + 50));
        Point placed = popup.Location;

        host.Location = new Point(host.Left + 30, host.Top);

        Assert.Equal(new Point(placed.X + 30, placed.Y), popup.Location);
    }

    // ---- the wait before a hover popup shows ----

    private static int WaitInterval(InfoPopupForm popup) => popup.GetPrivateField<System.Windows.Forms.Timer>("_showDelayTimer")!.Interval;

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

    private static int Interval(InfoPopupForm popup) => popup.GetPrivateField<System.Windows.Forms.Timer>("_hideTimer")!.Interval;

    private static bool TimeRuns(InfoPopupForm popup) => popup.GetPrivateField<System.Windows.Forms.Timer>("_hideTimer")!.Enabled;

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
