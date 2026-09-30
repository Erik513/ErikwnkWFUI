using System;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using Xunit;

namespace ErikwnkWFUI.Tests.Helpers;

public class OutsideClickFilterTests
{
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MOUSEMOVE = 0x0200;

    private static bool Send(OutsideClickFilter filter, int message, IntPtr window)
    {
        Message m = Message.Create(window, message, IntPtr.Zero, IntPtr.Zero);
        return filter.PreFilterMessage(ref m);
    }

    [Fact]
    public void AnyButtonDownOutside_CallsBack_AndNeverSwallowsTheClick()
    {
        int calls = 0;
        OutsideClickFilter filter = new OutsideClickFilter(w => true, () => calls++);

        Assert.False(Send(filter, WM_LBUTTONDOWN, (IntPtr)1));
        Assert.False(Send(filter, WM_RBUTTONDOWN, (IntPtr)1));

        Assert.Equal(2, calls);
    }

    [Fact]
    public void LeftButtonOnly_IgnoresTheOtherButtons()
    {
        int calls = 0;
        OutsideClickFilter filter = new OutsideClickFilter(w => true, () => calls++, leftButtonOnly: true);

        Send(filter, WM_RBUTTONDOWN, (IntPtr)1);
        Assert.Equal(0, calls);

        Send(filter, WM_LBUTTONDOWN, (IntPtr)1);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void AClickInside_AndOtherMessages_DoNothing_AndTheWindowIsPassedOn()
    {
        int calls = 0;
        IntPtr inside = (IntPtr)7;
        OutsideClickFilter filter = new OutsideClickFilter(w => w != inside, () => calls++);

        Send(filter, WM_LBUTTONDOWN, inside);
        Send(filter, WM_MOUSEMOVE, (IntPtr)1);

        Assert.Equal(0, calls);
    }

    [Fact]
    public void StartAndStop_AreSafeToRepeat()
    {
        OutsideClickFilter filter = new OutsideClickFilter(w => true, () => { });

        filter.Start();
        filter.Start();
        filter.Stop();
        filter.Stop();
        filter.Dispose();
    }
}
