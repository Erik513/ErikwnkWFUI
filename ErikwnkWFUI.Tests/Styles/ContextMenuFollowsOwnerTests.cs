using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiContextMenuStrip = ErikwnkWFUI.Controls.ContextMenuStrip;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// The context menu built into a list view, a grid or a tab control takes its highlight from the control's own
/// ContextMenuSelectionColor each time it paints: a color set by hand, the accent of a Primary control and a change of
/// the theme reach the menu without anyone handing it a color. [Collection]: the theme and the accent are switched.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ContextMenuFollowsOwnerTests : IDisposable
{
    private readonly Color _accentAtStart = UIColors.Primary;

    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(_accentAtStart);
    }

    private static WfuiContextMenuStrip MenuOf(Control owner)
    {
        if (owner is ErikwnkWFUI.Controls.TabControl tabs)
        {
            tabs.AllowUserToAddTabs = true;
            return (WfuiContextMenuStrip)tabs.ContextMenuStrip!;
        }

        return (WfuiContextMenuStrip)owner.ContextMenuStrip!;
    }

    private static Color OwnerColor(Control owner) => owner switch
    {
        ErikwnkWFUI.Controls.ListView list => list.ContextMenuSelectionColor,
        ErikwnkWFUI.Controls.ReadOnlyDataGridView grid => grid.ContextMenuSelectionColor,
        ErikwnkWFUI.Controls.TabControl tabs => tabs.ContextMenuSelectionColor,
        _ => throw new InvalidOperationException()
    };

    private static void SetOwnerColor(Control owner, Color color)
    {
        switch (owner)
        {
            case ErikwnkWFUI.Controls.ListView list: list.ContextMenuSelectionColor = color; break;
            case ErikwnkWFUI.Controls.ReadOnlyDataGridView grid: grid.ContextMenuSelectionColor = color; break;
            case ErikwnkWFUI.Controls.TabControl tabs: tabs.ContextMenuSelectionColor = color; break;
        }
    }

    public static IEnumerable<object[]> Owners()
    {
        foreach (var entry in new (string, Func<Control>)[]
        {
            ("ListView.Standard", () => UIStyles.ListViews.CreateStandard()),
            ("ListView.Primary", () => UIStyles.ListViews.CreatePrimary()),
            ("DataGridView.Standard", () => UIStyles.DataGridViews.CreateReadOnlyStandard()),
            ("DataGridView.Primary", () => UIStyles.DataGridViews.CreateReadOnlyPrimary()),
            ("DataGridView.Editable", () => UIStyles.DataGridViews.CreatePrimary()),
            ("TabControl.Standard", () => UIStyles.TabControls.CreateStandard()),
            ("TabControl.Primary", () => UIStyles.TabControls.CreatePrimary()),
        })
            yield return new object[] { entry.Item1, entry.Item2 };
    }

    [Theory]
    [MemberData(nameof(Owners))]
    public void TheMenu_HasTheColorOfTheOwner(string name, Func<Control> make)
    {
        using Control owner = make();

        Assert.Equal(OwnerColor(owner), MenuOf(owner).SelectionBackColor);
    }

    [Theory]
    [MemberData(nameof(Owners))]
    public void AColorSetOnTheOwnerByHand_ReachesTheMenu_AlsoLater(string name, Func<Control> make)
    {
        using Control owner = make();
        WfuiContextMenuStrip menu = MenuOf(owner);

        SetOwnerColor(owner, Color.Orange);
        Assert.Equal(Color.Orange, menu.SelectionBackColor);

        SetOwnerColor(owner, Color.Teal);
        Assert.Equal(Color.Teal, menu.SelectionBackColor);
    }

    [Theory]
    [MemberData(nameof(Owners))]
    public void TheMenu_FollowsTheAccent_AndTheTheme_OfAPrimaryOwner(string name, Func<Control> make)
    {
        using Control owner = make();
        WfuiContextMenuStrip menu = MenuOf(owner);
        Color before = menu.SelectionBackColor;

        UIColors.SetAccent(Color.OrangeRed);
        UIColors.ApplyTheme(UIThemes.Light);

        Assert.Equal(OwnerColor(owner), menu.SelectionBackColor);

        if (name.Contains("Primary"))
            Assert.NotEqual(before, menu.SelectionBackColor);
    }

    [Theory]
    [MemberData(nameof(Owners))]
    public void TheMenu_HasNoColorOfItsOwn_ItAsksTheOwner(string name, Func<Control> make)
    {
        using Control owner = make();
        WfuiContextMenuStrip menu = MenuOf(owner);

        // Not "set by hand" in the menu: the text on the highlight is still worked out for the color it has now.
        UIColors.SetAccent(Color.Gold);

        Assert.Equal(OwnerColor(owner), menu.SelectionBackColor);
    }
}
