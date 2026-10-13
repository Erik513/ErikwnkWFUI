using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Tests.Controls.CheckBox;

/// <summary>The size of the box is the caller's: no automatic size, also for a box made with new.</summary>
public class CheckBoxSizeTests
{
    [Fact]
    public void ABoxMadeWithNew_HasNoAutomaticSize_AndKeepsTheSizeItWasGiven()
    {
        using ErikwnkWFUI.Controls.CheckBox box = new ErikwnkWFUI.Controls.CheckBox { Text = "A longer label than the default size holds", Size = new Size(77, 31) };
        _ = box.Handle;

        Assert.False(box.AutoSize);
        Assert.Equal(new Size(77, 31), box.Size);
    }

    [Fact]
    public void TheFactoryBoxes_HaveNoAutomaticSizeEither()
    {
        using System.Windows.Forms.CheckBox standard = UIStyles.CheckBoxes.CreateStandard("Label");
        using System.Windows.Forms.CheckBox compact = UIStyles.CheckBoxes.CreateStandardCompact();

        Assert.False(standard.AutoSize);
        Assert.False(compact.AutoSize);
    }
}
