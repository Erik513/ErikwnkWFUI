using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>
/// Giving a layered window its picture: it costs no drawing objects of the system, also when the picture cannot be made. [Collection]: the number of
/// drawing objects of the whole process is counted, and other tests make and drop some.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class LayeredWindowTests
{
    private const uint GdiObjects = 0;

    [DllImport("user32.dll")]
    private static extern uint GetGuiResources(IntPtr process, uint flags);

    private static uint Count()
    {
        return GetGuiResources(Process.GetCurrentProcess().Handle, GdiObjects);
    }

    [Fact]
    public void ForAWindowThatIsNotThere_NothingThrows_AndNoDrawingObjectIsLeft()
    {
        using Bitmap picture = new Bitmap(20, 20);
        uint before = Count();

        for (int i = 0; i < 100; i++)
            LayeredWindow.Update(IntPtr.Zero, picture, 255);

        Assert.InRange(Count(), 0u, before + 30);
    }

    [Fact]
    public void WhenThePictureCannotBeMade_TheExceptionComesThrough_AndNoDeviceContextIsLeft()
    {
        Bitmap picture = new Bitmap(20, 20);
        picture.Dispose();
        uint before = Count();

        for (int i = 0; i < 100; i++)
            Assert.ThrowsAny<Exception>(() => LayeredWindow.Update(IntPtr.Zero, picture, 255));

        Assert.InRange(Count(), 0u, before + 30);
    }
}
