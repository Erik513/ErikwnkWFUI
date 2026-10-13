using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>The scaling of 96-dpi measures: the same everywhere, halves round away from zero, the seam of the tests wins.</summary>
public class DpiTests
{
    [Theory]
    [InlineData(10, 96, 10)]
    [InlineData(10, 144, 15)]
    [InlineData(10, 192, 20)]
    [InlineData(0, 192, 0)]
    [InlineData(2, 120, 3)]   // 2.5 rounds up (not to the even 2)
    [InlineData(6, 120, 8)]   // 7.5 -> 8
    [InlineData(10, 120, 13)] // 12.5 -> 13
    [InlineData(-2, 120, -3)] // away from zero
    public void AMeasure_IsScaledToTheScreen(int value96, int dpi, int expected)
    {
        Assert.Equal(expected, Dpi.Scale(value96, dpi));
    }

    [Fact]
    public void TheSeam_PretendsAnotherScreen_ZeroMeansTheRealOne()
    {
        using Control control = new Control();

        Assert.Equal(Dpi.Scale(10, control.DeviceDpi), Dpi.Scale(control, 10));
        Assert.Equal(Dpi.Scale(10, control.DeviceDpi), Dpi.Scale(control, 10, 0));
        Assert.Equal(20, Dpi.Scale(control, 10, 192));
    }
}
