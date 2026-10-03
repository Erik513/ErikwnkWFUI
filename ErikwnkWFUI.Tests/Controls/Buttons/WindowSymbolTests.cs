using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Tests.Controls.Buttons;

/// <summary>
/// The title bar's three buttons carry the window symbols of the library, and the
/// maximize button turns into the restore one and back.
/// </summary>
public class WindowSymbolTests
{
    private static Button Field(TitleBarControl bar, string name)
    {
        return (Button)typeof(TitleBarControl).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(bar)!;
    }

    [Fact]
    public void TheTitleBarButtons_CarryTheWindowSymbols()
    {
        using TitleBarControl bar = new TitleBarControl();

        Assert.Equal(UIStyles.Symbols.Minimize, Field(bar, "_minimizeButton").Text);
        Assert.Equal(UIStyles.Symbols.Maximize, Field(bar, "_maximizeButton").Text);
        Assert.Equal(UIStyles.Symbols.Close, Field(bar, "_closeButton").Text);
    }

    [Fact]
    public void TheMaximizeButton_ShowsRestoreWhileMaximized_AndMaximizeAgainAfterwards()
    {
        using TitleBarControl bar = new TitleBarControl();
        Button maximize = Field(bar, "_maximizeButton");

        bar.UpdateMaximizeButton(true);
        Assert.Equal(UIStyles.Symbols.Restore, maximize.Text);

        bar.UpdateMaximizeButton(true);
        Assert.Equal(UIStyles.Symbols.Restore, maximize.Text);

        bar.UpdateMaximizeButton(false);
        Assert.Equal(UIStyles.Symbols.Maximize, maximize.Text);
    }
}
