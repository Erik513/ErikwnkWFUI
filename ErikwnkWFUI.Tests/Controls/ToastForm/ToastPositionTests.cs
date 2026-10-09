using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>Where the toast is shown in its window: the default and the fixed places.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastPositionTests : IDisposable
{
    private static readonly Rectangle Area = new Rectangle(100, 200, 800, 600);
    private static readonly Size Toast = new Size(350, 80);
    private const int Edge = 20;

    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;
    private Form? _host;

    public ToastPositionTests()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;
        Current()?.Dispose();
        _host?.Dispose();
        ((System.Collections.IDictionary)typeof(ToastForm).GetField("_pending", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!).Clear();
    }

    private static Point Locate(ToastPosition position)
    {
        return (Point)typeof(ToastForm).GetMethod("GetToastLocation", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { Area, Toast, position, Edge })!;
    }

    private static ToastForm? Current()
    {
        return (ToastForm?)typeof(ToastForm).GetField("_currentToast", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);
    }

    private Form Host(Size client)
    {
        _host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(100, 100), ShowInTaskbar = false, ClientSize = client };
        _host.Show();

        return _host;
    }

    private static int CenterX => Area.Left + (Area.Width - Toast.Width) / 2;

    [Fact]
    public void WithoutAPosition_ItIsBelowTheMiddle()
    {
        Assert.Equal(ToastPosition.BelowCenter, default(ToastPosition));
        Assert.Equal(ToastPosition.BelowCenter, ErikwnkWFUI.UIStyles.Toasts.CreateStandard().Position);
        Assert.Equal(ToastPosition.BelowCenter, ErikwnkWFUI.UIStyles.Toasts.CreatePrimary().Position);
    }

    [Fact]
    public void BelowTheMiddle_IsCenteredThreeQuartersDown()
    {
        Point at = Locate(ToastPosition.BelowCenter);

        Assert.Equal(CenterX, at.X);
        Assert.Equal(Area.Top + 450 - Toast.Height / 2, at.Y);
    }

    [Fact]
    public void TheMiddle_IsInTheMiddleOfBothAxes()
    {
        Point at = Locate(ToastPosition.Center);

        Assert.Equal(CenterX, at.X);
        Assert.Equal(Area.Top + (Area.Height - Toast.Height) / 2, at.Y);
    }

    [Fact]
    public void AboveTheMiddle_IsCenteredAQuarterDown()
    {
        Point at = Locate(ToastPosition.AboveCenter);

        Assert.Equal(CenterX, at.X);
        Assert.Equal(Area.Top + 150 - Toast.Height / 2, at.Y);
        Assert.True(at.Y < Locate(ToastPosition.Center).Y);
    }

    [Theory]
    [InlineData(ToastPosition.TopCenter, false, true, true)]
    [InlineData(ToastPosition.BottomCenter, false, true, false)]
    [InlineData(ToastPosition.TopLeft, true, false, true)]
    [InlineData(ToastPosition.TopRight, true, false, true)]
    [InlineData(ToastPosition.BottomLeft, true, false, false)]
    [InlineData(ToastPosition.BottomRight, true, false, false)]
    public void EdgesAndCorners_KeepTheFixedDistance(ToastPosition position, bool atSide, bool centered, bool atTop)
    {
        Point at = Locate(position);

        if (centered)
            Assert.Equal(CenterX, at.X);

        if (atSide)
        {
            bool left = position == ToastPosition.TopLeft || position == ToastPosition.BottomLeft;
            Assert.Equal(left ? Area.Left + Edge : Area.Right - Edge - Toast.Width, at.X);
        }

        Assert.Equal(atTop ? Area.Top + Edge : Area.Bottom - Edge - Toast.Height, at.Y);
    }

    [Fact]
    public void EveryPosition_IsInsideTheArea_AndNineDifferentPlacesExist()
    {
        List<Point> places = new List<Point>();

        foreach (ToastPosition position in Enum.GetValues(typeof(ToastPosition)))
        {
            Point at = Locate(position);

            Assert.True(Area.Contains(new Rectangle(at, Toast)), position + " is outside: " + at);
            places.Add(at);
        }

        Assert.Equal(9, places.Distinct().Count());
    }

    [Fact]
    public void ARealToast_IsShownAtItsPosition_InsideTheClientArea()
    {
        Form host = Host(new Size(800, 600));
        Rectangle client = host.RectangleToScreen(host.ClientRectangle);
        Rectangle visible = Rectangle.Intersect(client, Screen.FromControl(host).WorkingArea);

        ToastForm.ShowToast("Corner", host, position: ToastPosition.TopLeft);

        Assert.Equal(visible.Left + Edge, Current()!.Left);
        Assert.Equal(visible.Top + Edge, Current()!.Top);

        ToastForm.ShowToast("Corner", host, position: ToastPosition.BottomRight);

        Assert.Equal(visible.Right - Edge - Current()!.Width, Current()!.Left);
        Assert.Equal(visible.Bottom - Edge - Current()!.Height, Current()!.Top);
    }

    [Fact]
    public void ASmallWindow_KeepsTheToastInside_AtEveryPosition()
    {
        Form host = Host(new Size(380, 120));
        Rectangle area = Rectangle.Intersect(host.Bounds, Screen.FromControl(host).WorkingArea);

        foreach (ToastPosition position in Enum.GetValues(typeof(ToastPosition)))
        {
            ToastForm.ShowToast("Small", host, position: position);

            Assert.True(Current()!.Top >= area.Top && Current()!.Bottom <= area.Bottom, position + " leaves the window in height");
            Assert.True(Current()!.Left >= area.Left, position + " leaves the window on the left");
        }
    }

    [Fact]
    public void TheFactories_AndTheToaster_PassThePositionOn()
    {
        Form host = Host(new Size(800, 600));
        Rectangle client = host.RectangleToScreen(host.ClientRectangle);

        ErikwnkWFUI.UIStyles.Toasts.ShowStandard("a", host, position: ToastPosition.TopLeft);
        int standardLeft = Current()!.Left;
        ErikwnkWFUI.UIStyles.Toasts.ShowPrimary("b", host, position: ToastPosition.TopLeft);
        int primaryLeft = Current()!.Left;
        ErikwnkWFUI.UIStyles.Toasts.ShowStandard(new InfoPopupSection("t", "c"), host, position: ToastPosition.TopLeft);
        int contentLeft = Current()!.Left;

        Assert.Equal(standardLeft, primaryLeft);
        Assert.Equal(standardLeft, contentLeft);
        Assert.True(standardLeft < client.Left + client.Width / 4);

        Toaster toaster = ErikwnkWFUI.UIStyles.Toasts.CreateStandard(position: ToastPosition.BottomRight);
        Assert.Equal(ToastPosition.BottomRight, toaster.Position);
        toaster.Show("d", host);
        Assert.True(Current()!.Left > client.Left + client.Width / 2);

        toaster.Position = ToastPosition.BelowCenter;
        toaster.Show("e", host);
        Assert.Equal(client.Left + (client.Width - Current()!.Width) / 2, Current()!.Left);
    }

    [Fact]
    public void AToastThatWaits_KeepsItsPosition_AndTwoOfTheSameTextAtOtherPlacesAreTwo()
    {
        Form host = Host(new Size(800, 600));
        PopupSupport.OwnerIsActive = _ => false;

        ToastForm.ShowToast("Same", host, position: ToastPosition.TopLeft);
        ToastForm.ShowToast("Same", host, position: ToastPosition.BottomRight);
        ToastForm.ShowToast("Same", host, position: ToastPosition.BottomRight);

        var queue = (System.Collections.IEnumerable)((System.Collections.IDictionary)typeof(ToastForm)
            .GetField("_pending", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!)[host]!;

        Assert.Equal(2, queue.Cast<object>().Count());

        PopupSupport.OwnerIsActive = _ => true;
        typeof(ToastForm).GetMethod("OnOwnerActivated", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { host, EventArgs.Empty });
        Rectangle client = host.RectangleToScreen(host.ClientRectangle);

        Assert.True(Current()!.Left < client.Left + client.Width / 4, "the first one is shown at the top left");
    }
}
