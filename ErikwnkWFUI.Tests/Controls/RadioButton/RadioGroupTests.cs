using System.Windows.Forms;
using ErikwnkWFUI.Controls;

namespace ErikwnkWFUI.Tests.Controls.RadioButtons;

/// <summary>A group of radio buttons that need not share a parent: one choice, one event, buttons that come and go.</summary>
public class RadioGroupTests
{
    private static System.Windows.Forms.RadioButton[] Buttons(int count)
    {
        System.Windows.Forms.RadioButton[] buttons = new System.Windows.Forms.RadioButton[count];

        for (int i = 0; i < count; i++)
            buttons[i] = UIStyles.RadioButtons.CreateStandard("Item " + i);

        return buttons;
    }

    [Fact]
    public void SelectingOne_ClearsTheOthers_EvenInDifferentParents()
    {
        var buttons = Buttons(3);
        using System.Windows.Forms.Panel one = new System.Windows.Forms.Panel();
        using System.Windows.Forms.Panel two = new System.Windows.Forms.Panel();
        one.Controls.Add(buttons[0]);
        two.Controls.Add(buttons[1]);
        two.Controls.Add(buttons[2]);

        using RadioGroup group = new RadioGroup(buttons);
        buttons[0].Checked = true;
        buttons[1].Checked = true;

        Assert.False(buttons[0].Checked);
        Assert.True(buttons[1].Checked);
        Assert.Equal(1, group.SelectedIndex);
        Assert.Same(buttons[1], group.SelectedButton);

        buttons[2].Checked = true;

        Assert.False(buttons[1].Checked);
        Assert.Equal(2, group.SelectedIndex);
    }

    [Fact]
    public void SelectionChanged_IsRaisedOncePerChoice()
    {
        var buttons = Buttons(3);
        using RadioGroup group = new RadioGroup(buttons);
        int raised = 0;
        group.SelectionChanged += (s, e) => raised++;

        buttons[0].Checked = true;
        Assert.Equal(1, raised);

        buttons[1].Checked = true;
        Assert.Equal(2, raised);

        buttons[1].Checked = true;
        Assert.Equal(2, raised);
    }

    [Fact]
    public void SelectedIndex_SelectsAndClears_AndChecksItsRange()
    {
        var buttons = Buttons(3);
        using RadioGroup group = new RadioGroup(buttons);
        int raised = 0;
        group.SelectionChanged += (s, e) => raised++;

        Assert.Equal(-1, group.SelectedIndex);
        Assert.Null(group.SelectedButton);

        group.SelectedIndex = 2;
        Assert.True(buttons[2].Checked);
        Assert.Equal(1, raised);

        group.SelectedIndex = -1;
        Assert.All(buttons, button => Assert.False(button.Checked));
        Assert.Equal(2, raised);

        group.SelectedIndex = -1;
        Assert.Equal(2, raised);

        Assert.Throws<ArgumentOutOfRangeException>(() => group.SelectedIndex = 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => group.SelectedIndex = -2);
    }

    [Fact]
    public void SeveralCheckedAtTheStart_LeaveTheLastOneAdded()
    {
        var buttons = Buttons(3);
        buttons[0].Checked = true;
        buttons[2].Checked = true;

        using RadioGroup group = new RadioGroup(buttons);

        // (A button that is added checked clears the ones before it.)
        Assert.Equal(2, group.SelectedIndex);
        Assert.False(buttons[0].Checked);
    }

    [Fact]
    public void ARemovedButton_IsLeftAlone_AndAddingTwiceChangesNothing()
    {
        var buttons = Buttons(2);
        using RadioGroup group = new RadioGroup(buttons);
        group.Add(buttons[0]);
        Assert.Equal(2, group.Buttons.Count);

        Assert.True(group.Remove(buttons[0]));
        Assert.False(group.Remove(buttons[0]));

        buttons[0].Checked = true;
        buttons[1].Checked = true;

        Assert.True(buttons[0].Checked);
        Assert.True(buttons[1].Checked);
    }

    [Fact]
    public void ADisposedButton_LeavesTheGroup()
    {
        var buttons = Buttons(2);
        using RadioGroup group = new RadioGroup(buttons);

        buttons[0].Dispose();

        Assert.Single(group.Buttons);
    }

    [Fact]
    public void ADisposedGroup_LetsGoOfTheButtons()
    {
        var buttons = Buttons(2);
        RadioGroup group = new RadioGroup(buttons);
        group.Dispose();
        group.Dispose();

        buttons[0].Checked = true;
        buttons[1].Checked = true;

        Assert.True(buttons[0].Checked);
        Assert.Empty(group.Buttons);
        Assert.Throws<ObjectDisposedException>(() => group.Add(buttons[0]));
    }

    [Fact]
    public void NullsAreHandled()
    {
        using RadioGroup empty = new RadioGroup();
        using RadioGroup withNull = new RadioGroup(null);

        Assert.Equal(-1, empty.SelectedIndex);
        Assert.Throws<ArgumentNullException>(() => empty.Add(null));
        Assert.False(empty.Remove(null));
    }

    [Fact]
    public void AHandlerThatChangesTheChoice_DoesNotLoopForever()
    {
        var buttons = Buttons(2);
        using RadioGroup group = new RadioGroup(buttons);
        int raised = 0;
        group.SelectionChanged += (s, e) =>
        {
            raised++;

            if (group.SelectedIndex == 0)
                group.SelectedIndex = 1;
        };

        buttons[0].Checked = true;

        Assert.Equal(1, group.SelectedIndex);
        Assert.True(raised >= 2 && raised < 10);
    }
}