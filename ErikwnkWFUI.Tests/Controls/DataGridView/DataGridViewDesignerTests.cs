using System.ComponentModel;
using System.Drawing;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>What the designer writes of the two grids: only the colors that were set (never those of the theme), nothing of the color set of the toast, and the defaults.</summary>
[Collection(AccentColorTestCollection.Name)]
public class DataGridViewDesignerTests
{
    [Fact]
    public void TheDesigner_WritesAColorOnlyWhereItWasSet_ForBothGrids()
    {
        foreach (System.Windows.Forms.DataGridView grid in new System.Windows.Forms.DataGridView[] { new ReadOnlyDataGridView(), new ErikwnkWFUI.Controls.DataGridView() })
        {
            using (grid)
            {
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(grid);

                foreach (string name in new[]
                {
                    "ContextMenuSelectionColor", "HeaderBackColor", "HeaderForeColor", "BorderColor", "RowBackColor", "RowForeColor", "SelectionBackColor",
                    "ColumnReorderIndicatorColor"
                })
                {
                    PropertyDescriptor property = properties[name]!;

                    Assert.False(property.ShouldSerializeValue(grid), grid.GetType().Name + "." + name + " writes the value of the theme");
                    property.SetValue(grid, Color.Magenta);
                    Assert.True(property.ShouldSerializeValue(grid), grid.GetType().Name + "." + name);
                    property.ResetValue(grid);
                    Assert.False(property.ShouldSerializeValue(grid), grid.GetType().Name + "." + name);
                }

                Assert.False(properties["ToastColors"]!.IsBrowsable);

                foreach (PropertyDescriptor property in properties)
                {
                    if (property.Attributes[typeof(DefaultValueAttribute)] is DefaultValueAttribute attribute && typeof(ReadOnlyDataGridView).IsAssignableFrom(property.ComponentType))
                        Assert.True(Equals(attribute.Value, property.GetValue(grid)), grid.GetType().Name + "." + property.Name + " is not its default");
                }
            }
        }
    }

    [Fact]
    public void TheDeleteColumn_IsOffByDefault_AndTheDesignerKnowsIt()
    {
        using ErikwnkWFUI.Controls.DataGridView grid = new ErikwnkWFUI.Controls.DataGridView();

        Assert.False(grid.ShowDeleteRowColumn);
        Assert.False(TypeDescriptor.GetProperties(grid)["ShowDeleteRowColumn"]!.ShouldSerializeValue(grid));
    }
}
