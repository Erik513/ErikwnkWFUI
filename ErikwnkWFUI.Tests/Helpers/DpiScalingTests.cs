using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>The sizes that were once fixed pixels (the title bar, the caption buttons, the ring, the slim bar, the default button) are given for 96 dpi and scaled to the screen; at 96 dpi they are what they were.</summary>
public class DpiScalingTests
{
    private static int ScreenDpi()
    {
        using Control probe = new Control();

        return probe.DeviceDpi;
    }

    private static int Scaled(int value96)
    {
        return (int)Math.Round(value96 * ScreenDpi() / 96.0, MidpointRounding.AwayFromZero);
    }

    [Fact]
    public void TheDefaultSizes_AreTheOnesOf96Dpi_ScaledToTheScreen()
    {
        using Spinner ring = UIStyles.Spinners.CreateStandard();
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard();
        using Button button = UIStyles.Buttons.CreateStandard("a");
        using Button symbol = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Copy);
        using TitleBarControl bar = new TitleBarControl();

        Assert.Equal(new Size(Scaled(24), Scaled(24)), ring.Size);
        Assert.Equal(Scaled(4), slim.Height);
        Assert.Equal(new Size(Scaled(30), Scaled(30)), button.Size);
        Assert.Equal(new Size(Scaled(30), Scaled(30)), symbol.Size);
        Assert.Equal(Scaled(30), bar.Height);
    }

    [Fact]
    public void ASizeThatIsGiven_IsGivenFor96Dpi_ForTheRing()
    {
        using Spinner ring = UIStyles.Spinners.CreateStandard(40);

        Assert.Equal(new Size(Scaled(40), Scaled(40)), ring.Size);
    }
}
