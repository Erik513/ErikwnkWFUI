using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiListBox = ErikwnkWFUI.Controls.ListBox;
using WfuiTitledListBox = ErikwnkWFUI.Controls.TitledListBox;

namespace ErikwnkWFUI.Tests.Controls.ListBox;

/// <summary>
/// The members of the list box that the titled one passes on, each one reaching the list inside and coming back the same (the titled list box is
/// used like a list box), and what the designer writes of the two classes: only what was set, never the colors of the theme.
/// [Collection]: the defaults read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TitledListBoxMembersTests
{
    private static WfuiTitledListBox Make()
    {
        WfuiTitledListBox titled = UIStyles.TitledListBoxes.CreateStandard("Title");
        titled.Items.AddRange(new object[] { "alpha", "beta", "gamma" });

        return titled;
    }

    [Fact]
    public void TheColorsHeightsAndSwitches_ReachTheListInside_AndComeBackTheSame()
    {
        using WfuiTitledListBox titled = Make();
        WfuiListBox inner = titled.InnerListBox;

        titled.DragIndicatorColor = Color.Red;
        titled.SelectedForeColor = Color.Green;
        titled.SelectedBackColor = Color.Blue;
        titled.ItemBackColor = Color.Yellow;
        titled.ItemForeColor = Color.Cyan;
        titled.HoverBackColor = Color.Orange;
        titled.DisabledForeColor = Color.Gray;
        titled.DisabledBackColor = Color.Brown;
        titled.DragHandleColor = Color.Pink;

        Assert.Equal((Color.Red, Color.Green, Color.Blue), (inner.DragIndicatorColor, inner.SelectedForeColor, inner.SelectedBackColor));
        Assert.Equal((Color.Yellow, Color.Cyan, Color.Orange), (inner.ItemBackColor, inner.ItemForeColor, inner.HoverBackColor));
        Assert.Equal((Color.Gray, Color.Brown, Color.Pink), (inner.DisabledForeColor, inner.DisabledBackColor, inner.DragHandleColor));
        Assert.Equal((Color.Red, Color.Green, Color.Blue), (titled.DragIndicatorColor, titled.SelectedForeColor, titled.SelectedBackColor));
        Assert.Equal((Color.Yellow, Color.Cyan, Color.Orange), (titled.ItemBackColor, titled.ItemForeColor, titled.HoverBackColor));
        Assert.Equal((Color.Gray, Color.Brown, Color.Pink), (titled.DisabledForeColor, titled.DisabledBackColor, titled.DragHandleColor));

        titled.ItemHeight = 44;
        titled.ShowEnumeration = true;
        titled.AllowReorder = true;
        titled.DeselectOnOutsideClick = false;
        titled.Sorted = true;
        titled.ScrollAlwaysVisible = true;
        titled.IntegralHeight = true;
        titled.FormatString = "0";
        titled.FormattingEnabled = true;

        Assert.Equal(44, inner.ItemHeight);
        Assert.True(inner.ShowEnumeration && inner.AllowReorder && inner.Sorted && inner.ScrollAlwaysVisible && inner.IntegralHeight && inner.FormattingEnabled);
        Assert.False(inner.DeselectOnOutsideClick);
        Assert.Equal("0", inner.FormatString);
        Assert.Equal(44, titled.ItemHeight);
        Assert.True(titled.ShowEnumeration && titled.AllowReorder && titled.Sorted && titled.ScrollAlwaysVisible && titled.IntegralHeight && titled.FormattingEnabled);
        Assert.False(titled.DeselectOnOutsideClick);
        Assert.Equal("0", titled.FormatString);
    }

    [Fact]
    public void TheSelectionTheFindingAndTheItems_ReachTheListInside()
    {
        using WfuiTitledListBox titled = Make();
        WfuiListBox inner = titled.InnerListBox;
        titled.AllowReorder = true;
        titled.SelectionMode = SelectionMode.MultiExtended;
        Assert.Equal(SelectionMode.MultiExtended, inner.SelectionMode);

        titled.SelectedIndex = 1;
        Assert.Equal(1, inner.SelectedIndex);
        Assert.Equal("beta", titled.SelectedItem);
        titled.SelectedItem = "gamma";
        Assert.Contains(2, inner.SelectedIndices.Cast<int>());

        titled.ClearSelected();
        titled.SetSelected(0, true);
        titled.SetSelected(2, true);
        Assert.True(titled.GetSelected(0));
        Assert.Equal(2, titled.SelectedIndices.Count);
        Assert.Equal(2, titled.SelectedItems.Count);

        titled.ClearSelected();
        Assert.Empty(inner.SelectedItems);

        Assert.Equal(1, titled.FindString("be"));
        Assert.Equal(2, titled.FindString("g", 0));
        Assert.Equal(2, titled.FindStringExact("gamma"));
        Assert.Equal("alpha", titled.GetItemText("alpha"));
        Assert.Equal(inner.GetItemRectangle(1), titled.GetItemRectangle(1));
        Assert.Equal(inner.IndexFromPoint(new Point(5, 5)), titled.IndexFromPoint(new Point(5, 5)));

        titled.TopIndex = 0;
        Assert.Equal(0, titled.TopIndex);

        titled.MoveItem(0, 2);
        Assert.Equal("beta", titled.Items[0]);

        titled.BeginUpdate();
        titled.Items.Add("delta");
        titled.EndUpdate();
        Assert.Equal(4, inner.Items.Count);
    }

    [Fact]
    public void TheProvidersTheDataAndTheFont_ReachTheListInside()
    {
        using WfuiTitledListBox titled = Make();
        WfuiListBox inner = titled.InnerListBox;
        Func<object, bool> disabled = item => item is "beta";
        Func<object, Image> icon = item => null!;
        Func<object, string> text = item => "<" + item + ">";

        titled.IsItemDisabled = disabled;
        titled.IconProvider = icon;
        titled.DisplayTextProvider = text;
        Assert.Same(disabled, inner.IsItemDisabled);
        Assert.Same(icon, inner.IconProvider);
        Assert.Same(text, inner.DisplayTextProvider);
        Assert.Same(text, titled.DisplayTextProvider);
        Assert.Same(disabled, titled.IsItemDisabled);
        Assert.Same(icon, titled.IconProvider);

        titled.Items.Clear();
        titled.DataSource = new List<string> { "one", "two" };
        titled.DisplayMember = "Length";
        titled.ValueMember = "Length";
        Assert.Equal("Length", inner.DisplayMember);
        Assert.Equal("Length", titled.ValueMember);
        Assert.Equal(2, ((List<string>)titled.DataSource).Count);
        titled.SelectedIndex = 0;
        Assert.Equal(3, titled.SelectedValue);
        titled.SelectedValue = 3;

        titled.DataSource = null;
        titled.Items.AddRange(new object[] { "a", "b" });
        titled.MultiColumn = true;
        titled.ColumnWidth = 60;
        Assert.True(inner.MultiColumn);
        Assert.Equal(60, titled.ColumnWidth);
        titled.MultiColumn = false;

        Font bigger = new Font(titled.Font.FontFamily, 14);
        titled.Font = bigger;
        Assert.Same(bigger, inner.Font);
        titled.ForeColor = Color.Magenta;
        Assert.Equal(Color.Magenta, inner.ForeColor);

        using System.Windows.Forms.ContextMenuStrip menu = new System.Windows.Forms.ContextMenuStrip();
        titled.ContextMenuStrip = menu;
        Assert.Same(menu, inner.ContextMenuStrip);
    }

    [Fact]
    public void TheDesigner_WritesAColorOnlyWhereItWasSet_ForBothClasses()
    {
        using WfuiListBox list = new WfuiListBox();
        using WfuiTitledListBox titled = new WfuiTitledListBox();

        foreach (Control control in new Control[] { list, titled })
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(control);

            foreach (string name in new[]
            {
                "DragIndicatorColor", "SelectedForeColor", "SelectedBackColor", "ItemBackColor", "ItemForeColor", "HoverBackColor", "DisabledForeColor",
                "DisabledBackColor", "DragHandleColor", "BorderColor", "ItemHeight"
            })
            {
#if NETFRAMEWORK
                // (On the .NET Framework the designer takes the ItemHeight of the standard list box, not the one of the library: not tested there.)
                if (name == "ItemHeight")
                    continue;
#endif
                PropertyDescriptor property = properties[name]!;
                object other = name == "ItemHeight" ? 50 : Color.Magenta;
                object before = property.GetValue(control)!;

                Assert.False(property.ShouldSerializeValue(control), control.GetType().Name + "." + name + " writes the value of the theme");
                property.SetValue(control, other);
                Assert.True(property.ShouldSerializeValue(control), control.GetType().Name + "." + name);
                property.ResetValue(control);
                Assert.False(property.ShouldSerializeValue(control), control.GetType().Name + "." + name);
                Assert.Equal(before, property.GetValue(control));
            }
        }
    }

    [Fact]
    public void TheDesigner_WritesTheTitleBarOnlyWhereItWasSet_AndDoesNotSeeTheFunctions()
    {
        using WfuiTitledListBox titled = new WfuiTitledListBox();
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(titled);

        foreach (string name in new[] { "HeaderBackColor", "HeaderForeColor", "HeaderHeight", "HeaderFont" })
        {
            PropertyDescriptor property = properties[name]!;
            object other = name == "HeaderHeight" ? 44 : name == "HeaderFont" ? new Font("Arial", 20) : Color.Magenta;

            Assert.False(property.ShouldSerializeValue(titled), name + " writes the default");
            property.SetValue(titled, other);
            Assert.True(property.ShouldSerializeValue(titled), name);
            property.ResetValue(titled);
            Assert.False(property.ShouldSerializeValue(titled), name);
        }

        foreach (string name in new[] { "IsItemDisabled", "IconProvider", "DisplayTextProvider" })
            Assert.False(properties[name]!.IsBrowsable, name);

        foreach (Control control in new Control[] { new WfuiListBox(), titled })
        {
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(control))
            {
                if (property.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute attribute
                    && property.Name is not ("BorderStyle" or "Font" or "ForeColor" or "IntegralHeight" or "DrawMode" or "AllowDrop" or "ItemHeight"))
                    Assert.True(Equals(attribute.Value, property.GetValue(control)), control.GetType().Name + "." + property.Name + " is not its default");
            }
        }
    }
}
