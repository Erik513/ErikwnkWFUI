using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Tests.Controls.RadioButtons;

/// <summary>The size of the radio button: the factory sets it, with AutoSize on it follows the text and the font, and it is as large as a check box.</summary>
public class RadioButtonSizeTests
{
    [Fact]
    public void TheFactoryButtons_HaveNoAutomaticSize_AndTheSizeOfACheckBoxWithTheSameText()
    {
        using System.Windows.Forms.RadioButton radio = UIStyles.RadioButtons.CreateStandard("Label");
        using System.Windows.Forms.CheckBox check = UIStyles.CheckBoxes.CreateStandard("Label");

        Assert.False(radio.AutoSize);
        Assert.Equal(check.Size, radio.Size);
    }

    [Fact]
    public void WithAutoSizeOn_TheSizeFollowsTheTextAndTheFont()
    {
        using Form host = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000) };
        using ErikwnkWFUI.Controls.RadioButton radio = new ErikwnkWFUI.Controls.RadioButton { Text = "Short", AutoSize = true };
        host.Controls.Add(radio);
        host.Show();

        Assert.Equal(ErikwnkWFUI.Controls.RadioButton.ComputePreferredSize("Short", radio.Font, radio.DeviceDpi), radio.Size);

        radio.Text = "A much longer label than the first one";

        Assert.Equal(ErikwnkWFUI.Controls.RadioButton.ComputePreferredSize(radio.Text, radio.Font, radio.DeviceDpi), radio.Size);
        Assert.True(radio.Width > 100);
    }

    [Fact]
    public void ACompactButton_IsASquare()
    {
        using System.Windows.Forms.RadioButton radio = UIStyles.RadioButtons.CreateStandardCompact();

        Assert.Equal(radio.Width, radio.Height);
    }
}