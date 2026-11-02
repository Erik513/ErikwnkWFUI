using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>
/// The info popup: where it is shown, inside the window, and the window itself.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class InfoPopupPlacementTests : InfoPopupTestBase
{
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

    [Trait("Focus", "true")]   // (it minimizes a window, which hands the foreground to another one)
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
}
