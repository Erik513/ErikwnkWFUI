using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>The tooltips of the library wrap a long text at a fixed width, not only the one with the full text of a label or a bar.</summary>
public class ToolTipWidthTests
{
    private static readonly PropertyInfo HandleProperty = typeof(ToolTip).GetProperty("Handle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

    private static int MaxWidthOf(ToolTip toolTip)
    {
        IntPtr window = (IntPtr)HandleProperty.GetValue(toolTip)!;

        return (int)SendMessage(window, TTM_GETMAXTIPWIDTH, IntPtr.Zero, IntPtr.Zero);
    }

    private static int Expected(Control owner) => (int)Math.Round(ToolTipWidth.MaxWidthAt96Dpi * owner.DeviceDpi / 96.0);

    [Fact]
    public void TheTooltipWithTheFullTextOfACellOfTheListView_IsLimited()
    {
        using ErikwnkWFUI.Controls.ListView list = (ErikwnkWFUI.Controls.ListView)UIStyles.ListViews.CreateStandard();
        ToolTip toolTip = list.GetPrivateField<ToolTip>("_cellToolTip")!;

        Assert.Equal(Expected(list), MaxWidthOf(toolTip));
    }

    [Fact]
    public void TheTooltipOfATab_IsLimited()
    {
        using ReadOnlyTabControl tabs = UIStyles.TabControls.CreateReadOnlyStandard();
        ToolTip toolTip = (ToolTip)typeof(ReadOnlyTabControl).GetMethod("EnsureTabToolTip", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(tabs, null)!;

        Assert.Equal(Expected(tabs), MaxWidthOf(toolTip));
    }

    [Fact]
    public void TheTooltipOfAToggleSwitch_IsLimited()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(true, "On", "Off");
        toggle.ToolTipText = "A text for the tooltip of a switch";
        ToolTip toolTip = toggle.GetPrivateField<ToolTip>("_toolTip")!;

        Assert.Equal(Expected(toggle), MaxWidthOf(toolTip));
    }

    [Fact]
    public void ALimitThatIsGivenAgain_ChangesTheWidth_AndATooltipWithoutOneIsLeftAlone()
    {
        using ToggleSwitch owner = UIStyles.ToggleSwitches.CreateStandard(true, "On", "Off");
        ToolTip limited = new ToolTip();
        ToolTip alone = new ToolTip();

        ToolTipWidth.Set(limited, 300);
        Assert.Equal(300, MaxWidthOf(limited));

        ToolTipWidth.Set(limited, 250);
        Assert.Equal(250, MaxWidthOf(limited));

        // (A tooltip without a limit: nothing is sent to its window, it has the width Windows gives it.)
        ToolTipWidth.Apply(alone);
        Assert.NotEqual(300, MaxWidthOf(alone));
        Assert.NotEqual(250, MaxWidthOf(alone));

        limited.Dispose();
        alone.Dispose();
    }
}
