using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;
using SlimProgressBar = ErikwnkWFUI.Controls.SlimProgressBar;
using Spinner = ErikwnkWFUI.Controls.Spinner;
using TrackBar = ErikwnkWFUI.Controls.TrackBar;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>The controls that draw everything themselves share one set of styles.</summary>
public class ControlStyleSetsTests
{
    public static IEnumerable<object[]> Controls()
    {
        yield return new object[] { new ToggleSwitch() };
        yield return new object[] { new TrackBar() };
        yield return new object[] { new Spinner() };
        yield return new object[] { new CardPanel() };
        yield return new object[] { new SlimProgressBar() };
        yield return new object[] { new ReadOnlyTabControl() };
        yield return new object[] { new SymbolButton() };
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void ADrawnControl_HasEveryStyleOfTheSharedSet(Control control)
    {
        using (control)
        {
            foreach (ControlStyles style in new[]
            {
                ControlStyles.UserPaint, ControlStyles.AllPaintingInWmPaint,
                ControlStyles.OptimizedDoubleBuffer, ControlStyles.ResizeRedraw
            })
            {
                Assert.True(control.InvokePrivate<bool>("GetStyle", style), control.GetType().Name + " lacks " + style);
            }
        }
    }

    [Fact]
    public void TheSharedSet_IsTheFourStylesAndNothingElse()
    {
        Assert.Equal(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            ControlStyleSets.OwnerDrawn);
    }
}
