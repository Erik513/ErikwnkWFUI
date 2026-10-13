using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>
/// The toast follows the scaling of the screen its window is on: its size, the padding and the
/// place of the check mark. The tests give it other scalings directly, as a test machine has one.
/// [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ToastScalingTests : ToastTestBase
{
    private static int Scaled(int value96, int dpi) => (int)Math.Round(value96 * dpi / 96.0, MidpointRounding.AwayFromZero);

    private static Label Part(ToastForm toast, string name) => toast.GetPrivateField<Label>(name)!;

    [Theory]
    [InlineData(96, 350, 80)]
    [InlineData(120, 438, 100)]
    [InlineData(144, 525, 120)]
    [InlineData(192, 700, 160)]
    public void TheSizeAndThePlacesInside_ScaleWithTheDpi(int dpi, int width, int height)
    {
        using ToastForm toast = new ToastForm { Symbol = ToastIcon.Success };

        toast.ApplyDpi(dpi);

        Assert.Equal(new Size(width, height), toast.Size);
        Assert.Equal(new Size(Scaled(30, dpi), Scaled(30, dpi)), Part(toast, "_iconLabel").Size);
        Assert.Equal(new Point(Scaled(10, dpi), (height - Scaled(30, dpi)) / 2), Part(toast, "_iconLabel").Location);
        Assert.Equal(Scaled(30, dpi), Part(toast, "_messageLabel").Padding.Left);
    }

    [Fact]
    public void AToast_TakesTheScalingOfTheScreenItsWindowIsOn()
    {
        Form host = Host(client: new Size(900, 500));

        ToastForm.OwnerDpi = _ => 144;
        ToastForm.ShowToast("Scaled", host);

        Assert.Equal(new Size(525, 120), Current()!.Size);
    }

    [Fact]
    public void AScalingOnADisposedToast_IsHarmless()
    {
        ToastForm toast = new ToastForm();
        toast.Dispose();

        Exception? thrown = Record.Exception(() => toast.ApplyDpi(144));

        Assert.Null(thrown);
    }
}
