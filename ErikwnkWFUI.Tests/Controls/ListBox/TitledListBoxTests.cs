using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiTitledListBox = ErikwnkWFUI.Controls.TitledListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// TitledListBox is a thin wrapper around ListBox (an optional header bar
/// plus a set of proxied members - see its own class remarks) - these tests
/// only cover that the proxying itself is correct and that CreatePrimary
/// reaches the same colors ListBoxConstructionTests locks in on the plain
/// ListBox; the underlying selection/reorder logic itself is ListBox's own
/// concern, already covered there.
/// [Collection] (see AccentColorTestCollection) - these read UIColors.Primary,
/// which UIColorsTests changes via SetAccent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TitledListBoxTests
{
    // The one rule of this control: it is the ListBox with a title, so whatever the library's ListBox offers
    // (property, method, event) it offers too, of the same type.
    [Fact]
    public void EveryMemberOfTheLibrarysListBox_IsOnTheTitledListBoxToo()
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly;
        List<string> missing = new List<string>();

        foreach (System.Reflection.PropertyInfo property in typeof(ErikwnkWFUI.Controls.ListBox).GetProperties(flags))
        {
            System.Reflection.PropertyInfo mirrored = typeof(WfuiTitledListBox).GetProperty(property.Name);

            if (mirrored == null || mirrored.PropertyType != property.PropertyType)
                missing.Add("property " + property.Name);
        }

        foreach (System.Reflection.EventInfo item in typeof(ErikwnkWFUI.Controls.ListBox).GetEvents(flags))
        {
            if (typeof(WfuiTitledListBox).GetEvent(item.Name) == null)
                missing.Add("event " + item.Name);
        }

        foreach (System.Reflection.MethodInfo method in typeof(ErikwnkWFUI.Controls.ListBox).GetMethods(flags))
        {
            if (method.IsSpecialName)
                continue;

            Type[] parameters = method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

            if (typeof(WfuiTitledListBox).GetMethod(method.Name, parameters) == null)
                missing.Add("method " + method.Name);
        }

        Assert.Empty(missing);
    }

    [Fact]
    public void TheMirroredMembers_ReachTheInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();

        control.AllowReorder = true;
        control.ShowEnumeration = true;
        control.DisplayMember = "Name";
        control.SelectionMode = SelectionMode.MultiExtended;
        control.ItemBackColor = Color.Red;
        control.HoverBackColor = Color.Blue;
        control.Items.Add("a");
        control.Items.Add("b");
        control.SetSelected(1, true);

        Assert.True(control.InnerListBox.AllowReorder);
        Assert.True(control.InnerListBox.ShowEnumeration);
        Assert.Equal("Name", control.InnerListBox.DisplayMember);
        Assert.Equal(SelectionMode.MultiExtended, control.InnerListBox.SelectionMode);
        Assert.Equal(Color.Red, control.InnerListBox.ItemBackColor);
        Assert.Equal(Color.Blue, control.InnerListBox.HoverBackColor);
        Assert.True(control.GetSelected(1));
        Assert.Single(control.SelectedItems);
        Assert.Equal(1, control.FindString("b"));
    }

    [Fact]
    public void Items_ProxiesToInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();

        control.Items.Add("A");
        control.Items.Add("B");

        Assert.Equal(2, control.InnerListBox.Items.Count);
        Assert.Equal("A", control.InnerListBox.Items[0]);
    }

    [Fact]
    public void SelectedIndex_ProxiesToInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();
        control.Items.Add("A");
        control.Items.Add("B");

        control.SelectedIndex = 1;

        Assert.Equal(1, control.InnerListBox.SelectedIndex);
        Assert.Equal("B", control.SelectedItem);
    }

    [Fact]
    public void ClearSelected_ProxiesToInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();
        control.Items.Add("A");
        control.SelectedIndex = 0;

        control.ClearSelected();

        Assert.Equal(-1, control.SelectedIndex);
    }

    [Fact]
    public void MoveItem_ProxiesToInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();
        control.Items.Add("A");
        control.Items.Add("B");
        control.Items.Add("C");

        control.MoveItem(0, 2);

        Assert.Equal("B", control.Items[0]);
        Assert.Equal("C", control.Items[1]);
        Assert.Equal("A", control.Items[2]);
    }

    [Fact]
    public void SelectedIndexChanged_FiresWhenInnerListBoxSelectionChanges()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();
        control.Items.Add("A");
        bool raised = false;
        control.SelectedIndexChanged += (sender, e) => raised = true;

        control.SelectedIndex = 0;

        Assert.True(raised);
    }

    [Fact]
    public void ItemsReordered_FiresWhenInnerListBoxReorders()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox();
        control.Items.Add("A");
        control.Items.Add("B");
        control.Items.Add("C");
        bool raised = false;
        control.ItemsReordered += (sender, e) => raised = true;

        control.MoveItem(0, 2);

        Assert.True(raised);
    }

    [Fact]
    public void SelectedBackColor_ProxiesToInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox
        {
            SelectedBackColor = UIColors.Primary
        };

        Assert.Equal(UIColors.Primary, control.InnerListBox.SelectedBackColor);
    }

    [Fact]
    public void DragIndicatorColor_ProxiesToInnerListBox()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox
        {
            DragIndicatorColor = UIColors.Primary
        };

        Assert.Equal(UIColors.Primary, control.InnerListBox.DragIndicatorColor);
    }

    [Fact]
    public void Title_RoundTrips()
    {
        using WfuiTitledListBox control = new WfuiTitledListBox
        {
            Title = "Sample list"
        };

        Assert.Equal("Sample list", control.Title);
    }

    [Fact]
    public void CreatePrimary_SetsSelectedBackColorToAccent()
    {
        using WfuiTitledListBox control = UIStyles.TitledListBoxes.CreatePrimary();

        Assert.Equal(UIColors.Primary, control.SelectedBackColor);
    }

    [Fact]
    public void CreatePrimary_SetsDragIndicatorColorToAccent()
    {
        using WfuiTitledListBox control = UIStyles.TitledListBoxes.CreatePrimary();

        Assert.Equal(UIColors.Primary, control.DragIndicatorColor);
    }

    [Fact]
    public void CreateStandard_KeepsColorsGray()
    {
        using WfuiTitledListBox control = UIStyles.TitledListBoxes.CreateStandard();

        Assert.Equal(UIColors.BorderLight, control.SelectedBackColor);
        Assert.Equal(UIColors.BorderLight, control.DragIndicatorColor);
    }
}
