using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;
using TextBox = System.Windows.Forms.TextBox;
using VolumeControl = ErikwnkWFUI.Controls.VolumeControl;

namespace ErikwnkWFUI.Tests.Controls.VolumeControls;

/// <summary>Where the focus is after a click on the mute button: on the slider, so that the key M (named in the tooltip) and the arrow keys work at once. [Collection]: the focus belongs to the whole process.</summary>
[Trait("Focus", "true")]
[Collection(FocusTestCollection.Name)]
public class VolumeControlFocusTests
{
    private static Form Show(VolumeControl control, out TextBox other)
    {
        Form host = OffscreenForm.Create(new Size(220, 80), Color.Black);
        other = new TextBox { Location = new Point(0, 45), Width = 100 };
        host.Controls.Add(control);
        host.Controls.Add(other);
        host.Show();
        host.Activate();
        control.PerformLayout();
        other.Focus();
        Application.DoEvents();

        return host;
    }

    private static void Click(VolumeControl control) => control.MuteButton.InvokePrivate("OnClick", EventArgs.Empty);

    private static void PressM(VolumeControl control) => control.Slider.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.M));

    [Fact]
    public void AfterAClickOnTheButton_TheSliderHasTheFocus_AndTheKeyMWorksAtOnce()
    {
        StaThread.Run(() =>
        {
            using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
            using Form host = Show(control, out TextBox other);
            Assert.True(other.Focused, "the start is not as expected: the text box has no focus");

            Click(control);

            Assert.True(control.Slider.Focused, "the focus did not go to the slider");
            Assert.False(control.MuteButton.Focused);
            Assert.True(control.IsMuted);

            PressM(control);
            Assert.False(control.IsMuted);
        });
    }

    [Fact]
    public void TheFocus_StaysOnTheSlider_WhenTheButtonIsClickedAgain()
    {
        StaThread.Run(() =>
        {
            using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
            using Form host = Show(control, out TextBox other);

            Click(control);
            Click(control);

            Assert.True(control.Slider.Focused);
            Assert.False(control.IsMuted);
        });
    }

    [Fact]
    public void ADisabledControl_TakesNoFocus_AndChangesNothing()
    {
        StaThread.Run(() =>
        {
            using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
            using Form host = Show(control, out TextBox other);
            control.Enabled = false;
            other.Focus();

            Click(control);

            Assert.False(control.Slider.Focused);
            Assert.True(other.Focused);
            Assert.False(control.IsMuted);
        });
    }

    [Fact]
    public void TheWheelOverTheButton_DoesNotTakeTheFocus()
    {
        StaThread.Run(() =>
        {
            using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
            using Form host = Show(control, out TextBox other);

            control.MuteButton.InvokePrivate("OnMouseWheel", new HandledMouseEventArgs(MouseButtons.None, 0, 5, 5, 120));

            Assert.True(other.Focused);
        });
    }
}
