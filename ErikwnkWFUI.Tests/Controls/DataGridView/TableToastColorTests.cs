using System.ComponentModel;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Controls.ToastFormTests;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiDataGridView = ErikwnkWFUI.Controls.DataGridView;
using WfuiListView = ErikwnkWFUI.Controls.ListView;
using WfuiReadOnlyDataGridView = ErikwnkWFUI.Controls.ReadOnlyDataGridView;

namespace ErikwnkWFUI.Tests.Controls.DataGridView;

/// <summary>The toast that confirms a copy or an action of a list or a grid: its color follows the look of the control, and can be set.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TableToastColorTests : ToastTestBase
{
    public override void Dispose()
    {
        UIColors.SetAccent(UIAccentColors.Blue);
        base.Dispose();
    }

    private static UIColorSet? GridToast(Control grid)
    {
        return grid is WfuiReadOnlyDataGridView readOnly ? readOnly.ToastColors : ((WfuiListView)grid).ToastColors;
    }

    private (Form Host, T Control) On<T>(T control) where T : Control
    {
        Form host = Host();
        control.Dock = DockStyle.Fill;
        host.Controls.Add(control);

        return (host, control);
    }

    [Fact]
    public void TheStandardGridAndList_ConfirmWithTheNeutralGray_TheOnesWithALook_WithTheColorOfTheLook()
    {
        Assert.Null(GridToast(UIStyles.DataGridViews.CreateStandard()));
        Assert.Null(GridToast(UIStyles.DataGridViews.CreateReadOnlyStandard()));
        Assert.Null(GridToast(UIStyles.ListViews.CreateStandard()));

        Assert.Same(UIStyles.Colors.Sets.Green, GridToast(UIStyles.DataGridViews.CreateStandard(colors: UIStyles.Colors.Sets.Green)));
        Assert.Same(UIStyles.Colors.Sets.Green, GridToast(UIStyles.DataGridViews.CreateReadOnlyStandard(colors: UIStyles.Colors.Sets.Green)));
        Assert.Same(UIStyles.Colors.Sets.Red, GridToast(UIStyles.ListViews.CreateStandard(UIStyles.Colors.Sets.Red)));

        foreach (Control primary in new Control[] { UIStyles.DataGridViews.CreatePrimary(), UIStyles.DataGridViews.CreateReadOnlyPrimary(), UIStyles.ListViews.CreatePrimary() })
            Assert.Equal(UIColors.PrimarySet.Hover, GridToast(primary)!.Hover);
    }

    [Fact]
    public void ThePrimaryToast_FollowsTheAccentWhileTheGridIsKept()
    {
        using System.Windows.Forms.DataGridView grid = UIStyles.DataGridViews.CreatePrimary();
        using System.Windows.Forms.ListView list = UIStyles.ListViews.CreatePrimary();

        UIColors.SetAccent(UIAccentColors.Green);

        Assert.Equal(UIColors.PrimarySet.Hover, ((WfuiReadOnlyDataGridView)grid).ToastColors!.Hover);
        Assert.Equal(UIColors.PrimarySet.Hover, ((WfuiListView)list).ToastColors!.Hover);
        Assert.Equal(UIAccentColors.Green, ((WfuiListView)list).ToastColors!.Hover);
    }

    [Fact]
    public void ToastColors_CanBeSet_AndWhatWasSetWinsOverTheLook_NullIsTheGray()
    {
        using WfuiReadOnlyDataGridView grid = (WfuiReadOnlyDataGridView)UIStyles.DataGridViews.CreatePrimary();
        using WfuiListView list = (WfuiListView)UIStyles.ListViews.CreatePrimary();

        grid.ToastColors = UIStyles.Colors.Sets.Yellow;
        list.ToastColors = UIStyles.Colors.Sets.Yellow;
        UIColors.SetAccent(UIAccentColors.Red);

        Assert.Same(UIStyles.Colors.Sets.Yellow, grid.ToastColors);
        Assert.Same(UIStyles.Colors.Sets.Yellow, list.ToastColors);

        grid.ToastColors = null;
        list.ToastColors = null;

        Assert.Null(grid.ToastColors);
        Assert.Null(list.ToastColors);
    }

    [Fact]
    public void TheToastThatIsShown_HasTheColorOfTheGrid_AndOfTheList()
    {
        // An action of the grid (here: the message of a copy) ...
        (Form host, WfuiReadOnlyDataGridView grid) = On((WfuiReadOnlyDataGridView)UIStyles.DataGridViews.CreateReadOnlyStandard(colors: UIStyles.Colors.Sets.Green));
        grid.InvokePrivate("OnActionConfirmation", "Copied");

        Assert.NotNull(Current());
        Assert.Same(UIStyles.Colors.Sets.Green, Current()!.Colors);
        Current()!.Dispose();

        // ... the same grid with the color set by hand ...
        grid.ToastColors = UIStyles.Colors.Sets.Red;
        grid.InvokePrivate("OnActionConfirmation", "Copied");
        Assert.Same(UIStyles.Colors.Sets.Red, Current()!.Colors);
        Current()!.Dispose();

        // ... the plain one is gray ...
        (Form otherHost, WfuiReadOnlyDataGridView plain) = On((WfuiReadOnlyDataGridView)UIStyles.DataGridViews.CreateReadOnlyStandard());
        plain.InvokePrivate("OnActionConfirmation", "Copied");
        Assert.Null(Current()!.Colors);
        Current()!.Dispose();

        // ... and the list confirms a copy in its own color.
        (Form listHost, WfuiListView list) = On((WfuiListView)UIStyles.ListViews.CreateStandard(UIStyles.Colors.Sets.Teal));
        list.InvokePrivate("ShowCopyToast", "Copied");
        Assert.Same(UIStyles.Colors.Sets.Teal, Current()!.Colors);
    }
}